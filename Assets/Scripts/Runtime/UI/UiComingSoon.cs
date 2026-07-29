using UnityEngine;
using UnityEngine.UI;

namespace PolyPets.UI
{
    /// <summary>
    /// Marks a control as non-interactive for this build — greys it out and shows "Coming soon".
    /// </summary>
    public static class UiComingSoon
    {
        public static void Apply(UiChromeButton chrome, string reason = "Coming soon")
        {
            if (chrome == null)
                return;

            var button = chrome.Button;
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.interactable = false;
            }

            Overlay(chrome.transform as RectTransform, reason);
            DimGraphics(chrome.gameObject);
        }

        public static void Apply(Button button, string reason = "Coming soon")
        {
            if (button == null)
                return;
            button.onClick.RemoveAllListeners();
            button.interactable = false;
            Overlay(button.transform as RectTransform, reason);
            DimGraphics(button.gameObject);
        }

        public static void Apply(GameObject root, string reason = "Coming soon")
        {
            if (root == null)
                return;
            foreach (var btn in root.GetComponentsInChildren<Button>(true))
            {
                btn.onClick.RemoveAllListeners();
                btn.interactable = false;
            }

            Overlay(root.transform as RectTransform, reason);
            DimGraphics(root);
        }

        private static void DimGraphics(GameObject root)
        {
            var badge = root.transform.Find("ComingSoonBadge");
            foreach (var g in root.GetComponentsInChildren<Graphic>(true))
            {
                if (badge != null && (g.transform == badge || g.transform.IsChildOf(badge)))
                    continue;
                var c = g.color;
                g.color = new Color(c.r * 0.65f, c.g * 0.65f, c.b * 0.65f, Mathf.Clamp01(c.a * 0.75f));
            }
        }

        private static void Overlay(RectTransform host, string reason)
        {
            if (host == null)
                return;
            if (host.Find("ComingSoonBadge") != null)
                return;

            var go = new GameObject("ComingSoonBadge", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(host, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = new Color(0.08f, 0.07f, 0.06f, 0.72f);
            img.raycastTarget = true;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(4f, 2f);
            lrt.offsetMax = new Vector2(-4f, -2f);
            var text = labelGo.GetComponent<Text>();
            text.text = reason;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 11;
            text.color = new Color(1f, 0.85f, 0.55f, 1f);
            text.raycastTarget = false;
            UiFonts.ApplyBody(text);
        }
    }
}
