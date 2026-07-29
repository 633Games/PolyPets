using UnityEngine;
using PolyPets.Feel;
using PolyPets.Needs;

namespace PolyPets.Pets
{
    /// <summary>
    /// Builds starter pets (cel-shaded box pals) for the demo slice.
    /// </summary>
    public static class StarterPetFactory
    {
        public static PetAgent Spawn(
            PetSpecies species,
            string petName,
            PetDefinition definition,
            Transform parent,
            Material primary,
            Material secondary,
            Material shadowMat = null)
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
            agent.EnsureFoodBowl(secondary);

            if (root.GetComponent<PetProgression>() == null)
                root.AddComponent<PetProgression>();

            AttachShadowBlob(root.transform, shadowMat ?? secondary);

            var dirt = root.GetComponent<PetDirtVisual>() ?? root.AddComponent<PetDirtVisual>();
            dirt.BindNeeds(needs);
            dirt.CaptureRenderersFromHierarchy();
            var dirtMap = Resources.Load<Texture2D>("PolyPets/Tex_Dirt_Mask")
                          ?? Resources.Load<Texture2D>("PolyPets/Tex_Dirt_Overlay");
            if (dirtMap != null)
                dirt.ConfigureDirtMap(dirtMap);
            dirt.PushShaderState();

            return agent;
        }

        private static void BuildCat(Transform root, Material primary, Material secondary)
        {
            var body = Cube(root, "Body", new Vector3(0f, 0.45f, 0f), new Vector3(0.55f, 0.4f, 0.85f), primary);
            var head = Cube(root, "Head_Box", new Vector3(0f, 0.95f, 0.15f), new Vector3(0.55f, 0.55f, 0.55f), primary);
            Cube(root, "Ear_L", new Vector3(-0.18f, 1.28f, 0.05f), new Vector3(0.14f, 0.18f, 0.1f), secondary, noCollider: true);
            Cube(root, "Ear_R", new Vector3(0.18f, 1.28f, 0.05f), new Vector3(0.14f, 0.18f, 0.1f), secondary, noCollider: true);
            Cube(root, "Eye_L", new Vector3(-0.12f, 0.98f, 0.4f), new Vector3(0.1f, 0.12f, 0.06f), secondary, noCollider: true);
            Cube(root, "Eye_R", new Vector3(0.12f, 0.98f, 0.4f), new Vector3(0.1f, 0.12f, 0.06f), secondary, noCollider: true);
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
            Cube(root, "Snout", new Vector3(0f, 0.82f, 0.62f), new Vector3(0.28f, 0.22f, 0.28f), secondary, noCollider: true);
            Cube(root, "Ear_L", new Vector3(-0.28f, 1.05f, 0.3f), new Vector3(0.12f, 0.28f, 0.2f), secondary, noCollider: true);
            Cube(root, "Ear_R", new Vector3(0.28f, 1.05f, 0.3f), new Vector3(0.12f, 0.28f, 0.2f), secondary, noCollider: true);
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
            Cube(root, "Ear_L", new Vector3(-0.12f, 1.35f, 0.1f), new Vector3(0.1f, 0.45f, 0.08f), secondary, noCollider: true);
            Cube(root, "Ear_R", new Vector3(0.12f, 1.35f, 0.1f), new Vector3(0.1f, 0.45f, 0.08f), secondary, noCollider: true);
            Cube(root, "Eye_L", new Vector3(-0.1f, 0.88f, 0.4f), new Vector3(0.08f, 0.1f, 0.05f), secondary, noCollider: true);
            Cube(root, "Eye_R", new Vector3(0.1f, 0.88f, 0.4f), new Vector3(0.08f, 0.1f, 0.05f), secondary, noCollider: true);
            Leg(root, "Leg_FL", new Vector3(-0.14f, 0.14f, 0.18f), secondary);
            Leg(root, "Leg_FR", new Vector3(0.14f, 0.14f, 0.18f), secondary);
            Leg(root, "Leg_BL", new Vector3(-0.14f, 0.14f, -0.2f), secondary);
            Leg(root, "Leg_BR", new Vector3(0.14f, 0.14f, -0.2f), secondary);
            var cottontail = Cube(root, "Tail", new Vector3(0f, 0.45f, -0.4f), new Vector3(0.18f, 0.18f, 0.18f), secondary);
            root.GetComponent<PetAgent>().SetVisualRoots(head.transform, body.transform);
            _ = cottontail;
        }

        private static void AttachShadowBlob(Transform root, Material shadowMat)
        {
            var shadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shadow.name = "Shadow_Blob";
            var col = shadow.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(col);
                else
                    Object.DestroyImmediate(col);
            }

            shadow.transform.SetParent(root, false);
            shadow.transform.localPosition = new Vector3(0f, 0.012f, 0f);
            shadow.transform.localScale = new Vector3(0.75f, 0.012f, 0.5f);

            var renderer = shadow.GetComponent<MeshRenderer>();
            if (renderer == null)
                return;

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (shadowMat != null)
                renderer.sharedMaterial = shadowMat;
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
