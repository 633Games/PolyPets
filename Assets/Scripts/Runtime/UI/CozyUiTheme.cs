using UnityEngine;

namespace PolyPets.UI
{
    /// <summary>
    /// Soft companion chrome — warm parchment, cocoa ink, amber accents.
    /// </summary>
    public static class CozyUiTheme
    {
        public static readonly Color Parchment = new(0.97f, 0.94f, 0.88f, 0.94f);
        public static readonly Color ParchmentSolid = new(0.97f, 0.94f, 0.88f, 1f);
        public static readonly Color CreamChip = new(1f, 0.97f, 0.92f, 0.92f);
        public static readonly Color Cocoa = new(0.29f, 0.22f, 0.17f, 1f);
        public static readonly Color CocoaSoft = new(0.42f, 0.33f, 0.26f, 1f);
        public static readonly Color CocoaMuted = new(0.55f, 0.45f, 0.36f, 1f);
        public static readonly Color Amber = new(0.91f, 0.64f, 0.32f, 1f);
        public static readonly Color Honey = new(0.93f, 0.74f, 0.42f, 1f);
        public static readonly Color Blush = new(0.86f, 0.52f, 0.42f, 1f);
        public static readonly Color Leaf = new(0.45f, 0.62f, 0.38f, 1f);
        public static readonly Color SkySoft = new(0.55f, 0.68f, 0.72f, 1f);
        public static readonly Color MeterTrack = new(0.88f, 0.82f, 0.74f, 1f);
        public static readonly Color OverlayDim = new(0.22f, 0.16f, 0.12f, 0.35f);
        public static readonly Color Shadow = new(0.22f, 0.14f, 0.1f, 0.18f);
        public static readonly Color InputWell = new(1f, 0.98f, 0.95f, 1f);

        public static Font UiFont =>
            Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
            ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

        /// <summary>
        /// Chamfered round-rect suitable for 9-slice. Keep radius well under size/2 so
        /// stretched panels keep soft corners instead of pinching to points.
        /// </summary>
        public static Sprite CreateRoundedSprite(int size, int radius, Color fill, Color? border = null, int borderWidth = 2)
        {
            size = Mathf.Max(size, radius * 2 + 8);
            radius = Mathf.Clamp(radius, 4, (size / 2) - 4);

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "CozyRound"
            };

            var clear = new Color(0f, 0f, 0f, 0f);
            var edge = border ?? new Color(fill.r * 0.82f, fill.g * 0.78f, fill.b * 0.72f, fill.a);
            var pixels = new Color[size * size];

            float rOuter = radius;
            float rInner = Mathf.Max(0f, radius - borderWidth);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = SignedDistanceRoundRect(x + 0.5f, y + 0.5f, size, size, rOuter);
                    if (dist > 0.5f)
                    {
                        pixels[y * size + x] = clear;
                        continue;
                    }

                    float inner = SignedDistanceRoundRect(x + 0.5f, y + 0.5f, size, size, rInner);
                    float alpha = Mathf.Clamp01(0.5f - dist);
                    Color c = inner < -0.5f ? fill : edge;
                    c.a *= alpha * fill.a;
                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, false);
            // Border must leave a flat center strip; never use radius == size/2.
            float borderPx = radius;
            return Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(borderPx, borderPx, borderPx, borderPx));
        }

        public static Sprite CreateCircleSprite(int size, Color fill, Color? ring = null, int ringWidth = 0)
        {
            size = Mathf.Max(size, 32);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "CozyCircle"
            };

            var clear = new Color(0f, 0f, 0f, 0f);
            var edge = ring ?? fill;
            var pixels = new Color[size * size];
            float cx = size * 0.5f;
            float cy = size * 0.5f;
            float r = size * 0.5f - 1.25f;
            float rInner = ringWidth > 0 ? Mathf.Max(0f, r - ringWidth) : -1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - cx;
                    float dy = y + 0.5f - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float outerA = Mathf.Clamp01(r + 0.5f - d);
                    if (outerA <= 0f)
                    {
                        pixels[y * size + x] = clear;
                        continue;
                    }

                    Color c = fill;
                    if (rInner > 0f)
                    {
                        float hole = Mathf.Clamp01(d - (rInner - 0.5f));
                        // Ring only when ringWidth set and fill is for track ring
                        if (d < rInner - 0.5f)
                            c = clear;
                        else
                            c = Color.Lerp(fill, edge, hole);
                    }

                    c.a *= outerA * fill.a;
                    pixels[y * size + x] = c;
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        public static Sprite CreateBowlIcon(int size, Color ink)
        {
            size = Mathf.Max(size, 48);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "CozyBowl"
            };
            var clear = new Color(0f, 0f, 0f, 0f);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = clear;

            float s = size;
            // Bowl body — wide U shape
            FillEllipse(pixels, size, s * 0.5f, s * 0.58f, s * 0.34f, s * 0.22f, ink);
            // Hollow the top of the bowl
            FillEllipse(pixels, size, s * 0.5f, s * 0.48f, s * 0.28f, s * 0.12f, clear);
            // Rim
            FillEllipse(pixels, size, s * 0.5f, s * 0.42f, s * 0.36f, s * 0.07f, ink);
            FillEllipse(pixels, size, s * 0.5f, s * 0.42f, s * 0.28f, s * 0.04f, clear);
            // Food mound
            FillEllipse(pixels, size, s * 0.5f, s * 0.36f, s * 0.18f, s * 0.1f, ink);

            tex.SetPixels(pixels);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        public static Sprite CreateMoodIcon(int size, Color ink)
        {
            size = Mathf.Max(size, 48);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "CozyMood"
            };
            var clear = new Color(0f, 0f, 0f, 0f);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = clear;

            float s = size;
            // Soft heart via two circles + triangle-ish bottom
            FillEllipse(pixels, size, s * 0.35f, s * 0.38f, s * 0.18f, s * 0.18f, ink);
            FillEllipse(pixels, size, s * 0.65f, s * 0.38f, s * 0.18f, s * 0.18f, ink);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + 0.5f) / s - 0.5f;
                    float ny = (y + 0.5f) / s - 0.5f;
                    // lower diamond / V of heart
                    float v = Mathf.Abs(nx) * 1.15f + (ny - 0.02f) * 0.95f;
                    if (ny > 0.02f && v < 0.42f)
                    {
                        float a = Mathf.Clamp01((0.42f - v) * 12f);
                        var c = ink;
                        c.a *= a;
                        var idx = y * size + x;
                        if (c.a > pixels[idx].a)
                            pixels[idx] = c;
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static void FillEllipse(Color[] pixels, int size, float cx, float cy, float rx, float ry, Color color)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rx - 1));
            int x1 = Mathf.Min(size - 1, Mathf.CeilToInt(cx + rx + 1));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - ry - 1));
            int y1 = Mathf.Min(size - 1, Mathf.CeilToInt(cy + ry + 1));
            float rx2 = rx * rx;
            float ry2 = ry * ry;
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f - cx) / rx;
                    float dy = (y + 0.5f - cy) / ry;
                    float d = dx * dx + dy * dy;
                    float a = Mathf.Clamp01((1.05f - d) * 8f);
                    if (a <= 0f) continue;
                    int i = y * size + x;
                    if (color.a <= 0.001f)
                    {
                        pixels[i] = color;
                        continue;
                    }

                    var c = color;
                    c.a *= a;
                    // alpha over
                    var dst = pixels[i];
                    float outA = c.a + dst.a * (1f - c.a);
                    if (outA < 0.001f)
                    {
                        pixels[i] = new Color(0, 0, 0, 0);
                        continue;
                    }

                    pixels[i] = new Color(
                        (c.r * c.a + dst.r * dst.a * (1f - c.a)) / outA,
                        (c.g * c.a + dst.g * dst.a * (1f - c.a)) / outA,
                        (c.b * c.a + dst.b * dst.a * (1f - c.a)) / outA,
                        outA);
                }
            }
        }

        private static float SignedDistanceRoundRect(float x, float y, float w, float h, float r)
        {
            float cx = Mathf.Clamp(x, r, w - r);
            float cy = Mathf.Clamp(y, r, h - r);
            if (x >= r && x <= w - r && y >= r && y <= h - r)
                return -Mathf.Min(Mathf.Min(x, w - x), Mathf.Min(y, h - y));

            float dx = x - cx;
            float dy = y - cy;
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        }
    }
}
