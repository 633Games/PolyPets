using UnityEngine;
using UnityEngine.UI;
using PolyPets.Core;

namespace PolyPets.UI
{
    /// <summary>
    /// Lightweight settings stub — Coming soon for features not in this build.
    /// </summary>
    public sealed class SettingsStubPanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text bodyText;

        public bool IsOpen => root != null && root.activeSelf;

        public void Bind(GameObject panelRoot, Text body)
        {
            root = panelRoot;
            bodyText = body;
            Hide();
        }

        public void Toggle()
        {
            if (IsOpen) Hide();
            else Show();
        }

        public void Show()
        {
            if (root != null)
                root.SetActive(true);
            if (bodyText != null)
            {
                bodyText.text =
                    $"{StudioBrand.ProductName}\n{StudioBrand.StudioName}\n\n" +
                    "Settings panel — Coming soon.\n\n" +
                    "• Mute / volume\n" +
                    "• Reduce motion\n" +
                    "• Window position memory\n\n" +
                    "Pin (always on top) works from the top bar.";
                UiFonts.ApplyBody(bodyText);
            }

            PolyPets.Audio.JuicySfx.PlayUiClick();
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
        }
    }
}
