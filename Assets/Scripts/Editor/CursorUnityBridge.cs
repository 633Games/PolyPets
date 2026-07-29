#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using PolyPets.DebugTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PolyPets.EditorTools
{
    /// <summary>
    /// File-based command bridge for Cursor agents.
    /// Watches <c>.cursor/unity/cmd.json</c>, writes compact JSON to <c>out.json</c>.
    /// </summary>
    [InitializeOnLoad]
    public static class CursorUnityBridge
    {
        public const string BridgeVersion = "1.4.2";
        private const float PollSeconds = 0.35f;
        private const int MaxLogEntries = 200;
        private const int DefaultLogLines = 40;
        private const int MaxHierarchyNodes = 120;
        private const int MaxFindResults = 40;
        private const int MaxStringValueChars = 80;
        private const int MaxScreenshotEdge = 1280;

        private static readonly object LogLock = new();
        private static readonly List<LogEntry> LogRing = new(MaxLogEntries);
        private static readonly HashSet<string> AllowedMenus = new(StringComparer.Ordinal)
        {
            "PolyPets/★ First-Time Setup (run this)",
            "PolyPets/Bootstrap Starter House Scene",
            "PolyPets/Select Starter Scene",
            "PolyPets/Frame Camera On Active Room",
            "PolyPets/Rebuild Volume Profile",
            "PolyPets/Ensure URP Pipeline Assets",
            "PolyPets/UI/Apply Cozy Companion HUD",
            "PolyPets/UI/Apply Cozy Companion HUD (Silent)",
            "PolyPets/UI/Build UI Prefab Kit + Sprite Pack",
            "PolyPets/UI/Select Sprite Pack",
            "PolyPets/UI/Apply Vendor Sprite Pack (v1)",
            "PolyPets/Materials/Rebuild Color Palette (25 mats)",
            "PolyPets/Materials/Rebuild Color Palette (Silent)",
            "PolyPets/Materials/Select Material Palette",
            "PolyPets/Materials/Reimport Texture Placeholders",
            "PolyPets/Feel/Apply Tags In Open Scene",
            "PolyPets/Feel/Wrap Selection With FEEL[Squash]",
            "PolyPets/Feel/Detect More Mountains Feel Package",
            "PolyPets/Feel/Upgrade Tags To MMF Players (requires Feel)",
            "File/Save",
            "File/Save As...",
            "Edit/Play",
            "Edit/Pause",
            "Edit/Step",
            "Assets/Refresh",
        };

        private static string _bridgeDir;
        private static string _cmdPath;
        private static string _outPath;
        private static string _statusPath;
        private static string _capturesDir;
        private static string _lastCmdId;
        private static double _nextPollTime;
        private static bool _hookedLogs;
        private static bool _captureHooked;
        private static bool _captureInFlight;
        private static string _captureCmdId;
        private static string _captureCmdName;
        private static string _captureRelPath;
        private static string _captureAbsPath;
        private static string _captureView;
        private static double _captureStartedAt;
        private static int _captureMaxEdge;

        static CursorUnityBridge()
        {
            ResolvePaths();
            EnsureBridgeDir();
            HookLogs();
            HookCapture();
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.quitting += OnQuitting;
            WriteStatus("ready");
            Debug.Log($"[CursorUnityBridge] v{BridgeVersion} watching {_cmdPath}");
        }

        private static void ResolvePaths()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            _bridgeDir = Path.Combine(projectRoot, ".cursor", "unity");
            _cmdPath = Path.Combine(_bridgeDir, "cmd.json");
            _outPath = Path.Combine(_bridgeDir, "out.json");
            _statusPath = Path.Combine(_bridgeDir, "status.json");
            _capturesDir = Path.Combine(_bridgeDir, "captures");
        }

        private static void EnsureBridgeDir()
        {
            if (!Directory.Exists(_bridgeDir))
                Directory.CreateDirectory(_bridgeDir);
            if (!Directory.Exists(_capturesDir))
                Directory.CreateDirectory(_capturesDir);
        }

        private static void HookLogs()
        {
            if (_hookedLogs)
                return;
            Application.logMessageReceivedThreaded += OnLogMessage;
            _hookedLogs = true;
        }

        private static void HookCapture()
        {
            if (_captureHooked)
                return;
            CursorUnityCaptureRunner.Completed += OnCaptureCompleted;
            _captureHooked = true;
        }

        private static void OnQuitting()
        {
            WriteStatus("quitting");
            if (_hookedLogs)
            {
                Application.logMessageReceivedThreaded -= OnLogMessage;
                _hookedLogs = false;
            }

            if (_captureHooked)
            {
                CursorUnityCaptureRunner.Completed -= OnCaptureCompleted;
                _captureHooked = false;
            }

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            ResetCaptureState("quitting");
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state is PlayModeStateChange.ExitingPlayMode or PlayModeStateChange.EnteredEditMode)
                ResetCaptureState("play-mode-exit");
        }

        private static void ResetCaptureState(string reason)
        {
            if (!_captureInFlight)
                return;
            var pendingId = _captureCmdId;
            _captureInFlight = false;
            _captureCmdId = null;
            _captureCmdName = null;
            _captureAbsPath = null;
            if (!string.IsNullOrEmpty(pendingId))
            {
                WriteOut(new ResponseEnvelope
                {
                    id = pendingId,
                    ok = false,
                    cmd = "screenshot",
                    error = "Capture cancelled (" + reason + ")"
                });
            }
        }

        private static void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            lock (LogLock)
            {
                if (LogRing.Count >= MaxLogEntries)
                    LogRing.RemoveAt(0);
                LogRing.Add(new LogEntry
                {
                    t = DateTime.UtcNow.ToString("o"),
                    type = type.ToString(),
                    msg = Truncate(condition, 240),
                    stack = type is LogType.Error or LogType.Exception
                        ? Truncate(FirstStackLine(stackTrace), 160)
                        : null
                });
            }
        }

        private static void OnEditorUpdate()
        {
            if (EditorApplication.timeSinceStartup < _nextPollTime)
                return;
            _nextPollTime = EditorApplication.timeSinceStartup + PollSeconds;

            if ((EditorApplication.timeSinceStartup % 5.0) < PollSeconds)
                WriteStatus("ready");

            if (_captureInFlight &&
                EditorApplication.timeSinceStartup - _captureStartedAt > 3.0)
            {
                FinishCaptureWithCameraFallback("async capture timed out");
            }

            if (!File.Exists(_cmdPath))
                return;

            string raw;
            try
            {
                raw = File.ReadAllText(_cmdPath, Encoding.UTF8);
            }
            catch (IOException)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(raw))
                return;

            CommandEnvelope cmd;
            try
            {
                cmd = JsonUtility.FromJson<CommandEnvelope>(raw);
            }
            catch (Exception ex)
            {
                WriteOut(new ResponseEnvelope
                {
                    id = "",
                    ok = false,
                    cmd = "parse",
                    error = "Invalid JSON: " + ex.Message
                });
                TryDeleteCmd();
                return;
            }

            if (cmd == null || string.IsNullOrEmpty(cmd.cmd))
                return;

            if (!string.IsNullOrEmpty(cmd.id) && cmd.id == _lastCmdId)
                return;

            _lastCmdId = cmd.id;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            ResponseEnvelope response;
            try
            {
                response = Dispatch(cmd);
            }
            catch (Exception ex)
            {
                response = new ResponseEnvelope
                {
                    id = cmd.id,
                    ok = false,
                    cmd = cmd.cmd,
                    error = Truncate(ex.GetType().Name + ": " + ex.Message, 300)
                };
            }

            // null => async in flight (e.g. play-mode Game View capture)
            if (response == null)
            {
                TryDeleteCmd();
                return;
            }

            sw.Stop();
            response.ms = (int)sw.ElapsedMilliseconds;
            WriteOut(response);
            TryDeleteCmd();
        }

        private static ResponseEnvelope Dispatch(CommandEnvelope cmd)
        {
            var name = cmd.cmd.Trim().ToLowerInvariant();
            return name switch
            {
                "ping" or "status" => Ping(cmd),
                "hierarchy" => Hierarchy(cmd),
                "find" => Find(cmd),
                "log" => Log(cmd),
                "exec" => Exec(cmd),
                "get-component" or "getcomponent" or "component" => GetComponent(cmd),
                "menus" => ListMenus(cmd),
                "screenshot" or "capture" => Screenshot(cmd),
                "click" or "tap" => Click(cmd),
                "press" or "invoke" => PressUi(cmd),
                "focus" => FocusView(cmd),
                "select" => SelectObject(cmd),
                "gameview" or "game-view" => GameViewInfo(cmd),
                "play" => SetPlay(cmd),
                "set-gameview" or "gameview-set" => SetGameViewSize(cmd),
                "rebuild-hud" or "rebuildhud" => RebuildHud(cmd),
                "refresh" or "reimport" => RefreshAssets(cmd),
                _ => new ResponseEnvelope
                {
                    id = cmd.id,
                    ok = false,
                    cmd = cmd.cmd,
                    error = "Unknown cmd. Use: ping, hierarchy, find, log, exec, get-component, menus, screenshot, click, press, focus, select, gameview, play, set-gameview, rebuild-hud, refresh"
                }
            };
        }

        private static ResponseEnvelope Ping(CommandEnvelope cmd)
        {
            var scene = EditorSceneManager.GetActiveScene();
            var gv = GetGameViewSize();
            var sb = new StringBuilder(320);
            sb.Append('{');
            AppendKv(sb, "bridge", BridgeVersion, true);
            AppendKv(sb, "unity", Application.unityVersion);
            AppendKv(sb, "scene", scene.name);
            AppendKv(sb, "scenePath", scene.path);
            AppendKv(sb, "dirty", scene.isDirty);
            AppendKv(sb, "playMode", EditorApplication.isPlaying);
            AppendKv(sb, "compiling", EditorApplication.isCompiling);
            AppendKv(sb, "paused", EditorApplication.isPaused);
            AppendKv(sb, "platform", Application.platform.ToString());
            AppendKv(sb, "project", Path.GetFileName(Path.GetFullPath(Path.Combine(Application.dataPath, ".."))));
            AppendKv(sb, "gameViewW", (int)gv.x);
            AppendKv(sb, "gameViewH", (int)gv.y);
            sb.Append('}');

            return Ok(cmd, sb.ToString());
        }

        private static ResponseEnvelope Hierarchy(CommandEnvelope cmd)
        {
            var depth = Clamp(cmd.depth > 0 ? cmd.depth : 2, 1, 6);
            var maxNodes = Clamp(cmd.limit > 0 ? cmd.limit : 60, 1, MaxHierarchyNodes);
            var rootFilter = cmd.name;

            var scene = EditorSceneManager.GetActiveScene();
            var roots = scene.GetRootGameObjects();
            var nodes = new List<string>(maxNodes);
            var truncated = false;
            var count = 0;

            foreach (var root in roots)
            {
                if (!string.IsNullOrEmpty(rootFilter) &&
                    root.name.IndexOf(rootFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                if (!WalkHierarchy(root, 0, depth, maxNodes, nodes, ref count))
                {
                    truncated = true;
                    break;
                }
            }

            var sb = new StringBuilder(512);
            sb.Append('{');
            AppendKv(sb, "scene", scene.name, true);
            AppendKv(sb, "depth", depth);
            AppendKv(sb, "count", count);
            AppendKv(sb, "truncated", truncated);
            AppendCommaIfNeeded(sb);
            sb.Append("\"tree\":[");
            for (var i = 0; i < nodes.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(nodes[i]);
            }
            sb.Append("]}");
            return Ok(cmd, sb.ToString());
        }

        private static bool WalkHierarchy(
            GameObject go, int level, int maxDepth, int maxNodes,
            List<string> nodes, ref int count)
        {
            if (count >= maxNodes)
                return false;

            var childCount = go.transform.childCount;
            var comps = go.GetComponents<Component>();
            var typeNames = new List<string>(Math.Min(comps.Length, 8));
            foreach (var c in comps)
            {
                if (c == null || c is Transform) continue;
                typeNames.Add(c.GetType().Name);
                if (typeNames.Count >= 6) break;
            }

            var sb = new StringBuilder(128);
            sb.Append('{');
            AppendKv(sb, "name", go.name, true);
            AppendKv(sb, "active", go.activeInHierarchy);
            AppendKv(sb, "depth", level);
            AppendKv(sb, "children", childCount);
            AppendCommaIfNeeded(sb);
            sb.Append("\"comps\":[");
            for (var i = 0; i < typeNames.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append('"').Append(Esc(typeNames[i])).Append('"');
            }
            sb.Append("]}");
            nodes.Add(sb.ToString());
            count++;

            if (level >= maxDepth)
                return true;

            for (var i = 0; i < childCount; i++)
            {
                var child = go.transform.GetChild(i).gameObject;
                if (!WalkHierarchy(child, level + 1, maxDepth, maxNodes, nodes, ref count))
                    return false;
            }

            return true;
        }

        private static ResponseEnvelope Find(CommandEnvelope cmd)
        {
            var query = cmd.name ?? cmd.query ?? "";
            var typeName = cmd.type ?? cmd.component ?? "";
            var limit = Clamp(cmd.limit > 0 ? cmd.limit : 20, 1, MaxFindResults);

            Type componentType = null;
            if (!string.IsNullOrEmpty(typeName))
            {
                componentType = FindType(typeName);
                if (componentType == null)
                {
                    return new ResponseEnvelope
                    {
                        id = cmd.id,
                        ok = false,
                        cmd = cmd.cmd,
                        error = "Type not found: " + typeName
                    };
                }
            }

            var hits = new List<string>(limit);
            var all = UnityEngine.Object.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var go in all)
            {
                if (hits.Count >= limit)
                    break;

                if (!string.IsNullOrEmpty(query) &&
                    go.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                if (componentType != null && go.GetComponent(componentType) == null)
                    continue;

                hits.Add(CompactObject(go, componentType));
            }

            var sb = new StringBuilder(256);
            sb.Append('{');
            AppendKv(sb, "query", query, true);
            AppendKv(sb, "type", typeName ?? "");
            AppendKv(sb, "count", hits.Count);
            AppendKv(sb, "capped", hits.Count >= limit);
            AppendCommaIfNeeded(sb);
            sb.Append("\"hits\":[");
            for (var i = 0; i < hits.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(hits[i]);
            }
            sb.Append("]}");
            return Ok(cmd, sb.ToString());
        }

        private static ResponseEnvelope Log(CommandEnvelope cmd)
        {
            var n = Clamp(cmd.limit > 0 ? cmd.limit : DefaultLogLines, 1, MaxLogEntries);
            var filter = cmd.filter ?? cmd.query ?? "";
            var typeFilter = cmd.type ?? "";

            List<LogEntry> snapshot;
            lock (LogLock)
            {
                snapshot = new List<LogEntry>(LogRing);
            }

            var selected = new List<LogEntry>(n);
            for (var i = snapshot.Count - 1; i >= 0 && selected.Count < n; i--)
            {
                var e = snapshot[i];
                if (!string.IsNullOrEmpty(typeFilter) &&
                    e.type.IndexOf(typeFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                if (!string.IsNullOrEmpty(filter) &&
                    (e.msg == null || e.msg.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0))
                    continue;
                selected.Add(e);
            }

            selected.Reverse();

            var sb = new StringBuilder(512);
            sb.Append('{');
            AppendKv(sb, "count", selected.Count, true);
            AppendKv(sb, "ringSize", snapshot.Count);
            AppendKv(sb, "filter", filter);
            AppendKv(sb, "type", typeFilter);
            AppendCommaIfNeeded(sb);
            sb.Append("\"lines\":[");
            for (var i = 0; i < selected.Count; i++)
            {
                if (i > 0) sb.Append(',');
                var e = selected[i];
                sb.Append('{');
                AppendKv(sb, "type", e.type, true);
                AppendKv(sb, "msg", e.msg ?? "");
                if (!string.IsNullOrEmpty(e.stack))
                    AppendKv(sb, "stack", e.stack);
                sb.Append('}');
            }
            sb.Append("]}");
            return Ok(cmd, sb.ToString());
        }

        private static ResponseEnvelope Exec(CommandEnvelope cmd)
        {
            var menu = cmd.menu ?? cmd.name ?? cmd.query ?? "";
            if (string.IsNullOrWhiteSpace(menu))
            {
                return new ResponseEnvelope
                {
                    id = cmd.id,
                    ok = false,
                    cmd = cmd.cmd,
                    error = "Provide args.menu (allowlisted PolyPets/... path). Use cmd=menus."
                };
            }

            if (!AllowedMenus.Contains(menu))
            {
                if (!menu.StartsWith("PolyPets/", StringComparison.Ordinal))
                {
                    return new ResponseEnvelope
                    {
                        id = cmd.id,
                        ok = false,
                        cmd = cmd.cmd,
                        error = "Menu not allowlisted. Use cmd=menus for safe options."
                    };
                }
            }

            var ok = EditorApplication.ExecuteMenuItem(menu);
            var sb = new StringBuilder(128);
            sb.Append('{');
            AppendKv(sb, "menu", menu, true);
            AppendKv(sb, "executed", ok);
            sb.Append('}');
            if (!ok)
            {
                return new ResponseEnvelope
                {
                    id = cmd.id,
                    ok = false,
                    cmd = cmd.cmd,
                    error = "ExecuteMenuItem returned false (menu missing or unavailable).",
                    data = sb.ToString()
                };
            }

            return Ok(cmd, sb.ToString());
        }

        private static ResponseEnvelope ListMenus(CommandEnvelope cmd)
        {
            var sb = new StringBuilder(512);
            sb.Append('{');
            sb.Append("\"menus\":[");
            var first = true;
            foreach (var m in AllowedMenus)
            {
                if (!first) sb.Append(',');
                first = false;
                sb.Append('"').Append(Esc(m)).Append('"');
            }
            sb.Append("]}");
            return Ok(cmd, sb.ToString());
        }

        private static ResponseEnvelope GetComponent(CommandEnvelope cmd)
        {
            var objectName = cmd.name ?? cmd.query ?? "";
            var typeName = cmd.type ?? cmd.component ?? "";
            if (string.IsNullOrEmpty(objectName))
            {
                return new ResponseEnvelope
                {
                    id = cmd.id,
                    ok = false,
                    cmd = cmd.cmd,
                    error = "Provide args.name (GameObject name contains)."
                };
            }

            GameObject go = null;
            var all = UnityEngine.Object.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var candidate in all)
            {
                if (candidate.name.IndexOf(objectName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    go = candidate;
                    break;
                }
            }

            if (go == null)
            {
                return new ResponseEnvelope
                {
                    id = cmd.id,
                    ok = false,
                    cmd = cmd.cmd,
                    error = "GameObject not found: " + objectName
                };
            }

            Component target = null;
            if (!string.IsNullOrEmpty(typeName))
            {
                var t = FindType(typeName);
                if (t == null)
                {
                    return new ResponseEnvelope
                    {
                        id = cmd.id,
                        ok = false,
                        cmd = cmd.cmd,
                        error = "Type not found: " + typeName
                    };
                }

                target = go.GetComponent(t);
                if (target == null)
                    target = go.GetComponentInChildren(t, true);
            }
            else
            {
                var careType = FindType("CareHudController");
                if (careType != null)
                    target = go.GetComponent(careType);
                if (target == null)
                    target = go.GetComponent<MonoBehaviour>();
            }

            if (target == null)
            {
                return new ResponseEnvelope
                {
                    id = cmd.id,
                    ok = false,
                    cmd = cmd.cmd,
                    error = "No matching component on " + go.name
                };
            }

            var fields = SnapshotSerialized(target);
            var sb = new StringBuilder(512);
            sb.Append('{');
            AppendKv(sb, "object", go.name, true);
            AppendKv(sb, "path", GetPath(go.transform));
            AppendKv(sb, "active", go.activeInHierarchy);
            AppendKv(sb, "component", target.GetType().Name);
            AppendCommaIfNeeded(sb);
            sb.Append("\"fields\":");
            sb.Append(fields);
            sb.Append('}');
            return Ok(cmd, sb.ToString());
        }

        private static ResponseEnvelope Screenshot(CommandEnvelope cmd)
        {
            if (_captureInFlight)
            {
                return new ResponseEnvelope
                {
                    id = cmd.id,
                    ok = false,
                    cmd = cmd.cmd,
                    error = "Capture already in progress"
                };
            }

            var view = (cmd.view ?? cmd.type ?? cmd.name ?? "game").Trim().ToLowerInvariant();
            if (view is "capture" or "screenshot")
                view = "game";
            if (view is "desktop" or "win" or "hwnd")
                view = "window";
            if (view is not ("game" or "scene" or "both" or "window" or "camera"))
            {
                return new ResponseEnvelope
                {
                    id = cmd.id,
                    ok = false,
                    cmd = cmd.cmd,
                    error = "view must be game, scene, both, window (OS blit), or camera (Unity render)"
                };
            }

            EnsureBridgeDir();
            var stem = string.IsNullOrWhiteSpace(cmd.path)
                ? $"cap_{DateTime.UtcNow:yyyyMMdd_HHmmss}_{cmd.id}"
                : SanitizeFileStem(cmd.path);
            var maxEdge = cmd.limit > 0 ? Clamp(cmd.limit, 64, MaxScreenshotEdge) : 720;

            // OS desktop blit of editor panes — preferred for Game View (includes Overlay UI)
            if (view is "game" or "scene" or "window")
            {
                var abs = Path.Combine(_capturesDir, stem + ".png");
                EditorWindow targetWin = null;
                var insetTop = 0f;
                if (view == "game")
                {
                    FocusGameView();
                    targetWin = GetGameViewWindow();
                    insetTop = 39f; // tab + Game View toolbar
                }
                else if (view == "scene")
                {
                    FocusSceneView();
                    targetWin = SceneView.lastActiveSceneView;
                    if (targetWin == null && SceneView.sceneViews.Count > 0)
                        targetWin = SceneView.sceneViews[0] as SceneView;
                    insetTop = 39f;
                }
                else
                {
                    targetWin = EditorWindow.focusedWindow ?? GetGameViewWindow();
                }

                string deskErr = null;
                int w = 0, h = 0;
                if (targetWin != null &&
                    TryCaptureEditorWindowDesktop(targetWin, abs, maxEdge, insetTop, out w, out h, out deskErr))
                {
                    return Ok(cmd, CaptureMetaJson(view, RelFromProject(abs), w, h, false, "desktop"));
                }

                if (view == "window")
                {
                    return new ResponseEnvelope
                    {
                        id = cmd.id,
                        ok = false,
                        cmd = cmd.cmd,
                        error = deskErr ?? "Desktop window capture failed"
                    };
                }

                // Fall through to Unity camera / async paths for game & scene
                if (view == "scene")
                {
                    var sv = SceneView.lastActiveSceneView;
                    if (sv == null || sv.camera == null)
                    {
                        return new ResponseEnvelope
                        {
                            id = cmd.id,
                            ok = false,
                            cmd = cmd.cmd,
                            error = deskErr ?? "No active Scene View camera"
                        };
                    }

                    var saved = SaveCameraCapture(sv.camera, abs, maxEdge, out var err, out w, out h);
                    if (saved == null)
                    {
                        return new ResponseEnvelope
                        {
                            id = cmd.id,
                            ok = false,
                            cmd = cmd.cmd,
                            error = err ?? deskErr ?? "Scene capture failed"
                        };
                    }

                    return Ok(cmd, CaptureMetaJson("scene", saved, w, h, true, "camera"));
                }

                // game fallback
                return CaptureGameSyncOrAsync(cmd, stem + ".png", maxEdge, "game");
            }

            if (view == "both")
            {
                var sceneAbs = Path.Combine(_capturesDir, stem + "_scene.png");
                string scenePath = null;
                string sceneErr = null;
                int sceneW = 0, sceneH = 0;
                FocusSceneView();
                var svWin = SceneView.lastActiveSceneView as EditorWindow;
                if (svWin != null &&
                    TryCaptureEditorWindowDesktop(svWin, sceneAbs, maxEdge, 39f, out sceneW, out sceneH, out sceneErr))
                {
                    scenePath = RelFromProject(sceneAbs);
                }
                else
                {
                    scenePath = SaveCameraCapture(
                        SceneView.lastActiveSceneView != null ? SceneView.lastActiveSceneView.camera : null,
                        sceneAbs, maxEdge, out sceneErr, out sceneW, out sceneH);
                }

                var gameResult = CaptureGamePreferDesktop(cmd, stem + "_game.png", maxEdge, "both");
                if (gameResult == null)
                {
                    _captureView = "both";
                    _captureRelPath = RelFromProject(Path.Combine(_capturesDir, stem + "_game.png"));
                    return null;
                }

                var sbBoth = new StringBuilder(256);
                sbBoth.Append('{');
                AppendKv(sbBoth, "view", "both", true);
                AppendKv(sbBoth, "scenePath", scenePath ?? "");
                AppendKv(sbBoth, "sceneError", sceneErr ?? "");
                AppendKv(sbBoth, "sceneW", sceneW);
                AppendKv(sbBoth, "sceneH", sceneH);
                if (gameResult.ok && !string.IsNullOrEmpty(gameResult.data))
                {
                    AppendCommaIfNeeded(sbBoth);
                    sbBoth.Append("\"game\":").Append(gameResult.data);
                }
                else
                {
                    AppendKv(sbBoth, "gameError", gameResult.error ?? "game capture failed");
                }

                sbBoth.Append('}');
                var ok = !string.IsNullOrEmpty(scenePath) || gameResult.ok;
                return new ResponseEnvelope
                {
                    id = cmd.id,
                    ok = ok,
                    cmd = cmd.cmd,
                    error = ok ? null : (sceneErr ?? gameResult.error),
                    data = sbBoth.ToString()
                };
            }

            // camera — force Unity render path (no OS blit)
            return CaptureGameSyncOrAsync(cmd, stem + ".png", maxEdge, "camera");
        }

        private static ResponseEnvelope CaptureGamePreferDesktop(
            CommandEnvelope cmd, string fileName, int maxEdge, string viewLabel)
        {
            FocusGameView();
            var abs = Path.Combine(_capturesDir, fileName);
            var win = GetGameViewWindow();
            if (win != null &&
                TryCaptureEditorWindowDesktop(win, abs, maxEdge, 39f, out var w, out var h, out _))
            {
                return Ok(cmd, CaptureMetaJson(viewLabel, RelFromProject(abs), w, h, false, "desktop"));
            }

            return CaptureGameSyncOrAsync(cmd, fileName, maxEdge, viewLabel);
        }

        private static ResponseEnvelope CaptureGameSyncOrAsync(
            CommandEnvelope cmd, string fileName, int maxEdge, string viewLabel)
        {
            FocusGameView();
            var abs = Path.Combine(_capturesDir, fileName);

            if (EditorApplication.isPlaying && !EditorApplication.isPaused)
            {
                if (_captureInFlight)
                {
                    // Stale lock from a previous timed-out attempt
                    if (EditorApplication.timeSinceStartup - _captureStartedAt > 2.0)
                        ResetCaptureState("stale-lock");
                    else
                    {
                        return new ResponseEnvelope
                        {
                            id = cmd.id,
                            ok = false,
                            cmd = cmd.cmd,
                            error = "Capture already in progress"
                        };
                    }
                }

                _captureInFlight = true;
                _captureCmdId = cmd.id;
                _captureCmdName = cmd.cmd;
                _captureAbsPath = abs;
                _captureRelPath = RelFromProject(abs);
                _captureView = viewLabel;
                _captureMaxEdge = maxEdge;
                _captureStartedAt = EditorApplication.timeSinceStartup;

                // Re-subscribe each time — Enter Play Mode without domain reload
                // replaces the runtime static event instance.
                CursorUnityCaptureRunner.Completed -= OnCaptureCompleted;
                CursorUnityCaptureRunner.Completed += OnCaptureCompleted;
                _captureHooked = true;

                var go = new GameObject("~CursorUnityCapture");
                var runner = go.AddComponent<CursorUnityCaptureRunner>();
                runner.OnDone = OnCaptureCompleted;
                runner.Begin(cmd.id, abs);
                return null;
            }

            return CaptureGameCameraSync(cmd, abs, maxEdge, viewLabel);
        }

        private static ResponseEnvelope CaptureGameCameraSync(
            CommandEnvelope cmd, string abs, int maxEdge, string viewLabel)
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null)
            {
                var cams = UnityEngine.Object.FindObjectsByType<UnityEngine.Camera>(
                    FindObjectsInactive.Exclude, FindObjectsSortMode.None);
                foreach (var c in cams)
                {
                    if (c != null && c.enabled && c.gameObject.activeInHierarchy)
                    {
                        cam = c;
                        break;
                    }
                }
            }

            if (cam == null)
            {
                return new ResponseEnvelope
                {
                    id = cmd.id,
                    ok = false,
                    cmd = cmd.cmd,
                    error = "No camera found for game capture (enter Play for full Game View + Overlay UI)"
                };
            }

            // Overlay canvases are invisible to Camera.Render — temporarily parent them
            // to the game camera so HUD chrome appears in sync captures.
            var patched = new List<(Canvas canvas, RenderMode mode, UnityEngine.Camera worldCam, float plane)>();
            var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                if (canvas == null || canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    continue;
                patched.Add((canvas, canvas.renderMode, canvas.worldCamera, canvas.planeDistance));
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 0.5f;
            }

            string saved;
            string err;
            int w;
            int h;
            try
            {
                saved = SaveCameraCapture(cam, abs, maxEdge, out err, out w, out h);
            }
            finally
            {
                foreach (var p in patched)
                {
                    if (p.canvas == null) continue;
                    p.canvas.renderMode = p.mode;
                    p.canvas.worldCamera = p.worldCam;
                    p.canvas.planeDistance = p.plane;
                }
            }

            if (saved == null)
            {
                return new ResponseEnvelope
                {
                    id = cmd.id,
                    ok = false,
                    cmd = cmd.cmd,
                    error = err ?? "Game camera capture failed"
                };
            }

            // cameraFallback false when we successfully patched overlay canvases in
            return Ok(cmd, CaptureMetaJson(viewLabel, saved, w, h, patched.Count == 0));
        }

        private static void FinishCaptureWithCameraFallback(string reason)
        {
            if (!_captureInFlight)
                return;

            var id = _captureCmdId;
            var cmdName = _captureCmdName;
            var abs = _captureAbsPath;
            var view = _captureView;
            var maxEdge = _captureMaxEdge > 0 ? _captureMaxEdge : 720;
            _captureInFlight = false;
            _captureCmdId = null;
            _captureCmdName = null;
            _captureAbsPath = null;

            var envelope = new CommandEnvelope { id = id, cmd = cmdName };
            var result = CaptureGameCameraSync(envelope, abs, maxEdge, view ?? "game");
            if (result.ok && !string.IsNullOrEmpty(result.data))
            {
                // Annotate fallback reason inside data if possible
                result.data = result.data.TrimEnd('}') + ",\"fallbackReason\":\"" + Esc(reason) + "\"}";
            }
            else if (!result.ok)
            {
                result.error = (result.error ?? "fallback failed") + " (" + reason + ")";
            }

            WriteOut(result);

            // Clean up any leftover runner
            var leftover = GameObject.Find("~CursorUnityCapture");
            if (leftover != null)
                UnityEngine.Object.DestroyImmediate(leftover);
        }

        private static void OnCaptureCompleted(string requestId, byte[] png, string error)
        {
            if (!_captureInFlight || requestId != _captureCmdId)
                return;

            ResponseEnvelope response;
            try
            {
                if (!string.IsNullOrEmpty(error) || png == null || png.Length == 0)
                {
                    response = new ResponseEnvelope
                    {
                        id = _captureCmdId,
                        ok = false,
                        cmd = _captureCmdName,
                        error = error ?? "Empty capture"
                    };
                }
                else
                {
                    EnsureBridgeDir();
                    File.WriteAllBytes(_captureAbsPath, png);
                    var w = 0;
                    var h = 0;
                    TryReadPngSize(png, out w, out h);
                    response = new ResponseEnvelope
                    {
                        id = _captureCmdId,
                        ok = true,
                        cmd = _captureCmdName,
                        data = CaptureMetaJson(_captureView, RelFromProject(_captureAbsPath), w, h, false)
                    };
                }
            }
            catch (Exception ex)
            {
                response = new ResponseEnvelope
                {
                    id = _captureCmdId,
                    ok = false,
                    cmd = _captureCmdName,
                    error = Truncate(ex.GetType().Name + ": " + ex.Message, 300)
                };
            }
            finally
            {
                _captureInFlight = false;
                _captureCmdId = null;
                _captureCmdName = null;
                _captureAbsPath = null;
            }

            WriteOut(response);
        }

        private static string CaptureMetaJson(string view, string relPath, int w, int h, bool cameraFallback, string method = null)
        {
            var sb = new StringBuilder(192);
            sb.Append('{');
            AppendKv(sb, "view", view ?? "", true);
            AppendKv(sb, "path", relPath ?? "");
            AppendKv(sb, "width", w);
            AppendKv(sb, "height", h);
            AppendKv(sb, "cameraFallback", cameraFallback);
            AppendKv(sb, "method", method ?? (cameraFallback ? "camera" : "screenshot"));
            sb.Append('}');
            return sb.ToString();
        }

        private static EditorWindow GetGameViewWindow()
        {
            var T = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            return T != null ? EditorWindow.GetWindow(T) : null;
        }

#if UNITY_EDITOR_WIN
        private static bool TryCaptureEditorWindowDesktop(
            EditorWindow window, string absPath, int maxEdge, float insetTopPoints,
            out int width, out int height, out string error)
        {
            width = 0;
            height = 0;
            error = null;
            if (window == null)
            {
                error = "EditorWindow is null";
                return false;
            }

            try
            {
                window.Focus();
                window.Repaint();
                // Give the editor a beat to present the focused pane before BitBlt
                System.Threading.Thread.Sleep(50);

                var scale = Mathf.Max(0.5f, EditorGUIUtility.pixelsPerPoint);
                var r = window.position;
                var inset = Mathf.Max(0f, insetTopPoints) * scale;
                var x = Mathf.RoundToInt(r.x * scale);
                var y = Mathf.RoundToInt(r.y * scale + inset);
                var w = Mathf.Max(1, Mathf.RoundToInt(r.width * scale));
                var h = Mathf.Max(1, Mathf.RoundToInt(r.height * scale - inset));
                if (w < 8 || h < 8)
                {
                    error = $"Window rect too small ({w}x{h})";
                    return false;
                }

                // BitBlt after raising Unity — PrintWindow crop is unreliable for docked panes.
                BringWindowToTop(ProcessMainHwnd());
                SetForegroundWindow(ProcessMainHwnd());
                System.Threading.Thread.Sleep(30);

                if (!CaptureScreenRectToPng(x, y, w, h, absPath, maxEdge, out width, out height, out error))
                    return false;
                return true;
            }
            catch (Exception ex)
            {
                error = Truncate(ex.GetType().Name + ": " + ex.Message, 240);
                return false;
            }
        }

        private static bool CaptureScreenRectToPng(
            int x, int y, int srcW, int srcH, string absPath, int maxEdge,
            out int outW, out int outH, out string error)
        {
            outW = 0;
            outH = 0;
            error = null;

            IntPtr hdcScreen = IntPtr.Zero;
            IntPtr hdcMem = IntPtr.Zero;
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr hOld = IntPtr.Zero;

            try
            {
                hdcScreen = GetDC(IntPtr.Zero);
                if (hdcScreen == IntPtr.Zero)
                {
                    error = "GetDC failed";
                    return false;
                }

                hdcMem = CreateCompatibleDC(hdcScreen);
                hBitmap = CreateCompatibleBitmap(hdcScreen, srcW, srcH);
                if (hdcMem == IntPtr.Zero || hBitmap == IntPtr.Zero)
                {
                    error = "CreateCompatibleDC/Bitmap failed";
                    return false;
                }

                hOld = SelectObject(hdcMem, hBitmap);
                if (!BitBlt(hdcMem, 0, 0, srcW, srcH, hdcScreen, x, y, SRCCOPY))
                {
                    error = "BitBlt failed";
                    return false;
                }

                return EncodeHdcBitmapToPng(hdcMem, hBitmap, srcW, srcH, absPath, maxEdge, out outW, out outH, out error);
            }
            catch (Exception ex)
            {
                error = Truncate(ex.GetType().Name + ": " + ex.Message, 240);
                return false;
            }
            finally
            {
                if (hOld != IntPtr.Zero && hdcMem != IntPtr.Zero)
                    SelectObject(hdcMem, hOld);
                if (hBitmap != IntPtr.Zero)
                    DeleteObject(hBitmap);
                if (hdcMem != IntPtr.Zero)
                    DeleteDC(hdcMem);
                if (hdcScreen != IntPtr.Zero)
                    ReleaseDC(IntPtr.Zero, hdcScreen);
            }
        }

        /// <summary>
        /// Capture via PrintWindow on the Unity process main window, then crop to the
        /// Game/Scene pane rect. Works when Cursor (or another app) covers Unity.
        /// </summary>
        private static bool TryCaptureWindowHwnd(
            EditorWindow window, int paneX, int paneY, int paneW, int paneH,
            string absPath, int maxEdge, float insetTopPx,
            out int width, out int height, out string error)
        {
            width = 0;
            height = 0;
            error = null;
            var hwnd = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            if (hwnd == IntPtr.Zero)
            {
                error = "No Unity MainWindowHandle";
                return false;
            }

            if (!GetWindowRect(hwnd, out var wr))
            {
                error = "GetWindowRect failed";
                return false;
            }

            var winW = Mathf.Max(1, wr.Right - wr.Left);
            var winH = Mathf.Max(1, wr.Bottom - wr.Top);

            IntPtr hdcWin = IntPtr.Zero;
            IntPtr hdcMem = IntPtr.Zero;
            IntPtr hBitmap = IntPtr.Zero;
            IntPtr hOld = IntPtr.Zero;
            try
            {
                hdcWin = GetWindowDC(hwnd);
                hdcMem = CreateCompatibleDC(hdcWin);
                hBitmap = CreateCompatibleBitmap(hdcWin, winW, winH);
                if (hdcWin == IntPtr.Zero || hdcMem == IntPtr.Zero || hBitmap == IntPtr.Zero)
                {
                    error = "PrintWindow DC/Bitmap failed";
                    return false;
                }

                hOld = SelectObject(hdcMem, hBitmap);
                // PW_RENDERFULLCONTENT (2) — better for DWM/composited windows
                if (!PrintWindow(hwnd, hdcMem, 2) && !PrintWindow(hwnd, hdcMem, 0))
                {
                    error = "PrintWindow failed";
                    return false;
                }

                // Crop to editor pane relative to the main window
                var cropX = Mathf.Clamp(paneX - wr.Left, 0, winW - 1);
                var cropY = Mathf.Clamp(paneY - wr.Top, 0, winH - 1);
                var cropW = Mathf.Clamp(paneW, 1, winW - cropX);
                var cropH = Mathf.Clamp(paneH, 1, winH - cropY);

                // Encode full window then crop in software (simpler than BitBlt crop edge cases)
                var fullPath = absPath + ".full.tmp.png";
                if (!EncodeHdcBitmapToPng(hdcMem, hBitmap, winW, winH, fullPath, 0, out _, out _, out error))
                    return false;

                var bytes = File.ReadAllBytes(fullPath);
                try { File.Delete(fullPath); } catch { /* ignore */ }
                var full = new Texture2D(2, 2, TextureFormat.RGB24, false);
                if (!full.LoadImage(bytes))
                {
                    UnityEngine.Object.DestroyImmediate(full);
                    error = "LoadImage of PrintWindow buffer failed";
                    return false;
                }

                // LoadImage may flip; crop using texture coords (bottom-left origin)
                var srcY = Mathf.Clamp(full.height - (cropY + cropH), 0, full.height - 1);
                cropW = Mathf.Min(cropW, full.width - cropX);
                cropH = Mathf.Min(cropH, full.height - srcY);
                if (cropW < 8 || cropH < 8)
                {
                    UnityEngine.Object.DestroyImmediate(full);
                    error = $"Crop too small ({cropW}x{cropH})";
                    return false;
                }

                var pixels = full.GetPixels(cropX, srcY, cropW, cropH);
                UnityEngine.Object.DestroyImmediate(full);

                FitInside(cropW, cropH, maxEdge > 0 ? maxEdge : 4096, out width, out height);
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                if (width == cropW && height == cropH)
                {
                    tex.SetPixels(pixels);
                }
                else
                {
                    for (var py = 0; py < height; py++)
                    for (var px = 0; px < width; px++)
                    {
                        var sx = (px * cropW) / width;
                        var sy = (py * cropH) / height;
                        tex.SetPixel(px, py, pixels[sy * cropW + sx]);
                    }
                }

                tex.Apply(false, false);
                File.WriteAllBytes(absPath, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);
                return true;
            }
            catch (Exception ex)
            {
                error = Truncate(ex.GetType().Name + ": " + ex.Message, 240);
                return false;
            }
            finally
            {
                if (hOld != IntPtr.Zero && hdcMem != IntPtr.Zero)
                    SelectObject(hdcMem, hOld);
                if (hBitmap != IntPtr.Zero)
                    DeleteObject(hBitmap);
                if (hdcMem != IntPtr.Zero)
                    DeleteDC(hdcMem);
                if (hdcWin != IntPtr.Zero)
                    ReleaseDC(hwnd, hdcWin);
            }
        }

        private static bool EncodeHdcBitmapToPng(
            IntPtr hdcMem, IntPtr hBitmap, int srcW, int srcH, string absPath, int maxEdge,
            out int outW, out int outH, out string error)
        {
            outW = 0;
            outH = 0;
            error = null;

            var bmi = new BITMAPINFOHEADER();
            bmi.biSize = Marshal.SizeOf<BITMAPINFOHEADER>();
            bmi.biWidth = srcW;
            bmi.biHeight = -srcH; // top-down
            bmi.biPlanes = 1;
            bmi.biBitCount = 32;
            bmi.biCompression = 0;

            var bgra = new byte[srcW * srcH * 4];
            var handle = GCHandle.Alloc(bgra, GCHandleType.Pinned);
            try
            {
                var got = GetDIBits(hdcMem, hBitmap, 0, (uint)srcH, handle.AddrOfPinnedObject(), ref bmi, DIB_RGB_COLORS);
                if (got == 0)
                {
                    error = "GetDIBits failed";
                    return false;
                }
            }
            finally
            {
                handle.Free();
            }

            var edge = maxEdge > 0 ? maxEdge : Mathf.Max(srcW, srcH);
            FitInside(srcW, srcH, edge, out outW, out outH);
            var tex = new Texture2D(outW, outH, TextureFormat.RGB24, false);
            var pixels = new Color32[outW * outH];
            for (var py = 0; py < outH; py++)
            {
                var srcY = outH == srcH
                    ? (srcH - 1 - py)
                    : (srcH - 1 - (py * srcH) / outH);
                for (var px = 0; px < outW; px++)
                {
                    var srcX = outW == srcW ? px : (px * srcW) / outW;
                    var i = (srcY * srcW + srcX) * 4;
                    pixels[py * outW + px] = new Color32(bgra[i + 2], bgra[i + 1], bgra[i], 255);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            File.WriteAllBytes(absPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            return true;
        }

        private const int SRCCOPY = 0x00CC0020;
        private const int DIB_RGB_COLORS = 0;

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        private static IntPtr ProcessMainHwnd() =>
            System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth, int nHeight,
            IntPtr hObjectSource, int nXSrc, int nYSrc, int dwRop);

        [DllImport("gdi32.dll")]
        private static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint uStartScan, uint cScanLines,
            IntPtr lpvBits, ref BITMAPINFOHEADER lpbi, uint uUsage);

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public int biSize;
            public int biWidth;
            public int biHeight;
            public short biPlanes;
            public short biBitCount;
            public int biCompression;
            public int biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public int biClrUsed;
            public int biClrImportant;
        }
#else
        private static bool TryCaptureEditorWindowDesktop(
            EditorWindow window, string absPath, int maxEdge, float insetTopPoints,
            out int width, out int height, out string error)
        {
            width = 0;
            height = 0;
            error = "Desktop capture requires Windows Editor";
            return false;
        }
#endif


        private static string SaveCameraCapture(
            UnityEngine.Camera cam, string absPath, int maxEdge,
            out string error, out int width, out int height)
        {
            error = null;
            width = 0;
            height = 0;
            if (cam == null)
            {
                error = "Camera is null";
                return null;
            }

            try
            {
                var srcW = Math.Max(1, cam.pixelWidth);
                var srcH = Math.Max(1, cam.pixelHeight);
                if (srcW < 8 || srcH < 8)
                {
                    var gv = GetGameViewSize();
                    srcW = Math.Max(1, (int)gv.x);
                    srcH = Math.Max(1, (int)gv.y);
                }

                FitInside(srcW, srcH, maxEdge, out width, out height);
                var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
                var prevTarget = cam.targetTexture;
                var prevActive = RenderTexture.active;
                var prevFlags = cam.enabled;

                cam.enabled = true;
                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply(false, false);

                cam.targetTexture = prevTarget;
                cam.enabled = prevFlags;
                RenderTexture.active = prevActive;
                RenderTexture.ReleaseTemporary(rt);

                var png = tex.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(tex);
                File.WriteAllBytes(absPath, png);
                return RelFromProject(absPath);
            }
            catch (Exception ex)
            {
                error = Truncate(ex.GetType().Name + ": " + ex.Message, 240);
                return null;
            }
        }

        private static ResponseEnvelope Click(CommandEnvelope cmd)
        {
            if (!EditorApplication.isPlaying)
            {
                return new ResponseEnvelope
                {
                    id = cmd.id,
                    ok = false,
                    cmd = cmd.cmd,
                    error = "click requires Play Mode"
                };
            }

            FocusGameView();
            var gv = GetGameViewSize();
            var gw = Mathf.Max(1f, gv.x);
            var gh = Mathf.Max(1f, gv.y);

            float x = cmd.x;
            float y = cmd.y;
            if (cmd.normalized != 0)
            {
                x *= gw;
                y *= gh;
            }

            // Unity screen space is bottom-left origin. Optional top-left via filter=topleft
            var origin = (cmd.filter ?? "").Trim().ToLowerInvariant();
            if (origin is "topleft" or "top-left" or "tl")
                y = gh - y;

            x = Mathf.Clamp(x, 0f, gw);
            y = Mathf.Clamp(y, 0f, gh);
            var screenPos = new Vector2(x, y);
            var mode = (cmd.type ?? cmd.view ?? "auto").Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(mode) || mode == "click" || mode == "tap")
                mode = "auto";

            string hitKind = "none";
            string hitName = "";
            string hitPath = "";

            if (mode is "auto" or "ui")
            {
                var es = EventSystem.current;
                if (es != null)
                {
                    var ped = new PointerEventData(es)
                    {
                        position = screenPos,
                        button = PointerEventData.InputButton.Left
                    };
                    var results = new List<RaycastResult>(8);
                    es.RaycastAll(ped, results);
                    if (results.Count > 0)
                    {
                        var target = results[0].gameObject;
                        ExecuteEvents.ExecuteHierarchy(target, ped, ExecuteEvents.pointerDownHandler);
                        ExecuteEvents.ExecuteHierarchy(target, ped, ExecuteEvents.pointerClickHandler);
                        ExecuteEvents.ExecuteHierarchy(target, ped, ExecuteEvents.pointerUpHandler);
                        hitKind = "ui";
                        hitName = target.name;
                        hitPath = GetPath(target.transform);
                    }
                    else if (mode == "ui")
                    {
                        return new ResponseEnvelope
                        {
                            id = cmd.id,
                            ok = false,
                            cmd = cmd.cmd,
                            error = $"No UI hit at ({x:0.#},{y:0.#})"
                        };
                    }
                }
                else if (mode == "ui")
                {
                    return new ResponseEnvelope
                    {
                        id = cmd.id,
                        ok = false,
                        cmd = cmd.cmd,
                        error = "No EventSystem in scene"
                    };
                }
            }

            if (hitKind == "none" && mode is "auto" or "world")
            {
                var cam = UnityEngine.Camera.main;
                if (cam == null)
                {
                    if (mode == "world")
                    {
                        return new ResponseEnvelope
                        {
                            id = cmd.id,
                            ok = false,
                            cmd = cmd.cmd,
                            error = "No Main Camera for world click"
                        };
                    }
                }
                else
                {
                    var ray = cam.ScreenPointToRay(screenPos);
                    if (Physics.Raycast(ray, out var hit, 500f))
                    {
                        hitKind = "world";
                        hitName = hit.collider.gameObject.name;
                        hitPath = GetPath(hit.collider.transform);
                        hit.collider.SendMessage("OnMouseDown", SendMessageOptions.DontRequireReceiver);
                        hit.collider.SendMessage("OnMouseUpAsButton", SendMessageOptions.DontRequireReceiver);
                        hit.collider.SendMessage("OnMouseUp", SendMessageOptions.DontRequireReceiver);
                    }
                    else if (mode == "world")
                    {
                        return new ResponseEnvelope
                        {
                            id = cmd.id,
                            ok = false,
                            cmd = cmd.cmd,
                            error = $"No physics hit at ({x:0.#},{y:0.#})"
                        };
                    }
                }
            }

            var sb = new StringBuilder(192);
            sb.Append('{');
            AppendKv(sb, "x", (int)x, true);
            AppendKv(sb, "y", (int)y);
            AppendKv(sb, "gameViewW", (int)gw);
            AppendKv(sb, "gameViewH", (int)gh);
            AppendKv(sb, "mode", mode);
            AppendKv(sb, "hit", hitKind);
            AppendKv(sb, "name", hitName);
            AppendKv(sb, "path", hitPath);
            sb.Append('}');

            if (hitKind == "none")
            {
                return new ResponseEnvelope
                {
                    id = cmd.id,
                    ok = false,
                    cmd = cmd.cmd,
                    error = $"Nothing hit at ({x:0.#},{y:0.#})",
                    data = sb.ToString()
                };
            }

            return Ok(cmd, sb.ToString());
        }

        private static ResponseEnvelope PressUi(CommandEnvelope cmd)
        {
            if (!EditorApplication.isPlaying)
            {
                return new ResponseEnvelope
                {
                    id = cmd.id,
                    ok = false,
                    cmd = cmd.cmd,
                    error = "press requires Play Mode"
                };
            }

            var needle = (cmd.name ?? cmd.query ?? "").Trim();
            if (string.IsNullOrEmpty(needle))
            {
                return new ResponseEnvelope
                {
                    id = cmd.id,
                    ok = false,
                    cmd = cmd.cmd,
                    error = "press requires -Name (button / UI control)"
                };
            }

            Button bestBtn = null;
            var bestScore = int.MaxValue;
            foreach (var btn in Resources.FindObjectsOfTypeAll<Button>())
            {
                if (btn == null || !btn.gameObject.scene.IsValid())
                    continue;
                var n = btn.gameObject.name;
                if (n.IndexOf(needle, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                var score = n.Equals(needle, StringComparison.OrdinalIgnoreCase) ? 0 : n.Length;
                if (score >= bestScore)
                    continue;
                bestScore = score;
                bestBtn = btn;
            }

            if (bestBtn == null)
            {
                return new ResponseEnvelope
                {
                    id = cmd.id,
                    ok = false,
                    cmd = cmd.cmd,
                    error = $"No Button matching '{needle}'"
                };
            }

            if (!bestBtn.gameObject.activeInHierarchy)
            {
                return new ResponseEnvelope
                {
                    id = cmd.id,
                    ok = false,
                    cmd = cmd.cmd,
                    error = $"Button '{bestBtn.name}' is inactive",
                    data = $"{{\"name\":\"{Esc(bestBtn.name)}\",\"path\":\"{Esc(GetPath(bestBtn.transform))}\"}}"
                };
            }

            bestBtn.onClick.Invoke();
            var sb = new StringBuilder(160);
            sb.Append('{');
            AppendKv(sb, "name", bestBtn.name, true);
            AppendKv(sb, "path", GetPath(bestBtn.transform));
            AppendKv(sb, "interactable", bestBtn.interactable);
            sb.Append('}');
            return Ok(cmd, sb.ToString());
        }

        private static ResponseEnvelope FocusView(CommandEnvelope cmd)
        {
            var view = (cmd.view ?? cmd.type ?? cmd.name ?? "game").Trim().ToLowerInvariant();
            if (view is "scene" or "sceneview")
            {
                FocusSceneView();
                return Ok(cmd, "{\"view\":\"scene\"}");
            }

            FocusGameView();
            return Ok(cmd, "{\"view\":\"game\"}");
        }

        private static ResponseEnvelope SelectObject(CommandEnvelope cmd)
        {
            var objectName = cmd.name ?? cmd.query ?? "";
            if (string.IsNullOrEmpty(objectName))
            {
                return new ResponseEnvelope
                {
                    id = cmd.id,
                    ok = false,
                    cmd = cmd.cmd,
                    error = "Provide name"
                };
            }

            GameObject go = null;
            var all = UnityEngine.Object.FindObjectsByType<GameObject>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var candidate in all)
            {
                if (candidate.name.IndexOf(objectName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    go = candidate;
                    break;
                }
            }

            if (go == null)
            {
                return new ResponseEnvelope
                {
                    id = cmd.id,
                    ok = false,
                    cmd = cmd.cmd,
                    error = "GameObject not found: " + objectName
                };
            }

            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            var sb = new StringBuilder(128);
            sb.Append('{');
            AppendKv(sb, "name", go.name, true);
            AppendKv(sb, "path", GetPath(go.transform));
            sb.Append('}');
            return Ok(cmd, sb.ToString());
        }

        private static ResponseEnvelope GameViewInfo(CommandEnvelope cmd)
        {
            var gv = GetGameViewSize();
            var sb = new StringBuilder(128);
            sb.Append('{');
            AppendKv(sb, "width", (int)gv.x, true);
            AppendKv(sb, "height", (int)gv.y);
            AppendKv(sb, "playMode", EditorApplication.isPlaying);
            AppendKv(sb, "paused", EditorApplication.isPaused);
            sb.Append('}');
            return Ok(cmd, sb.ToString());
        }

        private static ResponseEnvelope SetPlay(CommandEnvelope cmd)
        {
            var mode = (cmd.name ?? cmd.query ?? cmd.type ?? "toggle").Trim().ToLowerInvariant();
            bool next;
            if (mode is "start" or "on" or "1" or "true" or "play")
                next = true;
            else if (mode is "stop" or "off" or "0" or "false" or "edit")
                next = false;
            else
                next = !EditorApplication.isPlaying;

            if (EditorApplication.isPlaying != next)
                EditorApplication.isPlaying = next;

            var sb = new StringBuilder(96);
            sb.Append('{');
            AppendKv(sb, "playMode", next, true);
            AppendKv(sb, "requested", mode);
            sb.Append('}');
            return Ok(cmd, sb.ToString());
        }

        private static ResponseEnvelope SetGameViewSize(CommandEnvelope cmd)
        {
            var w = cmd.limit > 0 ? cmd.limit : Mathf.RoundToInt(cmd.x);
            var h = cmd.depth > 0 ? cmd.depth : Mathf.RoundToInt(cmd.y);
            if (w <= 0) w = 480;
            if (h <= 0) h = 720;
            w = Clamp(w, 160, 3840);
            h = Clamp(h, 160, 2160);

            var ok = TrySetGameViewResolution(w, h, out var err);
            var gv = GetGameViewSize();
            var sb = new StringBuilder(128);
            sb.Append('{');
            AppendKv(sb, "requestedW", w, true);
            AppendKv(sb, "requestedH", h);
            AppendKv(sb, "width", (int)gv.x);
            AppendKv(sb, "height", (int)gv.y);
            AppendKv(sb, "applied", ok);
            if (!string.IsNullOrEmpty(err))
                AppendKv(sb, "note", err);
            sb.Append('}');
            return ok
                ? Ok(cmd, sb.ToString())
                : new ResponseEnvelope
                {
                    id = cmd.id,
                    ok = false,
                    cmd = cmd.cmd,
                    error = err ?? "Failed to set Game View size",
                    data = sb.ToString()
                };
        }

        private static ResponseEnvelope RebuildHud(CommandEnvelope cmd)
        {
            if (EditorApplication.isPlaying)
            {
                return new ResponseEnvelope
                {
                    id = cmd.id,
                    ok = false,
                    cmd = cmd.cmd,
                    error = "Exit Play Mode before rebuild-hud"
                };
            }

            var ok = CozyHudBuilder.ApplyCozyHudToOpenScene();
            if (!ok)
            {
                return new ResponseEnvelope
                {
                    id = cmd.id,
                    ok = false,
                    cmd = cmd.cmd,
                    error = "ApplyCozyHudToOpenScene failed (need === SYSTEMS === / === UI ===)"
                };
            }

            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            var saved = EditorSceneManager.SaveScene(scene);
            var sb = new StringBuilder(96);
            sb.Append('{');
            AppendKv(sb, "saved", saved, true);
            AppendKv(sb, "scene", scene.path);
            sb.Append('}');
            return Ok(cmd, sb.ToString());
        }

        private static ResponseEnvelope RefreshAssets(CommandEnvelope cmd)
        {
            if (EditorApplication.isPlaying)
            {
                return new ResponseEnvelope
                {
                    id = cmd.id,
                    ok = false,
                    cmd = cmd.cmd,
                    error = "Exit Play Mode before refresh"
                };
            }

            var mode = (cmd.name ?? cmd.type ?? cmd.query ?? "default").Trim().ToLowerInvariant();
            var force = mode is "force" or "hard" or "all";
            var reloadScripts = mode is "scripts" or "reload" or "domain";

            if (force)
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            else
                AssetDatabase.Refresh(ImportAssetOptions.Default);

            if (reloadScripts)
                EditorUtility.RequestScriptReload();

            var sb = new StringBuilder(128);
            sb.Append('{');
            AppendKv(sb, "mode", force ? "force" : (reloadScripts ? "scripts" : "default"), true);
            AppendKv(sb, "force", force);
            AppendKv(sb, "scriptReload", reloadScripts);
            AppendKv(sb, "compiling", EditorApplication.isCompiling);
            sb.Append('}');
            return Ok(cmd, sb.ToString());
        }

        private static bool TrySetGameViewResolution(int width, int height, out string error)
        {
            error = null;
            try
            {
                var editorAsm = typeof(EditorWindow).Assembly;
                var sizesType = editorAsm.GetType("UnityEditor.GameViewSizes");
                var singletonType = editorAsm.GetType("UnityEditor.ScriptableSingleton`1")
                    ?.MakeGenericType(sizesType);
                var instanceProp = singletonType?.GetProperty("instance");
                var sizes = instanceProp?.GetValue(null, null);
                if (sizes == null)
                {
                    error = "GameViewSizes singleton missing";
                    return false;
                }

                var currentGroup = sizesType.GetProperty("currentGroup")?.GetValue(sizes, null);
                if (currentGroup == null)
                {
                    error = "currentGroup missing";
                    return false;
                }

                var groupType = currentGroup.GetType();
                var getTotalCount = groupType.GetMethod("GetTotalCount");
                var getGameViewSize = groupType.GetMethod("GetGameViewSize");
                var addCustomSize = groupType.GetMethod("AddCustomSize");
                int total = (int)getTotalCount.Invoke(currentGroup, null);
                int index = -1;
                for (int i = 0; i < total; i++)
                {
                    var size = getGameViewSize.Invoke(currentGroup, new object[] { i });
                    var sizeType = size.GetType();
                    var w = (int)sizeType.GetProperty("width").GetValue(size, null);
                    var h = (int)sizeType.GetProperty("height").GetValue(size, null);
                    if (w == width && h == height)
                    {
                        index = i;
                        break;
                    }
                }

                if (index < 0)
                {
                    var gvSizeType = editorAsm.GetType("UnityEditor.GameViewSize");
                    var gvSizeTypeEnum = editorAsm.GetType("UnityEditor.GameViewSizeType");
                    var fixedRes = Enum.Parse(gvSizeTypeEnum, "FixedResolution");
                    var ctor = gvSizeType.GetConstructor(new[]
                    {
                        gvSizeTypeEnum, typeof(int), typeof(int), typeof(string)
                    });
                    var label = $"{width}x{height} PolyPets";
                    var custom = ctor.Invoke(new object[] { fixedRes, width, height, label });
                    addCustomSize.Invoke(currentGroup, new[] { custom });
                    total = (int)getTotalCount.Invoke(currentGroup, null);
                    index = total - 1;
                }

                var gameViewType = editorAsm.GetType("UnityEditor.GameView");
                var gameView = EditorWindow.GetWindow(gameViewType);
                var sizeSelection = gameViewType.GetProperty("selectedSizeIndex",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                sizeSelection?.SetValue(gameView, index, null);
                gameViewType.GetMethod("SizeSelectionCallback",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?.Invoke(gameView, new object[] { index, null });
                gameView.Repaint();
                FocusGameView();
                return true;
            }
            catch (Exception ex)
            {
                error = Truncate(ex.GetType().Name + ": " + ex.Message, 200);
                return false;
            }
        }

        private static void FocusGameView()
        {
            var T = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (T == null) return;
            var win = EditorWindow.GetWindow(T);
            if (win != null)
            {
                win.Focus();
                win.Repaint();
            }
        }

        private static void FocusSceneView()
        {
            var sv = SceneView.lastActiveSceneView;
            if (sv == null)
                sv = SceneView.sceneViews.Count > 0 ? SceneView.sceneViews[0] as SceneView : null;
            if (sv != null)
            {
                sv.Focus();
                sv.Repaint();
            }
        }

        private static Vector2 GetGameViewSize()
        {
            try
            {
                var T = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
                if (T != null)
                {
                    var method = T.GetMethod("GetSizeOfMainGameView",
                        BindingFlags.NonPublic | BindingFlags.Static);
                    if (method != null)
                    {
                        var result = method.Invoke(null, null);
                        if (result is Vector2 v)
                            return v;
                    }
                }
            }
            catch
            {
                // fall through
            }

            return new Vector2(480, 720);
        }

        private static string SnapshotSerialized(Component target)
        {
            var so = new SerializedObject(target);
            var it = so.GetIterator();
            var sb = new StringBuilder(256);
            sb.Append('{');
            var first = true;
            var entered = false;
            var fieldCount = 0;
            const int maxFields = 40;

            while (it.NextVisible(!entered))
            {
                entered = true;
                if (it.name == "m_Script")
                    continue;
                if (fieldCount >= maxFields)
                {
                    if (!first) sb.Append(',');
                    AppendKv(sb, "_truncated", true, first);
                    break;
                }

                var key = it.name;
                var val = SerializePropertyValue(it);
                if (!first) sb.Append(',');
                sb.Append('"').Append(Esc(key)).Append("\":").Append(val);
                first = false;
                fieldCount++;
            }

            sb.Append('}');
            return sb.ToString();
        }

        private static string SerializePropertyValue(SerializedProperty p)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                {
                    var obj = p.objectReferenceValue;
                    if (obj == null) return "null";
                    var go = obj as GameObject;
                    var comp = obj as Component;
                    var label = go != null ? go.name : (comp != null ? comp.gameObject.name + ":" + comp.GetType().Name : obj.name);
                    return "\"" + Esc(Truncate(label, MaxStringValueChars)) + "\"";
                }
                case SerializedPropertyType.String:
                    return "\"" + Esc(Truncate(p.stringValue ?? "", MaxStringValueChars)) + "\"";
                case SerializedPropertyType.Integer:
                    return p.intValue.ToString();
                case SerializedPropertyType.Boolean:
                    return p.boolValue ? "true" : "false";
                case SerializedPropertyType.Float:
                    return p.floatValue.ToString("0.###");
                case SerializedPropertyType.Enum:
                    return "\"" + Esc(p.enumDisplayNames[Mathf.Clamp(p.enumValueIndex, 0, p.enumDisplayNames.Length - 1)]) + "\"";
                case SerializedPropertyType.Vector2:
                    return $"{{\"x\":{p.vector2Value.x:0.##},\"y\":{p.vector2Value.y:0.##}}}";
                case SerializedPropertyType.Vector3:
                    return $"{{\"x\":{p.vector3Value.x:0.##},\"y\":{p.vector3Value.y:0.##},\"z\":{p.vector3Value.z:0.##}}}";
                case SerializedPropertyType.Color:
                    var c = p.colorValue;
                    return $"{{\"r\":{c.r:0.##},\"g\":{c.g:0.##},\"b\":{c.b:0.##},\"a\":{c.a:0.##}}}";
                case SerializedPropertyType.ArraySize:
                    return p.intValue.ToString();
                default:
                    return "\"" + Esc(Truncate(p.propertyType.ToString(), 40)) + "\"";
            }
        }

        private static string CompactObject(GameObject go, Type highlightType)
        {
            var sb = new StringBuilder(96);
            sb.Append('{');
            AppendKv(sb, "name", go.name, true);
            AppendKv(sb, "path", Truncate(GetPath(go.transform), 120));
            AppendKv(sb, "active", go.activeInHierarchy);
            if (highlightType != null)
            {
                var c = go.GetComponent(highlightType);
                AppendKv(sb, "hasComponent", c != null);
            }
            sb.Append('}');
            return sb.ToString();
        }

        private static Type FindType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            foreach (var t in TypeCache.GetTypesDerivedFrom<Component>())
            {
                if (t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase))
                    return t;
                if (t.FullName != null &&
                    t.FullName.Equals(typeName, StringComparison.OrdinalIgnoreCase))
                    return t;
            }

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var t = asm.GetType(typeName, false, true);
                    if (t != null)
                        return t;
                }
                catch
                {
                    // ignore
                }
            }

            return null;
        }

        private static string GetPath(Transform t)
        {
            var parts = new List<string>(8);
            while (t != null)
            {
                parts.Add(t.name);
                t = t.parent;
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        private static ResponseEnvelope Ok(CommandEnvelope cmd, string dataJson)
        {
            return new ResponseEnvelope
            {
                id = cmd.id,
                ok = true,
                cmd = cmd.cmd,
                data = dataJson
            };
        }

        private static void WriteOut(ResponseEnvelope response)
        {
            EnsureBridgeDir();
            var sb = new StringBuilder(512);
            sb.Append('{');
            AppendKv(sb, "id", response.id ?? "", true);
            AppendKv(sb, "ok", response.ok);
            AppendKv(sb, "cmd", response.cmd ?? "");
            AppendKv(sb, "ms", response.ms);
            if (!string.IsNullOrEmpty(response.error))
                AppendKv(sb, "error", response.error);
            if (!string.IsNullOrEmpty(response.data))
            {
                AppendCommaIfNeeded(sb);
                sb.Append("\"data\":");
                sb.Append(response.data);
                sb.Append(',');
            }

            AppendKv(sb, "t", DateTime.UtcNow.ToString("o"));
            sb.Append('}');

            var tmp = _outPath + ".tmp";
            File.WriteAllText(tmp, sb.ToString(), Encoding.UTF8);
            if (File.Exists(_outPath))
                File.Delete(_outPath);
            File.Move(tmp, _outPath);
        }

        private static void WriteStatus(string state)
        {
            try
            {
                EnsureBridgeDir();
                var scene = SceneManager.GetActiveScene();
                var sb = new StringBuilder(192);
                sb.Append('{');
                AppendKv(sb, "state", state, true);
                AppendKv(sb, "bridge", BridgeVersion);
                AppendKv(sb, "scene", scene.name);
                AppendKv(sb, "playMode", EditorApplication.isPlaying);
                AppendKv(sb, "compiling", EditorApplication.isCompiling);
                AppendKv(sb, "t", DateTime.UtcNow.ToString("o"));
                sb.Append('}');
                File.WriteAllText(_statusPath, sb.ToString(), Encoding.UTF8);
            }
            catch
            {
                // ignore IO races
            }
        }

        private static void TryDeleteCmd()
        {
            try
            {
                if (File.Exists(_cmdPath))
                    File.Delete(_cmdPath);
            }
            catch
            {
                // leave for next poll if locked
            }
        }

        private static void AppendCommaIfNeeded(StringBuilder sb)
        {
            if (sb.Length > 0 && sb[sb.Length - 1] != '{' && sb[sb.Length - 1] != '[' && sb[sb.Length - 1] != ',')
                sb.Append(',');
        }

        private static void AppendKv(StringBuilder sb, string key, string value, bool first = false)
        {
            AppendCommaIfNeeded(sb);
            sb.Append('"').Append(Esc(key)).Append("\":\"").Append(Esc(value ?? "")).Append('"');
        }

        private static void AppendKv(StringBuilder sb, string key, bool value, bool first = false)
        {
            AppendCommaIfNeeded(sb);
            sb.Append('"').Append(Esc(key)).Append("\":").Append(value ? "true" : "false");
        }

        private static void AppendKv(StringBuilder sb, string key, int value, bool first = false)
        {
            AppendCommaIfNeeded(sb);
            sb.Append('"').Append(Esc(key)).Append("\":").Append(value);
        }

        private static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }

        private static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s ?? "";
            return s.Substring(0, max - 1) + "…";
        }

        private static string FirstStackLine(string stack)
        {
            if (string.IsNullOrEmpty(stack)) return null;
            var idx = stack.IndexOf('\n');
            return idx < 0 ? stack : stack.Substring(0, idx).Trim();
        }

        private static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);

        private static void FitInside(int srcW, int srcH, int maxEdge, out int w, out int h)
        {
            w = srcW;
            h = srcH;
            var edge = Math.Max(w, h);
            if (edge <= maxEdge)
                return;
            var scale = maxEdge / (float)edge;
            w = Math.Max(1, Mathf.RoundToInt(w * scale));
            h = Math.Max(1, Mathf.RoundToInt(h * scale));
        }

        private static string SanitizeFileStem(string raw)
        {
            var s = Path.GetFileNameWithoutExtension(raw.Trim());
            if (string.IsNullOrEmpty(s))
                s = "capture";
            foreach (var c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            return s;
        }

        private static string RelFromProject(string absPath)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            absPath = Path.GetFullPath(absPath);
            if (absPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
            {
                var rel = absPath.Substring(projectRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return rel.Replace('\\', '/');
            }

            return absPath.Replace('\\', '/');
        }

        private static void TryReadPngSize(byte[] png, out int w, out int h)
        {
            w = 0;
            h = 0;
            // IHDR width/height at bytes 16-23
            if (png == null || png.Length < 24)
                return;
            w = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];
            h = (png[20] << 24) | (png[21] << 16) | (png[22] << 8) | png[23];
        }

        [Serializable]
        private class CommandEnvelope
        {
            public string id;
            public string cmd;
            public string name;
            public string query;
            public string type;
            public string component;
            public string filter;
            public string menu;
            public string view;
            public string path;
            public int depth;
            public int limit;
            public float x;
            public float y;
            public int normalized;
        }

        private class ResponseEnvelope
        {
            public string id;
            public bool ok;
            public string cmd;
            public int ms;
            public string error;
            public string data;
        }

        private class LogEntry
        {
            public string t;
            public string type;
            public string msg;
            public string stack;
        }

        [MenuItem("PolyPets/Cursor Bridge/Ping (write status)", priority = 900)]
        private static void MenuPing()
        {
            WriteStatus("ready");
            WriteOut(Ping(new CommandEnvelope { id = "menu-ping", cmd = "ping" }));
            EditorUtility.DisplayDialog("Cursor Bridge", $"Status written.\n{_statusPath}", "OK");
        }

        [MenuItem("PolyPets/Cursor Bridge/Refresh Assets", priority = 902)]
        private static void MenuRefresh()
        {
            AssetDatabase.Refresh(ImportAssetOptions.Default);
            Debug.Log("[CursorUnityBridge] AssetDatabase.Refresh()");
        }

        [MenuItem("PolyPets/Cursor Bridge/Open Bridge Folder", priority = 901)]
        private static void OpenFolder()
        {
            EnsureBridgeDir();
            EditorUtility.RevealInFinder(_bridgeDir);
        }

        [MenuItem("PolyPets/Cursor Bridge/Screenshot Scene View", priority = 910)]
        private static void MenuShotScene()
        {
            WriteOut(Screenshot(new CommandEnvelope { id = "menu-shot-scene", cmd = "screenshot", view = "scene" }));
        }

        [MenuItem("PolyPets/Cursor Bridge/Screenshot Game View", priority = 911)]
        private static void MenuShotGame()
        {
            var r = Screenshot(new CommandEnvelope { id = "menu-shot-game", cmd = "screenshot", view = "game" });
            if (r != null)
                WriteOut(r);
        }
    }
}
#endif

