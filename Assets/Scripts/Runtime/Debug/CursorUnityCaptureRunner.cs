using System;
using System.Collections;
using UnityEngine;

namespace PolyPets.DebugTools
{
    /// <summary>
    /// One-shot play-mode helper used by <c>CursorUnityBridge</c> to grab a true Game View
    /// frame (includes Screen Space Overlay UI) after the frame finishes rendering.
    /// </summary>
    [AddComponentMenu("")]
    public sealed class CursorUnityCaptureRunner : MonoBehaviour
    {
        /// <summary>Legacy static hook (domain-reload safe only if re-subscribed each capture).</summary>
        public static event Action<string, byte[], string> Completed;

        /// <summary>Preferred: instance callback set by the editor bridge before Begin.</summary>
        public Action<string, byte[], string> OnDone;

        string _requestId;
        bool _started;

        public void Begin(string requestId, string absolutePathUnused = null)
        {
            _requestId = requestId ?? "";
            if (!_started)
            {
                _started = true;
                StartCoroutine(CaptureCoroutine());
            }
        }

        IEnumerator CaptureCoroutine()
        {
            // A couple of frames is more reliable in Editor Play Mode than
            // WaitForEndOfFrame alone (that wait can stall when Game View is idle).
            yield return null;
            yield return null;
            yield return new WaitForEndOfFrame();

            string error = null;
            byte[] png = null;
            try
            {
                var tex = ScreenCapture.CaptureScreenshotAsTexture();
                if (tex == null)
                {
                    error = "CaptureScreenshotAsTexture returned null";
                }
                else
                {
                    png = tex.EncodeToPNG();
                    Destroy(tex);
                }
            }
            catch (Exception ex)
            {
                error = ex.GetType().Name + ": " + ex.Message;
            }

            try
            {
                // Prefer instance callback only (avoid double-invoke with static event).
                if (OnDone != null)
                    OnDone.Invoke(_requestId, png, error);
                else
                    Completed?.Invoke(_requestId, png, error);
            }
            finally
            {
                Destroy(gameObject);
            }
        }
    }
}
