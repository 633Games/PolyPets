using UnityEngine;

namespace PolyPets.Desktop
{
    /// <summary>
    /// Desktop companion window defaults for the PC build.
    /// Always-on-top needs a platform plugin at runtime; this stores intent + safe Player settings helpers.
    /// </summary>
    public sealed class DesktopWindowController : MonoBehaviour
    {
        [Header("Window")]
        [SerializeField] private Vector2Int windowSize = new(480, 720);
        [SerializeField] private bool startInWindowedMode = true;
        [SerializeField] private bool alwaysOnTop = true;
        [SerializeField] private bool rememberPosition = true;

        public Vector2Int WindowSize => windowSize;
        public bool AlwaysOnTop => alwaysOnTop;

        public void ApplyStartupWindowSettings()
        {
            if (!Application.isPlaying)
                return;

#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
            if (startInWindowedMode && Screen.fullScreenMode != FullScreenMode.Windowed)
                Screen.fullScreenMode = FullScreenMode.Windowed;

            if (windowSize.x > 0 && windowSize.y > 0)
                Screen.SetResolution(windowSize.x, windowSize.y, FullScreenMode.Windowed);
#endif

            // Always-on-top is applied by a native helper when available.
            if (alwaysOnTop)
                DesktopNative.TrySetAlwaysOnTop(true);

            if (rememberPosition)
                DesktopNative.TryRestoreWindowPosition();
        }

        public void SetAlwaysOnTop(bool enabled)
        {
            alwaysOnTop = enabled;
            DesktopNative.TrySetAlwaysOnTop(enabled);
        }
    }

    /// <summary>
    /// Placeholder for Win32 / Cocoa hooks. Safe no-op until a native plugin is wired.
    /// </summary>
    public static class DesktopNative
    {
        public static bool TrySetAlwaysOnTop(bool enabled)
        {
            // TODO: P/Invoke SetWindowPos (Windows) / NSWindow.Level (macOS)
            Debug.Log($"[PolyPets] Always-on-top requested: {enabled} (native hook not linked yet)");
            return false;
        }

        public static bool TryRestoreWindowPosition()
        {
            // TODO: read PlayerPrefs and move native window
            return false;
        }
    }
}
