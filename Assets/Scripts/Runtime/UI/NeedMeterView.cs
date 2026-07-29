using UnityEngine;
using UnityEngine.UI;

namespace PolyPets.UI
{
    /// <summary>
    /// Radial pie need meter — icon in the center, fill amount = satisfaction (0–100).
    /// No numeric readout.
    /// </summary>
    public sealed class NeedMeterView : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Image trackImage;
        [SerializeField] private Image iconImage;
        [SerializeField] private Color fillColor = CozyUiTheme.Amber;
        [SerializeField] private Color emptyColor = CozyUiTheme.MeterTrack;

        public void Bind(Image track, Image fill, Image icon, Color color)
        {
            trackImage = track;
            fillImage = fill;
            iconImage = icon;
            fillColor = color;
            ApplyColors(1f);
        }

        /// <summary>Legacy bar bind — kept so mid-reload editor scripts still compile.</summary>
        public void Bind(Text caption, Text value, Image fill, string captionLabel, Color color)
        {
            fillImage = fill;
            fillColor = color;
            if (fillImage != null)
            {
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Radial360;
                fillImage.fillOrigin = (int)Image.Origin360.Top;
            }
            ApplyColors(1f);
        }

        public void Set(float amount0to100, string statusHint = null)
        {
            float t = Mathf.Clamp01(amount0to100 / 100f);
            if (fillImage != null)
            {
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Radial360;
                fillImage.fillOrigin = (int)Image.Origin360.Top;
                fillImage.fillClockwise = true;
                fillImage.fillAmount = t;
            }

            ApplyColors(t);
        }

        private void ApplyColors(float t)
        {
            if (fillImage != null)
            {
                // Soften when empty so it reads “needs care”
                var c = Color.Lerp(emptyColor, fillColor, Mathf.Lerp(0.35f, 1f, t));
                fillImage.color = c;
            }

            if (iconImage != null)
            {
                var ink = Color.Lerp(CozyUiTheme.CocoaMuted, CozyUiTheme.Cocoa, Mathf.Lerp(0.4f, 1f, t));
                iconImage.color = ink;
            }
        }
    }
}
