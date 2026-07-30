#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PolyPets.EditorTools
{
    /// <summary>
    /// Low-token agent bridge: shell/Cursor writes cmd.txt; this polls and writes status.json.
    /// Commands: play | stop | pause | unpause | refresh | ping | setup
    /// </summary>
    [InitializeOnLoad]
    public static class PolyPetsAgentBridge
    {
        private const string FolderName = "PolyPetsAgent";
        private const double PollSeconds = 0.35;

        private static double _nextPoll;
        private static string _lastCmd = "";
        private static string _lastResult = "idle";
        private static string _lastError = "";

        static PolyPetsAgentBridge()
        {
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += _ => WriteStatus();
            EditorApplication.pauseStateChanged += _ => WriteStatus();
            EnsureFolders();
            WriteStatus();
        }

        private static string AgentDir
        {
            get
            {
                var dir = Path.Combine(Directory.GetCurrentDirectory(), "Temp", FolderName);
                return dir;
            }
        }

        private static string CmdPath => Path.Combine(AgentDir, "cmd.txt");
        private static string StatusPath => Path.Combine(AgentDir, "status.json");

        private static void EnsureFolders()
        {
            var dir = AgentDir;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        private static void OnEditorUpdate()
        {
            if (EditorApplication.timeSinceStartup < _nextPoll)
                return;
            _nextPoll = EditorApplication.timeSinceStartup + PollSeconds;

            try
            {
                EnsureFolders();
                if (File.Exists(CmdPath))
                {
                    var raw = File.ReadAllText(CmdPath).Trim();
                    // Consume immediately so the same command is not double-run.
                    File.Delete(CmdPath);
                    if (!string.IsNullOrEmpty(raw))
                        RunCommand(raw);
                }
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                _lastResult = "error";
            }

            WriteStatus();
        }

        private static void RunCommand(string raw)
        {
            var cmd = raw.Split(new[] { ' ', '\t', '\r', '\n' }, 2, StringSplitOptions.RemoveEmptyEntries)[0]
                .Trim()
                .ToLowerInvariant();
            _lastCmd = cmd;
            _lastError = "";

            switch (cmd)
            {
                case "play":
                    if (!EditorApplication.isPlaying)
                        EditorApplication.isPlaying = true;
                    _lastResult = "play";
                    break;
                case "stop":
                    if (EditorApplication.isPlaying)
                        EditorApplication.isPlaying = false;
                    EditorApplication.isPaused = false;
                    _lastResult = "stop";
                    break;
                case "pause":
                    if (EditorApplication.isPlaying)
                        EditorApplication.isPaused = true;
                    _lastResult = "pause";
                    break;
                case "unpause":
                case "resume":
                    EditorApplication.isPaused = false;
                    _lastResult = "unpause";
                    break;
                case "refresh":
                case "reload":
                case "assets":
                    // Same as Assets → Refresh (reimport / pick up disk changes).
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                    EditorApplication.ExecuteMenuItem("Assets/Refresh");
                    _lastResult = "refresh";
                    break;
                case "setup":
                    PolyPetsFirstRun.FirstTimeSetupBatch();
                    _lastResult = "setup";
                    break;
                case "ping":
                    _lastResult = "pong";
                    break;
                case "focus-game":
                    EditorApplication.ExecuteMenuItem("Window/General/Game");
                    _lastResult = "focus-game";
                    break;
                case "focus-console":
                    EditorApplication.ExecuteMenuItem("Window/General/Console");
                    _lastResult = "focus-console";
                    break;
                default:
                    _lastResult = "unknown";
                    _lastError = "unknown cmd: " + cmd;
                    break;
            }
        }

        private static void WriteStatus()
        {
            try
            {
                EnsureFolders();
                var sb = new StringBuilder(256);
                sb.Append('{');
                Append(sb, "playing", EditorApplication.isPlaying);
                sb.Append(',');
                Append(sb, "paused", EditorApplication.isPaused);
                sb.Append(',');
                Append(sb, "compiling", EditorApplication.isCompiling);
                sb.Append(',');
                Append(sb, "updating", EditorApplication.isUpdating);
                sb.Append(',');
                Append(sb, "focused", EditorApplication.isFocused);
                sb.Append(',');
                Append(sb, "lastCmd", _lastCmd);
                sb.Append(',');
                Append(sb, "lastResult", _lastResult);
                sb.Append(',');
                Append(sb, "lastError", _lastError);
                sb.Append(',');
                Append(sb, "scene", UnityEngine.SceneManagement.SceneManager.GetActiveScene().path);
                sb.Append(',');
                Append(sb, "t", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
                sb.Append('}');
                File.WriteAllText(StatusPath, sb.ToString());
            }
            catch
            {
                // Never throw from editor update.
            }
        }

        private static void Append(StringBuilder sb, string key, bool value)
        {
            sb.Append('"').Append(key).Append("\":").Append(value ? "true" : "false");
        }

        private static void Append(StringBuilder sb, string key, string value)
        {
            sb.Append('"').Append(key).Append("\":\"")
                .Append(Escape(value ?? ""))
                .Append('"');
        }

        private static string Escape(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
        }

        [MenuItem("PolyPets/Agent/Write Status Now", priority = 500)]
        private static void MenuWriteStatus()
        {
            WriteStatus();
            EditorUtility.RevealInFinder(AgentDir);
        }

        [MenuItem("PolyPets/Agent/Play", priority = 501)]
        private static void MenuPlay() => RunCommand("play");

        [MenuItem("PolyPets/Agent/Stop", priority = 502)]
        private static void MenuStop() => RunCommand("stop");

        [MenuItem("PolyPets/Agent/Refresh Assets", priority = 503)]
        private static void MenuRefresh() => RunCommand("refresh");
    }
}
#endif
