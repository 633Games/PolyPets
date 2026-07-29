using System;
using System.Reflection;
using UnityEngine;

namespace PolyPets.Feel
{
    /// <summary>
    /// Optional bridge to More Mountains Feel (MMF_Player) via reflection.
    /// Safe when Feel is not imported — calls no-op.
    /// </summary>
    public static class FeelBridge
    {
        private static bool _searched;
        private static Type _mmfPlayerType;
        private static MethodInfo _playMethod;
        private static MethodInfo _playFeedbacksMethod;

        public static bool IsFeelAvailable
        {
            get
            {
                EnsureTypes();
                return _mmfPlayerType != null;
            }
        }

        /// <summary>
        /// Plays an MMF_Player on the object (or children) if Feel is present.
        /// Falls back silently when Feel is missing.
        /// </summary>
        public static bool TryPlayFeedback(GameObject host, string context = null)
        {
            if (host == null)
                return false;

            EnsureTypes();
            if (_mmfPlayerType == null)
                return false;

            var player = host.GetComponent(_mmfPlayerType) as Component;
            if (player == null)
                player = host.GetComponentInChildren(_mmfPlayerType, true) as Component;
            if (player == null)
                return false;

            try
            {
                if (_playFeedbacksMethod != null)
                {
                    _playFeedbacksMethod.Invoke(player, null);
                    return true;
                }

                if (_playMethod != null)
                {
                    _playMethod.Invoke(player, null);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[PolyPets] Feel play failed ({context}): {ex.Message}");
            }

            return false;
        }

        public static Component EnsureMmfPlayer(GameObject host, bool autoPlayOnStart = false)
        {
            if (host == null)
                return null;
            EnsureTypes();
            if (_mmfPlayerType == null)
                return null;

            var existing = host.GetComponent(_mmfPlayerType) as Component;
            if (existing != null)
                return existing;

            var added = host.AddComponent(_mmfPlayerType) as Component;
            if (added != null && autoPlayOnStart)
            {
                try
                {
                    var soType = Type.GetType("UnityEditor.SerializedObject, UnityEditor");
                    // Runtime: set via reflection fields if present
                    var field = _mmfPlayerType.GetField("AutoPlayOnStart")
                                ?? _mmfPlayerType.GetField("m_AutoPlayOnStart");
                    field?.SetValue(added, true);
                }
                catch { /* ignore */ }
            }

            return added;
        }

        private static void EnsureTypes()
        {
            if (_searched)
                return;
            _searched = true;

            _mmfPlayerType = FindType("MoreMountains.Feedbacks.MMF_Player")
                             ?? FindType("MoreMountains.Feedbacks.MMFeedbacks");
            if (_mmfPlayerType == null)
                return;

            _playFeedbacksMethod = _mmfPlayerType.GetMethod("PlayFeedbacks", Type.EmptyTypes)
                                   ?? _mmfPlayerType.GetMethod("PlayFeedbacks", new[] { typeof(Vector3) });
            _playMethod = _mmfPlayerType.GetMethod("Play", Type.EmptyTypes);
        }

        private static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = null;
                try { type = assembly.GetType(fullName, throwOnError: false); }
                catch { /* dynamic */ }
                if (type != null)
                    return type;
            }

            return null;
        }
    }
}
