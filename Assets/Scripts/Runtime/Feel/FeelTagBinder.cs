using UnityEngine;

namespace PolyPets.Feel
{
    /// <summary>
    /// Applies the right juice component for a <see cref="FeelTag"/>.
    /// Works without the Asset Store Feel pack (built-in idles). When Feel is imported,
    /// use <c>PolyPets → Feel → Upgrade Tags To MMF Players</c> to swap in MMF_Player setups.
    /// </summary>
    public static class FeelTagBinder
    {
        public static void Apply(FeelTag tag)
        {
            if (tag == null)
                return;

            EnsureBehaviour(tag);
        }

        public static void ApplyAndPlay(FeelTag tag)
        {
            Apply(tag);
            Play(tag);
        }

        public static void Play(FeelTag tag)
        {
            if (tag == null)
                return;

            switch (tag.TagType)
            {
                case FeelTagType.Squash:
                    tag.GetComponent<FeelIdleSquash>()?.Play();
                    break;
                case FeelTagType.Wobble:
                    tag.GetComponent<FeelIdleWobble>()?.Play();
                    break;
                case FeelTagType.Bounce:
                    tag.GetComponent<FeelIdleBounce>()?.Play();
                    break;
                case FeelTagType.Punch:
                    tag.GetComponent<FeelPunchScale>()?.Play();
                    break;
                case FeelTagType.Shake:
                    tag.GetComponent<FeelShake>()?.Play();
                    break;
                case FeelTagType.Pop:
                    tag.GetComponent<FeelPopIn>()?.Play();
                    break;
            }

            // If More Mountains Feel is imported, also trigger any MMF_Player on the object.
            FeelBridge.TryPlayFeedback(tag.gameObject, tag.TagType.ToString());
        }

        public static void Stop(FeelTag tag)
        {
            if (tag == null)
                return;

            tag.GetComponent<FeelIdleSquash>()?.Stop();
            tag.GetComponent<FeelIdleWobble>()?.Stop();
            tag.GetComponent<FeelIdleBounce>()?.Stop();
            tag.GetComponent<FeelPunchScale>()?.Stop();
            tag.GetComponent<FeelShake>()?.Stop();
            tag.GetComponent<FeelPopIn>()?.Stop();
        }

        public static FeelTag EnsureTagOn(GameObject go, FeelTagType type, bool autoPlay = true)
        {
            if (go == null)
                return null;

            if (!FeelTagNaming.TryParseObjectName(go.name, out _))
                go.name = FeelTagNaming.MakeName(type, StripFeelSuffix(go.name));

            var tag = go.GetComponent<FeelTag>();
            if (tag == null)
                tag = go.AddComponent<FeelTag>();

            tag.Configure(type, autoPlay, go.transform);
            EnsureBehaviour(tag);
            return tag;
        }

        /// <summary>
        /// Feel docs recommend: Root → SquashContainer(1,1,1) → Model.
        /// Moves all children under a new FEEL[Squash] container and tags it.
        /// </summary>
        public static Transform WrapChildrenWithFeelContainer(Transform root, FeelTagType type, string containerBaseName = null)
        {
            if (root == null)
                return null;

            var existing = root.Find(FeelTagNaming.MakeName(type, containerBaseName));
            if (existing != null)
            {
                EnsureTagOn(existing.gameObject, type);
                return existing;
            }

            var container = new GameObject(FeelTagNaming.MakeName(type, containerBaseName));
            container.transform.SetParent(root, false);
            container.transform.localPosition = Vector3.zero;
            container.transform.localRotation = Quaternion.identity;
            container.transform.localScale = Vector3.one;

            // Move existing children under the container (skip if already only the container).
            var children = new Transform[root.childCount];
            int write = 0;
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child == container.transform)
                    continue;
                children[write++] = child;
            }

            for (int i = 0; i < write; i++)
                children[i].SetParent(container.transform, true);

            EnsureTagOn(container, type, autoPlay: true);
            return container.transform;
        }

        private static void EnsureBehaviour(FeelTag tag)
        {
            switch (tag.TagType)
            {
                case FeelTagType.Squash:
                    if (tag.GetComponent<FeelIdleSquash>() == null)
                        tag.gameObject.AddComponent<FeelIdleSquash>();
                    break;
                case FeelTagType.Wobble:
                    if (tag.GetComponent<FeelIdleWobble>() == null)
                        tag.gameObject.AddComponent<FeelIdleWobble>();
                    break;
                case FeelTagType.Bounce:
                    if (tag.GetComponent<FeelIdleBounce>() == null)
                        tag.gameObject.AddComponent<FeelIdleBounce>();
                    break;
                case FeelTagType.Punch:
                    if (tag.GetComponent<FeelPunchScale>() == null)
                        tag.gameObject.AddComponent<FeelPunchScale>();
                    break;
                case FeelTagType.Shake:
                    if (tag.GetComponent<FeelShake>() == null)
                        tag.gameObject.AddComponent<FeelShake>();
                    break;
                case FeelTagType.Pop:
                    if (tag.GetComponent<FeelPopIn>() == null)
                        tag.gameObject.AddComponent<FeelPopIn>();
                    break;
            }
        }

        private static string StripFeelSuffix(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Target";
            int idx = name.IndexOf("_FEEL[", System.StringComparison.OrdinalIgnoreCase);
            if (idx > 0)
                return name.Substring(0, idx);
            if (name.StartsWith("FEEL[", System.StringComparison.OrdinalIgnoreCase))
                return "Target";
            return name;
        }
    }
}
