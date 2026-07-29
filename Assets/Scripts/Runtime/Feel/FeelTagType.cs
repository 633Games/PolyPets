using System;
using UnityEngine;

namespace PolyPets.Feel
{
    /// <summary>
    /// Feel-inspired juice tags. Name objects <c>FEEL[Squash]</c> (or add <see cref="FeelTag"/>)
    /// and the binder will attach the matching idle/juice behaviour.
    /// Mirrors More Mountains Feel feedback families where practical:
    /// https://feel-docs.moremountains.com/list_mmfeedbacks.html
    /// </summary>
    public enum FeelTagType
    {
        None = 0,
        /// <summary>Breathing squash &amp; stretch idle (MMF_SquashAndStretch-style, looping).</summary>
        Squash = 1,
        /// <summary>Soft scale punch on enable / Play().</summary>
        Punch = 2,
        /// <summary>Position + rotation wobble idle (MMWiggle-like).</summary>
        Wobble = 3,
        /// <summary>Vertical bounce idle.</summary>
        Bounce = 4,
        /// <summary>One-shot pop-in scale.</summary>
        Pop = 5,
        /// <summary>Light positional shake loop.</summary>
        Shake = 6,
    }

    public static class FeelTagNaming
    {
        public const string Prefix = "FEEL[";
        public const string Suffix = "]";

        public static bool TryParseObjectName(string objectName, out FeelTagType type)
        {
            type = FeelTagType.None;
            if (string.IsNullOrEmpty(objectName))
                return false;

            int start = objectName.IndexOf(Prefix, StringComparison.OrdinalIgnoreCase);
            if (start < 0)
                return false;

            start += Prefix.Length;
            int end = objectName.IndexOf(Suffix, start, StringComparison.Ordinal);
            if (end < 0)
                return false;

            string token = objectName.Substring(start, end - start).Trim();
            return Enum.TryParse(token, ignoreCase: true, out type) && type != FeelTagType.None;
        }

        public static string MakeName(FeelTagType type, string baseName = null)
        {
            string tag = $"{Prefix}{type}{Suffix}";
            if (string.IsNullOrWhiteSpace(baseName))
                return tag;
            return $"{baseName}_{tag}";
        }
    }
}
