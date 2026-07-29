using UnityEngine;

namespace PolyPets.UI
{
    /// <summary>
    /// Soft companion chrome — warm milk tea panels, gentle cocoa ink, honey accents.
    /// Tuned for low eye-strain contrast on a small 480×720 desk window.
    /// </summary>
    public static class CozyUiTheme
    {
        // Soft translucent panels (not stark white-on-dark)
        public static readonly Color Parchment = new(0.96f, 0.93f, 0.87f, 0.88f);
        public static readonly Color ParchmentSolid = new(0.96f, 0.93f, 0.87f, 0.98f);
        public static readonly Color CreamChip = new(0.98f, 0.95f, 0.90f, 0.90f);
        // Ink: mid cocoa — readable without harsh black
        public static readonly Color Cocoa = new(0.40f, 0.32f, 0.26f, 1f);
        public static readonly Color CocoaSoft = new(0.50f, 0.42f, 0.35f, 1f);
        public static readonly Color CocoaMuted = new(0.60f, 0.52f, 0.44f, 1f);
        // Accents: desaturated honey / rose (cozy, not neon)
        public static readonly Color Amber = new(0.84f, 0.62f, 0.38f, 1f);
        public static readonly Color Honey = new(0.88f, 0.72f, 0.48f, 1f);
        public static readonly Color Blush = new(0.78f, 0.52f, 0.46f, 1f);
        public static readonly Color Leaf = new(0.50f, 0.62f, 0.44f, 1f);
        public static readonly Color SkySoft = new(0.58f, 0.68f, 0.70f, 1f);
        public static readonly Color MeterTrack = new(0.90f, 0.86f, 0.80f, 1f);
        public static readonly Color OverlayDim = new(0.28f, 0.22f, 0.18f, 0.28f);
        public static readonly Color Shadow = new(0.28f, 0.20f, 0.14f, 0.12f);
        public static readonly Color InputWell = new(0.99f, 0.97f, 0.94f, 1f);
        public static readonly Color BorderSoft = new(0.78f, 0.70f, 0.60f, 0.40f);

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

        /// <summary>
        /// Side-view fishing rod: cork grip, spinning reel + crank, tapered blank, tip eye.
        /// Pivot at the grip (top) so it can hang/angle over water.
        /// </summary>
        public static Sprite CreateFishingRodSprite(int width = 96, int height = 240)
        {
            width = Mathf.Max(width, 64);
            height = Mathf.Max(height, 160);
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "CozyFishingRod"
            };
            var clear = new Color(0f, 0f, 0f, 0f);
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = clear;

            var blank = new Color(0.38f, 0.24f, 0.14f, 1f);
            var blankLight = new Color(0.52f, 0.34f, 0.18f, 1f);
            var cork = new Color(0.72f, 0.52f, 0.28f, 1f);
            var corkDark = new Color(0.55f, 0.38f, 0.20f, 1f);
            var metal = new Color(0.28f, 0.30f, 0.34f, 1f);
            var metalHi = new Color(0.55f, 0.58f, 0.62f, 1f);

            float w = width;
            float h = height;
            float axisX = w * 0.42f;

            // Tapered blank — thick near grip (top), thin at tip (bottom)
            for (int y = 0; y < height; y++)
            {
                // y=0 is texture bottom (tip); y=height-1 is top (grip)
                float along = (y + 0.5f) / h; // 0 tip → 1 grip
                float halfThick = Mathf.Lerp(1.2f, 5.5f, along);
                float cx = axisX;
                for (int x = 0; x < width; x++)
                {
                    float dx = Mathf.Abs(x + 0.5f - cx);
                    if (dx > halfThick + 0.6f) continue;
                    float a = Mathf.Clamp01((halfThick + 0.5f - dx) * 2.2f);
                    var c = Color.Lerp(blank, blankLight, (halfThick - dx) / Mathf.Max(0.01f, halfThick));
                    c.a *= a;
                    Stamp(pixels, width, x, y, c);
                }
            }

            // Cork grip bands near top
            FillRect(pixels, width, height, axisX - 7f, h * 0.78f, 14f, h * 0.18f, cork);
            FillRect(pixels, width, height, axisX - 7f, h * 0.82f, 14f, 2.2f, corkDark);
            FillRect(pixels, width, height, axisX - 7f, h * 0.88f, 14f, 2.2f, corkDark);
            FillRect(pixels, width, height, axisX - 7f, h * 0.94f, 14f, 2.2f, corkDark);

            // Spinning reel body (right of grip)
            float reelCx = axisX + 16f;
            float reelCy = h * 0.72f;
            FillEllipse(pixels, width, height, reelCx, reelCy, 11f, 11f, metal);
            FillEllipse(pixels, width, height, reelCx, reelCy, 7f, 7f, metalHi);
            FillEllipse(pixels, width, height, reelCx, reelCy, 4f, 4f, metal);
            // Spool line wrap
            FillEllipse(pixels, width, height, reelCx, reelCy, 6.5f, 6.5f, new Color(0.85f, 0.88f, 0.9f, 0.55f));
            FillEllipse(pixels, width, height, reelCx, reelCy, 3.5f, 3.5f, metal);
            // Crank arm + knob
            FillRect(pixels, width, height, reelCx + 6f, reelCy - 1.5f, 14f, 3f, metal);
            FillEllipse(pixels, width, height, reelCx + 20f, reelCy, 4.5f, 4.5f, metalHi);

            // Guide rings along blank
            for (int i = 0; i < 4; i++)
            {
                float along = 0.12f + i * 0.16f;
                float yy = along * h;
                float halfThick = Mathf.Lerp(1.2f, 5.5f, along);
                FillEllipse(pixels, width, height, axisX, yy, halfThick + 2.2f, 1.6f, metalHi);
                FillEllipse(pixels, width, height, axisX, yy, halfThick * 0.55f, 0.7f, clear);
            }

            // Tip eye
            FillEllipse(pixels, width, height, axisX, h * 0.035f, 3.2f, 3.2f, metalHi);
            FillEllipse(pixels, width, height, axisX, h * 0.035f, 1.4f, 1.4f, clear);

            tex.SetPixels(pixels);
            tex.Apply(false, false);
            // Pivot at grip (top-center of texture → Unity UV y=1)
            return Sprite.Create(
                tex,
                new Rect(0, 0, width, height),
                new Vector2(0.42f, 0.92f),
                100f,
                0,
                SpriteMeshType.FullRect);
        }

        /// <summary>Classic red/white teardrop bobber float.</summary>
        public static Sprite CreateBobberSprite(int size = 64)
        {
            size = Mathf.Max(size, 48);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "CozyBobber"
            };
            var clear = new Color(0f, 0f, 0f, 0f);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = clear;

            float s = size;
            var red = new Color(0.92f, 0.28f, 0.22f, 1f);
            var white = new Color(0.98f, 0.96f, 0.94f, 1f);
            var stem = new Color(0.35f, 0.35f, 0.38f, 1f);

            // Body — oval float
            FillEllipse(pixels, size, s * 0.5f, s * 0.48f, s * 0.28f, s * 0.36f, red);
            // White equatorial band
            FillEllipse(pixels, size, s * 0.5f, s * 0.48f, s * 0.29f, s * 0.1f, white);
            // Stem / eyelet on top
            FillRect(pixels, size, size, s * 0.5f - 1.5f, s * 0.78f, 3f, s * 0.14f, stem);
            FillEllipse(pixels, size, s * 0.5f, s * 0.9f, 3.5f, 3.5f, stem);
            FillEllipse(pixels, size, s * 0.5f, s * 0.9f, 1.5f, 1.5f, clear);

            tex.SetPixels(pixels);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static void FillRect(Color[] pixels, int width, int height, float x, float y, float w, float h, Color color)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(x));
            int x1 = Mathf.Min(width - 1, Mathf.CeilToInt(x + w));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(y));
            int y1 = Mathf.Min(height - 1, Mathf.CeilToInt(y + h));
            for (int py = y0; py <= y1; py++)
            for (int px = x0; px <= x1; px++)
                Stamp(pixels, width, px, py, color);
        }

        private static void Stamp(Color[] pixels, int width, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= width) return;
            int i = y * width + x;
            if (i < 0 || i >= pixels.Length) return;
            if (color.a <= 0.001f)
            {
                pixels[i] = color;
                return;
            }

            var dst = pixels[i];
            float outA = color.a + dst.a * (1f - color.a);
            if (outA < 0.001f)
            {
                pixels[i] = new Color(0, 0, 0, 0);
                return;
            }

            pixels[i] = new Color(
                (color.r * color.a + dst.r * dst.a * (1f - color.a)) / outA,
                (color.g * color.a + dst.g * dst.a * (1f - color.a)) / outA,
                (color.b * color.a + dst.b * dst.a * (1f - color.a)) / outA,
                outA);
        }

        private static void FillEllipse(Color[] pixels, int size, float cx, float cy, float rx, float ry, Color color)
        {
            FillEllipse(pixels, size, size, cx, cy, rx, ry, color);
        }

        private static void FillEllipse(Color[] pixels, int width, int height, float cx, float cy, float rx, float ry, Color color)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rx - 1));
            int x1 = Mathf.Min(width - 1, Mathf.CeilToInt(cx + rx + 1));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - ry - 1));
            int y1 = Mathf.Min(height - 1, Mathf.CeilToInt(cy + ry + 1));
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x + 0.5f - cx) / Mathf.Max(0.01f, rx);
                    float dy = (y + 0.5f - cy) / Mathf.Max(0.01f, ry);
                    float d = dx * dx + dy * dy;
                    float a = Mathf.Clamp01((1.05f - d) * 8f);
                    if (a <= 0f) continue;
                    var c = color;
                    c.a *= a;
                    Stamp(pixels, width, x, y, c);
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
