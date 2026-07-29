using UnityEngine;
using PolyPets.Feel;
using PolyPets.Needs;

namespace PolyPets.Pets
{
    /// <summary>
    /// Builds greybox starter pets until art arrives.
    /// </summary>
    public static class StarterPetFactory
    {
        public static PetAgent Spawn(
            PetSpecies species,
            string petName,
            PetDefinition definition,
            Transform parent,
            Material primary,
            Material secondary)
        {
            var root = new GameObject($"Pet_{species}_{petName}");
            if (parent != null)
                root.transform.SetParent(parent, false);

            var agent = root.AddComponent<PetAgent>();
            var needs = root.AddComponent<PetNeeds>();
            agent.BindDefinition(definition);
            agent.SetPetName(petName);

            switch (species)
            {
                case PetSpecies.Dog:
                    BuildDog(root.transform, primary, secondary);
                    break;
                case PetSpecies.Rabbit:
                    BuildRabbit(root.transform, primary, secondary);
                    break;
                default:
                    BuildCat(root.transform, primary, secondary);
                    break;
            }

            FeelTagBinder.WrapChildrenWithFeelContainer(root.transform, FeelTagType.Squash, "Idle");

            // Re-bind head after Feel wrap so mouse-look tracks the moved transform.
            var headTf = FindNamed(root.transform, "Head_Box");
            var bodyTf = FindNamed(root.transform, "Body");
            if (headTf != null && bodyTf != null)
                agent.SetVisualRoots(headTf, bodyTf);

            agent.EnsureFoodBowl(secondary);
            return agent;
        }

        private static Transform FindNamed(Transform root, string name)
        {
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var found = FindNamed(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static void BuildCat(Transform root, Material primary, Material secondary)
        {
            var body = Cube(root, "Body", new Vector3(0f, 0.45f, 0f), new Vector3(0.55f, 0.4f, 0.85f), primary);
            var head = Cube(root, "Head_Box", new Vector3(0f, 0.95f, 0.15f), new Vector3(0.55f, 0.55f, 0.55f), primary);
            ParentLocal(Cube(root, "Ear_L", new Vector3(-0.18f, 1.28f, 0.05f), new Vector3(0.14f, 0.18f, 0.1f), secondary, noCollider: true), head.transform);
            ParentLocal(Cube(root, "Ear_R", new Vector3(0.18f, 1.28f, 0.05f), new Vector3(0.14f, 0.18f, 0.1f), secondary, noCollider: true), head.transform);
            ParentLocal(Cube(root, "Eye_L", new Vector3(-0.12f, 0.98f, 0.4f), new Vector3(0.1f, 0.12f, 0.06f), secondary, noCollider: true), head.transform);
            ParentLocal(Cube(root, "Eye_R", new Vector3(0.12f, 0.98f, 0.4f), new Vector3(0.1f, 0.12f, 0.06f), secondary, noCollider: true), head.transform);
            Leg(root, "Leg_FL", new Vector3(-0.16f, 0.16f, 0.25f), secondary);
            Leg(root, "Leg_FR", new Vector3(0.16f, 0.16f, 0.25f), secondary);
            Leg(root, "Leg_BL", new Vector3(-0.16f, 0.16f, -0.28f), secondary);
            Leg(root, "Leg_BR", new Vector3(0.16f, 0.16f, -0.28f), secondary);
            var tail = Cube(root, "Tail", new Vector3(0.28f, 0.55f, -0.5f), new Vector3(0.1f, 0.1f, 0.55f), primary);
            tail.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);

            var agent = root.GetComponent<PetAgent>();
            agent.SetVisualRoots(head.transform, body.transform);
        }

        private static void BuildDog(Transform root, Material primary, Material secondary)
        {
            var body = Cube(root, "Body", new Vector3(0f, 0.5f, 0f), new Vector3(0.6f, 0.45f, 0.95f), primary);
            var head = Cube(root, "Head_Box", new Vector3(0f, 0.95f, 0.35f), new Vector3(0.5f, 0.5f, 0.5f), primary);
            ParentLocal(Cube(root, "Snout", new Vector3(0f, 0.82f, 0.62f), new Vector3(0.28f, 0.22f, 0.28f), secondary, noCollider: true), head.transform);
            ParentLocal(Cube(root, "Ear_L", new Vector3(-0.28f, 1.05f, 0.3f), new Vector3(0.12f, 0.28f, 0.2f), secondary, noCollider: true), head.transform);
            ParentLocal(Cube(root, "Ear_R", new Vector3(0.28f, 1.05f, 0.3f), new Vector3(0.12f, 0.28f, 0.2f), secondary, noCollider: true), head.transform);
            Leg(root, "Leg_FL", new Vector3(-0.18f, 0.18f, 0.3f), secondary);
            Leg(root, "Leg_FR", new Vector3(0.18f, 0.18f, 0.3f), secondary);
            Leg(root, "Leg_BL", new Vector3(-0.18f, 0.18f, -0.32f), secondary);
            Leg(root, "Leg_BR", new Vector3(0.18f, 0.18f, -0.32f), secondary);
            var tail = Cube(root, "Tail", new Vector3(0f, 0.7f, -0.55f), new Vector3(0.12f, 0.12f, 0.4f), primary);
            tail.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
            root.GetComponent<PetAgent>().SetVisualRoots(head.transform, body.transform);
        }

        private static void BuildRabbit(Transform root, Material primary, Material secondary)
        {
            var body = Cube(root, "Body", new Vector3(0f, 0.4f, 0f), new Vector3(0.5f, 0.45f, 0.65f), primary);
            var head = Cube(root, "Head_Box", new Vector3(0f, 0.85f, 0.2f), new Vector3(0.45f, 0.45f, 0.45f), primary);
            ParentLocal(Cube(root, "Ear_L", new Vector3(-0.12f, 1.35f, 0.1f), new Vector3(0.1f, 0.45f, 0.08f), secondary, noCollider: true), head.transform);
            ParentLocal(Cube(root, "Ear_R", new Vector3(0.12f, 1.35f, 0.1f), new Vector3(0.1f, 0.45f, 0.08f), secondary, noCollider: true), head.transform);
            ParentLocal(Cube(root, "Eye_L", new Vector3(-0.1f, 0.88f, 0.4f), new Vector3(0.08f, 0.1f, 0.05f), secondary, noCollider: true), head.transform);
            ParentLocal(Cube(root, "Eye_R", new Vector3(0.1f, 0.88f, 0.4f), new Vector3(0.08f, 0.1f, 0.05f), secondary, noCollider: true), head.transform);
            Leg(root, "Leg_FL", new Vector3(-0.14f, 0.14f, 0.18f), secondary);
            Leg(root, "Leg_FR", new Vector3(0.14f, 0.14f, 0.18f), secondary);
            Leg(root, "Leg_BL", new Vector3(-0.14f, 0.14f, -0.2f), secondary);
            Leg(root, "Leg_BR", new Vector3(0.14f, 0.14f, -0.2f), secondary);
            var cottontail = Cube(root, "Tail", new Vector3(0f, 0.45f, -0.4f), new Vector3(0.18f, 0.18f, 0.18f), secondary);
            root.GetComponent<PetAgent>().SetVisualRoots(head.transform, body.transform);
            _ = cottontail;
        }

        private static void ParentLocal(GameObject child, Transform newParent)
        {
            var t = child.transform;
            Vector3 worldPos = t.position;
            Quaternion worldRot = t.rotation;
            Vector3 worldScale = t.lossyScale;
            t.SetParent(newParent, true);
            t.position = worldPos;
            t.rotation = worldRot;
            // Keep visual size after reparent (head may be non-uniform).
            var parentScale = newParent.lossyScale;
            t.localScale = new Vector3(
                worldScale.x / Mathf.Max(0.0001f, parentScale.x),
                worldScale.y / Mathf.Max(0.0001f, parentScale.y),
                worldScale.z / Mathf.Max(0.0001f, parentScale.z));
        }

        private static void Leg(Transform root, string name, Vector3 pos, Material mat)
        {
            Cube(root, name, pos, new Vector3(0.12f, 0.32f, 0.12f), mat, noCollider: true);
        }

        private static GameObject Cube(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat, bool noCollider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (noCollider)
            {
                var col = go.GetComponent<Collider>();
                if (col != null)
                {
                    if (Application.isPlaying)
                        Object.Destroy(col);
                    else
                        Object.DestroyImmediate(col);
                }
            }

            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (mat != null)
            {
                var renderer = go.GetComponent<MeshRenderer>();
                if (renderer != null)
                    renderer.sharedMaterial = mat;
            }

            return go;
        }
    }
}
