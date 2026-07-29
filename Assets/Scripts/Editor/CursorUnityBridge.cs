#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PolyPets.EditorTools
{
    /// <summary>
    /// File-based command bridge for Cursor agents.
    /// Watches <c>.cursor/unity/cmd.json</c>, writes compact JSON to <c>out.json</c>.
    /// </summary>
    [InitializeOnLoad]
    public static class CursorUnityBridge
    {
        public const string BridgeVersion = "1.0.0";
        private const float PollSeconds = 0.35f;
        private const int MaxLogEntries = 200;
        private const int DefaultLogLines = 40;
        private const int MaxHierarchyNodes = 120;
        private const int MaxFindResults = 40;
        private const int MaxStringValueChars = 80;

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
            "PolyPets/UI/Build UI Prefab Kit + Sprite Pack",
            "PolyPets/UI/Select Sprite Pack",
            "PolyPets/UI/Apply Vendor Sprite Pack (v1)",
            "PolyPets/Materials/Rebuild Color Palette (25 mats)",
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
        };

        private static string _bridgeDir;
        private static string _cmdPath;
        private static string _outPath;
        private static string _statusPath;
        private static string _lastCmdId;
        private static double _nextPollTime;
        private static bool _hookedLogs;

        static CursorUnityBridge()
        {
            ResolvePaths();
            EnsureBridgeDir();
            HookLogs();
            EditorApplication.update += OnEditorUpdate;
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
        }

        private static void EnsureBridgeDir()
        {
            if (!Directory.Exists(_bridgeDir))
                Directory.CreateDirectory(_bridgeDir);
        }

        private static void HookLogs()
        {
            if (_hookedLogs)
                return;
            Application.logMessageReceivedThreaded += OnLogMessage;
            _hookedLogs = true;
        }

        private static void OnQuitting()
        {
            WriteStatus("quitting");
            if (_hookedLogs)
            {
                Application.logMessageReceivedThreaded -= OnLogMessage;
                _hookedLogs = false;
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
                _ => new ResponseEnvelope
                {
                    id = cmd.id,
                    ok = false,
                    cmd = cmd.cmd,
                    error = "Unknown cmd. Use: ping, hierarchy, find, log, exec, get-component, menus"
                }
            };
        }

        private static ResponseEnvelope Ping(CommandEnvelope cmd)
        {
            var scene = EditorSceneManager.GetActiveScene();
            var sb = new StringBuilder(256);
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
                // Allow any PolyPets/ menu that exists if user passes exact path matching prefix
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
                // Prefer CareHudController when present; else first MonoBehaviour
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

            // Non-component types (rare)
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
            // data is already a JSON object string; embed without re-escaping via manual write
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

        [Serializable]
        private class CommandEnvelope
        {
            public string id;
            public string cmd;
            // Flat args for JsonUtility (no nested objects)
            public string name;
            public string query;
            public string type;
            public string component;
            public string filter;
            public string menu;
            public int depth;
            public int limit;
        }

        private class ResponseEnvelope
        {
            public string id;
            public bool ok;
            public string cmd;
            public int ms;
            public string error;
            public string data; // raw JSON object
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

        [MenuItem("PolyPets/Cursor Bridge/Open Bridge Folder", priority = 901)]
        private static void OpenFolder()
        {
            EnsureBridgeDir();
            EditorUtility.RevealInFinder(_bridgeDir);
        }
    }
}
#endif
