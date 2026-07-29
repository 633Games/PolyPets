using UnityEngine;
using PolyPets.Core;

namespace PolyPets.Desktop
{
    /// <summary>
    /// Desktop companion window defaults for the PC build.
    /// Always-on-top uses a platform plugin when linked; otherwise stores intent safely.
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

        private void Awake()
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            // Keep OS window title on-brand even before Player Settings bake.
            if (!string.IsNullOrEmpty(StudioBrand.WindowTitle))
            {
                // productName drives the window title in standalone builds.
            }
#endif
        }

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
    /// Optional Win32 / Cocoa hooks. Safe no-op until a native plugin is linked.
    /// </summary>
    public static class DesktopNative
    {
        public static bool TrySetAlwaysOnTop(bool enabled)
        {
            Debug.Log($"[PolyPets] Always-on-top requested: {enabled} (native plugin not linked yet)");
            return false;
        }

        public static bool TryRestoreWindowPosition()
        {
            return false;
        }
    }
}
