#if UNITY_EDITOR
using UnityEngine;
using PolyPets.House;
using PolyPets.Rendering;

namespace PolyPets.EditorTools
{
    /// <summary>
    /// Builds cozy multi-part rooms from the locked 25-material palette.
    /// Goal: after First-Time Setup the house already feels like half the game.
    /// </summary>
    public static class RoomBeautyBuilder
    {
        public static readonly Vector3 RoomSize = new(6f, 3f, 6f);

        public enum RoomStyle
        {
            Living,
            Kitchen,
            Bedroom,
            Garden
        }

        public static void DressRoom(GameObject roomGo, MaterialPalette palette, RoomStyle style)
        {
            if (roomGo == null || palette == null)
                return;

            BuildShell(roomGo, palette, style);
            switch (style)
            {
                case RoomStyle.Living:
                    DressLiving(roomGo, palette);
                    break;
                case RoomStyle.Kitchen:
                    DressKitchen(roomGo, palette);
                    break;
                case RoomStyle.Bedroom:
                    DressBedroom(roomGo, palette);
                    break;
                case RoomStyle.Garden:
                    DressGarden(roomGo, palette);
                    break;
            }
        }

        private static void BuildShell(GameObject roomGo, MaterialPalette palette, RoomStyle style)
        {
            bool outdoor = style == RoomStyle.Garden;
            Material floorMat = outdoor ? palette.dirtGarden : palette.floorWornWood;
            Material wallMat = palette.wallPeeling;
            Material trim = palette.trimDark;
            Material sky = palette.skyDusk;

            // Floor
            var floor = Prim(roomGo, "Floor", PrimitiveType.Cube,
                new Vector3(0f, -0.05f, 0f), new Vector3(RoomSize.x, 0.1f, RoomSize.z), floorMat);
            TileUVs(floor, outdoor ? 2.5f : 3.2f, outdoor ? 2.5f : 3.2f);

            // Walls — open front for camera; garden uses fence posts instead of full side walls.
            if (!outdoor)
            {
                var back = Prim(roomGo, "Wall_Back", PrimitiveType.Cube,
                    new Vector3(0f, RoomSize.y * 0.5f, RoomSize.z * 0.5f),
                    new Vector3(RoomSize.x, RoomSize.y, 0.14f), wallMat);
                TileUVs(back, 2.2f, 1.4f);

                var left = Prim(roomGo, "Wall_Left", PrimitiveType.Cube,
                    new Vector3(-RoomSize.x * 0.5f, RoomSize.y * 0.5f, 0f),
                    new Vector3(0.14f, RoomSize.y, RoomSize.z), wallMat);
                TileUVs(left, 2.2f, 1.4f);

                var right = Prim(roomGo, "Wall_Right", PrimitiveType.Cube,
                    new Vector3(RoomSize.x * 0.5f, RoomSize.y * 0.5f, 0f),
                    new Vector3(0.14f, RoomSize.y, RoomSize.z), wallMat);
                TileUVs(right, 2.2f, 1.4f);

                // Ceiling — closes the box so it reads as a room, not a diorama shell.
                var ceiling = Prim(roomGo, "Ceiling", PrimitiveType.Cube,
                    new Vector3(0f, RoomSize.y + 0.04f, 0f),
                    new Vector3(RoomSize.x + 0.2f, 0.1f, RoomSize.z + 0.2f), trim);
                TileUVs(ceiling, 2f, 2f);

                // Front apron — low wall so the void under the camera isn't empty floor-edge.
                Prim(roomGo, "Wall_FrontApron", PrimitiveType.Cube,
                    new Vector3(0f, 0.28f, -RoomSize.z * 0.5f),
                    new Vector3(RoomSize.x - 0.1f, 0.56f, 0.12f), wallMat);

                // Baseboards
                Prim(roomGo, "Trim_Back", PrimitiveType.Cube,
                    new Vector3(0f, 0.12f, RoomSize.z * 0.5f - 0.08f),
                    new Vector3(RoomSize.x - 0.3f, 0.24f, 0.08f), trim);
                Prim(roomGo, "Trim_Left", PrimitiveType.Cube,
                    new Vector3(-RoomSize.x * 0.5f + 0.08f, 0.12f, 0f),
                    new Vector3(0.08f, 0.24f, RoomSize.z - 0.3f), trim);
                Prim(roomGo, "Trim_Right", PrimitiveType.Cube,
                    new Vector3(RoomSize.x * 0.5f - 0.08f, 0.12f, 0f),
                    new Vector3(0.08f, 0.24f, RoomSize.z - 0.3f), trim);

                // Crown molding hint
                Prim(roomGo, "Trim_CrownBack", PrimitiveType.Cube,
                    new Vector3(0f, RoomSize.y - 0.08f, RoomSize.z * 0.5f - 0.08f),
                    new Vector3(RoomSize.x - 0.3f, 0.1f, 0.08f), trim);

                // Window with sky peek
                BuildWindow(roomGo, palette, new Vector3(0f, 1.55f, RoomSize.z * 0.5f - 0.02f));
            }
            else
            {
                // Garden: low fence ring + open sky
                BuildFence(roomGo, palette);
                var skyPlane = Prim(roomGo, "SkyBackdrop", PrimitiveType.Quad,
                    new Vector3(0f, 2.4f, 4.2f), new Vector3(10f, 5.5f, 1f), sky);
                skyPlane.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                NoCollider(skyPlane);
            }

            // Soft ground contact shadow under the play area
            ShadowBlob(roomGo, "Shadow_RoomCenter", new Vector3(0f, 0.015f, 0f), new Vector3(2.8f, 0.01f, 2.0f), palette.shadowBlob);
        }

        private static void BuildWindow(GameObject roomGo, MaterialPalette palette, Vector3 center)
        {
            float w = 1.6f;
            float h = 1.1f;
            // Frame
            Prim(roomGo, "Window_Frame", PrimitiveType.Cube,
                center, new Vector3(w + 0.12f, h + 0.12f, 0.06f), palette.trimDark);
            // Glass / sky peek
            var glass = Prim(roomGo, "Window_Glass", PrimitiveType.Cube,
                center + new Vector3(0f, 0f, 0.04f), new Vector3(w, h, 0.04f), palette.skyDusk);
            NoCollider(glass);
            // Mullion
            Prim(roomGo, "Window_Mullion_V", PrimitiveType.Cube,
                center + new Vector3(0f, 0f, 0.05f), new Vector3(0.06f, h, 0.05f), palette.trimDark);
            Prim(roomGo, "Window_Mullion_H", PrimitiveType.Cube,
                center + new Vector3(0f, 0f, 0.05f), new Vector3(w, 0.06f, 0.05f), palette.trimDark);
            // Sill
            Prim(roomGo, "Window_Sill", PrimitiveType.Cube,
                center + new Vector3(0f, -h * 0.5f - 0.06f, -0.08f),
                new Vector3(w + 0.25f, 0.08f, 0.22f), palette.propDusty);
        }

        private static void BuildFence(GameObject roomGo, MaterialPalette palette)
        {
            float half = RoomSize.x * 0.5f - 0.1f;
            float zHalf = RoomSize.z * 0.5f - 0.1f;
            // Back rail
            Prim(roomGo, "Fence_Back", PrimitiveType.Cube,
                new Vector3(0f, 0.55f, zHalf), new Vector3(RoomSize.x - 0.2f, 0.12f, 0.08f), palette.trimDark);
            Prim(roomGo, "Fence_Left", PrimitiveType.Cube,
                new Vector3(-half, 0.55f, 0f), new Vector3(0.08f, 0.12f, RoomSize.z - 0.2f), palette.trimDark);
            Prim(roomGo, "Fence_Right", PrimitiveType.Cube,
                new Vector3(half, 0.55f, 0f), new Vector3(0.08f, 0.12f, RoomSize.z - 0.2f), palette.trimDark);

            for (int i = -2; i <= 2; i++)
            {
                float x = i * 1.15f;
                Prim(roomGo, $"FencePost_B{i}", PrimitiveType.Cube,
                    new Vector3(x, 0.45f, zHalf), new Vector3(0.12f, 0.9f, 0.12f), palette.propDusty);
            }
        }

        private static void DressLiving(GameObject roomGo, MaterialPalette palette)
        {
            // Sofa against back-left
            BuildSofa(roomGo, palette, new Vector3(-1.4f, 0f, 1.55f), 12f);
            // Coffee table
            Prim(roomGo, "Prop_CoffeeTable", PrimitiveType.Cube,
                new Vector3(-0.3f, 0.28f, 0.55f), new Vector3(1.1f, 0.12f, 0.55f), palette.propDusty);
            Prim(roomGo, "Prop_CoffeeLeg_L", PrimitiveType.Cube,
                new Vector3(-0.65f, 0.12f, 0.55f), new Vector3(0.08f, 0.24f, 0.08f), palette.trimDark);
            Prim(roomGo, "Prop_CoffeeLeg_R", PrimitiveType.Cube,
                new Vector3(0.05f, 0.12f, 0.55f), new Vector3(0.08f, 0.24f, 0.08f), palette.trimDark);

            BuildFloorLamp(roomGo, palette, new Vector3(1.85f, 0f, 1.55f));
            BuildPlant(roomGo, palette, new Vector3(2.1f, 0f, -1.35f), 1f);
            BuildCrate(roomGo, palette, new Vector3(-2.0f, 0f, -1.0f), 15f);
            BuildPicture(roomGo, palette, new Vector3(-1.6f, 1.7f, 2.88f), palette.coinGold);

            // Area rug
            var rug = Prim(roomGo, "Prop_Rug", PrimitiveType.Cube,
                new Vector3(0.1f, 0.02f, -0.15f), new Vector3(2.6f, 0.03f, 1.7f), palette.rugCharcoal);
            TileUVs(rug, 1.5f, 1.2f);

            BuildSideTable(roomGo, palette, new Vector3(1.5f, 0f, 0.2f));
        }

        private static void DressKitchen(GameObject roomGo, MaterialPalette palette)
        {
            // Counter run along back
            Prim(roomGo, "Prop_Counter", PrimitiveType.Cube,
                new Vector3(0f, 0.45f, 2.35f), new Vector3(4.2f, 0.9f, 0.7f), palette.propDusty);
            Prim(roomGo, "Prop_CounterTop", PrimitiveType.Cube,
                new Vector3(0f, 0.92f, 2.35f), new Vector3(4.35f, 0.08f, 0.78f), palette.metalDull);

            // Upper shelf + jars
            Prim(roomGo, "Prop_Shelf", PrimitiveType.Cube,
                new Vector3(0.2f, 1.75f, 2.75f), new Vector3(2.4f, 0.1f, 0.32f), palette.propDusty);
            Prim(roomGo, "Prop_Jar_A", PrimitiveType.Cylinder,
                new Vector3(-0.5f, 1.95f, 2.75f), new Vector3(0.22f, 0.18f, 0.22f), palette.bowlCeramic);
            Prim(roomGo, "Prop_Jar_B", PrimitiveType.Cylinder,
                new Vector3(0f, 1.95f, 2.75f), new Vector3(0.2f, 0.22f, 0.2f), palette.foodMedium);
            Prim(roomGo, "Prop_Jar_C", PrimitiveType.Cylinder,
                new Vector3(0.5f, 1.98f, 2.75f), new Vector3(0.18f, 0.16f, 0.18f), palette.foodBudget);

            // Sink basin
            Prim(roomGo, "Prop_Sink", PrimitiveType.Cube,
                new Vector3(1.2f, 0.95f, 2.35f), new Vector3(0.7f, 0.12f, 0.45f), palette.metalDull);
            Prim(roomGo, "Prop_Faucet", PrimitiveType.Cylinder,
                new Vector3(1.2f, 1.15f, 2.5f), new Vector3(0.06f, 0.18f, 0.06f), palette.metalDull);

            // Fruit bowl on counter
            Prim(roomGo, "Prop_FruitBowl", PrimitiveType.Sphere,
                new Vector3(-1.1f, 1.1f, 2.2f), new Vector3(0.4f, 0.22f, 0.4f), palette.bowlCeramic);
            Prim(roomGo, "Prop_Fruit_A", PrimitiveType.Sphere,
                new Vector3(-1.05f, 1.2f, 2.2f), new Vector3(0.14f, 0.14f, 0.14f), palette.foodSuper);
            Prim(roomGo, "Prop_Fruit_B", PrimitiveType.Sphere,
                new Vector3(-1.2f, 1.18f, 2.28f), new Vector3(0.12f, 0.12f, 0.12f), palette.carrotOrange);

            BuildFloorLamp(roomGo, palette, new Vector3(2.2f, 0f, -1.6f), shortLamp: true);
            BuildCrate(roomGo, palette, new Vector3(-2.1f, 0f, -1.2f), -20f);

            var rug = Prim(roomGo, "Prop_Rug", PrimitiveType.Cube,
                new Vector3(0f, 0.02f, 0.2f), new Vector3(1.8f, 0.03f, 1.1f), palette.rugCharcoal);
            TileUVs(rug, 1.2f, 1f);
        }

        private static void DressBedroom(GameObject roomGo, MaterialPalette palette)
        {
            BuildBed(roomGo, palette, new Vector3(-0.9f, 0f, 1.35f));
            BuildNightstand(roomGo, palette, new Vector3(0.85f, 0f, 1.7f));
            BuildFloorLamp(roomGo, palette, new Vector3(2.0f, 0f, 1.7f), shortLamp: true);
            BuildWardrobe(roomGo, palette, new Vector3(2.15f, 0f, -0.4f));
            BuildPlant(roomGo, palette, new Vector3(-2.15f, 0f, -1.4f), 0.85f);
            BuildPicture(roomGo, palette, new Vector3(0.4f, 1.85f, 2.88f), palette.fishSilver);

            var rug = Prim(roomGo, "Prop_Rug", PrimitiveType.Cube,
                new Vector3(0.2f, 0.02f, -0.4f), new Vector3(2.0f, 0.03f, 1.3f), palette.rugCharcoal);
            TileUVs(rug, 1.4f, 1.1f);
        }

        private static void DressGarden(GameObject roomGo, MaterialPalette palette)
        {
            // Raised beds
            BuildRaisedBed(roomGo, palette, new Vector3(-1.5f, 0f, 1.2f), new Vector3(1.6f, 0.35f, 0.85f));
            BuildRaisedBed(roomGo, palette, new Vector3(1.3f, 0f, 1.35f), new Vector3(1.3f, 0.3f, 0.7f));

            // Pond
            Prim(roomGo, "Prop_Pond", PrimitiveType.Cylinder,
                new Vector3(0.2f, 0.06f, -0.6f), new Vector3(1.4f, 0.08f, 1.0f), palette.waterPond);
            Prim(roomGo, "Prop_PondRim", PrimitiveType.Cylinder,
                new Vector3(0.2f, 0.02f, -0.6f), new Vector3(1.55f, 0.05f, 1.15f), palette.trimDark);

            BuildPlant(roomGo, palette, new Vector3(-2.2f, 0f, -1.3f), 1.15f);
            BuildPlant(roomGo, palette, new Vector3(2.15f, 0f, -1.5f), 0.9f);
            BuildPlant(roomGo, palette, new Vector3(-2.0f, 0f, 2.0f), 0.75f);

            // Carrot sprouts in bed
            for (int i = 0; i < 4; i++)
            {
                float x = -1.9f + i * 0.28f;
                Prim(roomGo, $"Prop_Sprout_{i}", PrimitiveType.Cylinder,
                    new Vector3(x, 0.45f, 1.2f), new Vector3(0.06f, 0.18f, 0.06f), palette.plantLeaf);
                Prim(roomGo, $"Prop_CarrotTip_{i}", PrimitiveType.Cylinder,
                    new Vector3(x, 0.28f, 1.2f), new Vector3(0.08f, 0.1f, 0.08f), palette.carrotOrange);
            }

            BuildCrate(roomGo, palette, new Vector3(2.0f, 0f, 0.2f), 25f);
            // Soft path strip
            Prim(roomGo, "Prop_Path", PrimitiveType.Cube,
                new Vector3(0f, 0.01f, 0.3f), new Vector3(1.0f, 0.02f, 3.5f), palette.propDusty);

            // Warm garden lantern
            BuildFloorLamp(roomGo, palette, new Vector3(-2.2f, 0f, 0.3f), shortLamp: true);
        }

        private static void BuildSofa(GameObject roomGo, MaterialPalette palette, Vector3 pos, float yaw)
        {
            var root = new GameObject("Prop_Sofa");
            root.transform.SetParent(roomGo.transform, false);
            root.transform.localPosition = pos;
            root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Prim(root, "Seat", PrimitiveType.Cube,
                new Vector3(0f, 0.28f, 0f), new Vector3(1.8f, 0.28f, 0.7f), palette.rugCharcoal);
            Prim(root, "Back", PrimitiveType.Cube,
                new Vector3(0f, 0.55f, 0.28f), new Vector3(1.8f, 0.55f, 0.18f), palette.rugCharcoal);
            Prim(root, "Arm_L", PrimitiveType.Cube,
                new Vector3(-0.95f, 0.4f, 0f), new Vector3(0.18f, 0.4f, 0.7f), palette.propDusty);
            Prim(root, "Arm_R", PrimitiveType.Cube,
                new Vector3(0.95f, 0.4f, 0f), new Vector3(0.18f, 0.4f, 0.7f), palette.propDusty);
            Prim(root, "Cushion", PrimitiveType.Cube,
                new Vector3(-0.35f, 0.48f, -0.05f), new Vector3(0.55f, 0.12f, 0.45f), palette.bowlCeramic);
            ShadowBlob(root, "Shadow", new Vector3(0f, 0.01f, 0f), new Vector3(1.9f, 0.01f, 0.75f), palette.shadowBlob);
        }

        private static void BuildBed(GameObject roomGo, MaterialPalette palette, Vector3 pos)
        {
            var root = new GameObject("Prop_Bed");
            root.transform.SetParent(roomGo.transform, false);
            root.transform.localPosition = pos;

            Prim(root, "Frame", PrimitiveType.Cube,
                new Vector3(0f, 0.22f, 0f), new Vector3(1.7f, 0.25f, 2.2f), palette.propDusty);
            Prim(root, "Mattress", PrimitiveType.Cube,
                new Vector3(0f, 0.42f, 0f), new Vector3(1.55f, 0.2f, 2.0f), palette.bowlCeramic);
            Prim(root, "Blanket", PrimitiveType.Cube,
                new Vector3(0f, 0.55f, -0.15f), new Vector3(1.5f, 0.08f, 1.4f), palette.rugCharcoal);
            Prim(root, "Pillow", PrimitiveType.Cube,
                new Vector3(0f, 0.58f, 0.75f), new Vector3(0.9f, 0.14f, 0.35f), palette.wallPeeling);
            Prim(root, "Headboard", PrimitiveType.Cube,
                new Vector3(0f, 0.7f, 1.05f), new Vector3(1.7f, 0.7f, 0.1f), palette.trimDark);
            ShadowBlob(root, "Shadow", new Vector3(0f, 0.01f, 0f), new Vector3(1.8f, 0.01f, 2.3f), palette.shadowBlob);
        }

        private static void BuildNightstand(GameObject roomGo, MaterialPalette palette, Vector3 pos)
        {
            Prim(roomGo, "Prop_Nightstand", PrimitiveType.Cube,
                pos + new Vector3(0f, 0.3f, 0f), new Vector3(0.45f, 0.55f, 0.4f), palette.propDusty);
            Prim(roomGo, "Prop_NightstandTop", PrimitiveType.Cube,
                pos + new Vector3(0f, 0.58f, 0f), new Vector3(0.5f, 0.05f, 0.45f), palette.trimDark);
        }

        private static void BuildWardrobe(GameObject roomGo, MaterialPalette palette, Vector3 pos)
        {
            Prim(roomGo, "Prop_Wardrobe", PrimitiveType.Cube,
                pos + new Vector3(0f, 1.0f, 0f), new Vector3(0.7f, 2.0f, 0.5f), palette.propDusty);
            Prim(roomGo, "Prop_WardrobeDoor", PrimitiveType.Cube,
                pos + new Vector3(0f, 1.0f, -0.26f), new Vector3(0.6f, 1.8f, 0.04f), palette.trimDark);
            Prim(roomGo, "Prop_WardrobeKnob", PrimitiveType.Sphere,
                pos + new Vector3(0.2f, 1.0f, -0.3f), new Vector3(0.08f, 0.08f, 0.08f), palette.metalDull);
        }

        private static void BuildSideTable(GameObject roomGo, MaterialPalette palette, Vector3 pos)
        {
            Prim(roomGo, "Prop_SideTable", PrimitiveType.Cube,
                pos + new Vector3(0f, 0.35f, 0f), new Vector3(0.5f, 0.08f, 0.5f), palette.propDusty);
            Prim(roomGo, "Prop_SideLeg", PrimitiveType.Cylinder,
                pos + new Vector3(0f, 0.16f, 0f), new Vector3(0.08f, 0.16f, 0.08f), palette.trimDark);
        }

        private static void BuildFloorLamp(GameObject roomGo, MaterialPalette palette, Vector3 pos, bool shortLamp = false)
        {
            var root = new GameObject("Prop_Lamp");
            root.transform.SetParent(roomGo.transform, false);
            root.transform.localPosition = pos;

            float poleH = shortLamp ? 0.55f : 0.85f;
            float shadeY = shortLamp ? 1.15f : 1.55f;

            Prim(root, "Base", PrimitiveType.Cylinder,
                new Vector3(0f, 0.06f, 0f), new Vector3(0.28f, 0.05f, 0.28f), palette.metalDull);
            Prim(root, "Pole", PrimitiveType.Cylinder,
                new Vector3(0f, poleH * 0.5f + 0.08f, 0f), new Vector3(0.07f, poleH * 0.5f, 0.07f), palette.trimDark);
            var shade = Prim(root, "Shade", PrimitiveType.Cube,
                new Vector3(0f, shadeY, 0f), new Vector3(0.5f, 0.28f, 0.5f), palette.accentLamp);

            // Warm point light — driven by DayNight via RoomLamp
            var lightGo = new GameObject("RoomLamp");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, shadeY, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.78f, 0.48f);
            light.intensity = 1.6f;
            light.range = shortLamp ? 3.8f : 5.0f;
            light.shadows = LightShadows.None;
            var roomLamp = lightGo.AddComponent<RoomLamp>();
            roomLamp.Bind(light);

            ShadowBlob(root, "Shadow", new Vector3(0f, 0.01f, 0f), new Vector3(0.45f, 0.01f, 0.45f), palette.shadowBlob);
            _ = shade;
        }

        private static void BuildPlant(GameObject roomGo, MaterialPalette palette, Vector3 pos, float scale)
        {
            var root = new GameObject("Prop_Plant");
            root.transform.SetParent(roomGo.transform, false);
            root.transform.localPosition = pos;
            root.transform.localScale = Vector3.one * scale;

            Prim(root, "Pot", PrimitiveType.Cylinder,
                new Vector3(0f, 0.22f, 0f), new Vector3(0.32f, 0.22f, 0.32f), palette.propDusty);
            Prim(root, "Soil", PrimitiveType.Cylinder,
                new Vector3(0f, 0.4f, 0f), new Vector3(0.26f, 0.04f, 0.26f), palette.dirtGarden);
            Prim(root, "Leaf_A", PrimitiveType.Sphere,
                new Vector3(0f, 0.7f, 0f), new Vector3(0.45f, 0.4f, 0.45f), palette.plantLeaf);
            Prim(root, "Leaf_B", PrimitiveType.Sphere,
                new Vector3(0.18f, 0.85f, 0.05f), new Vector3(0.28f, 0.28f, 0.28f), palette.plantLeaf);
            Prim(root, "Leaf_C", PrimitiveType.Sphere,
                new Vector3(-0.15f, 0.9f, -0.1f), new Vector3(0.25f, 0.3f, 0.25f), palette.plantLeaf);
            ShadowBlob(root, "Shadow", new Vector3(0f, 0.01f, 0f), new Vector3(0.4f, 0.01f, 0.4f), palette.shadowBlob);
        }

        private static void BuildCrate(GameObject roomGo, MaterialPalette palette, Vector3 pos, float yaw)
        {
            var root = new GameObject("Prop_Crate");
            root.transform.SetParent(roomGo.transform, false);
            root.transform.localPosition = pos;
            root.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            Prim(root, "Box", PrimitiveType.Cube,
                new Vector3(0f, 0.3f, 0f), new Vector3(0.75f, 0.55f, 0.6f), palette.propDusty);
            Prim(root, "Lid", PrimitiveType.Cube,
                new Vector3(0.05f, 0.6f, 0f), new Vector3(0.78f, 0.06f, 0.62f), palette.trimDark);
            ShadowBlob(root, "Shadow", new Vector3(0f, 0.01f, 0f), new Vector3(0.8f, 0.01f, 0.65f), palette.shadowBlob);
        }

        private static void BuildPicture(GameObject roomGo, MaterialPalette palette, Vector3 pos, Material art)
        {
            Prim(roomGo, "Prop_PictureFrame", PrimitiveType.Cube,
                pos, new Vector3(0.7f, 0.55f, 0.05f), palette.trimDark);
            Prim(roomGo, "Prop_PictureArt", PrimitiveType.Cube,
                pos + new Vector3(0f, 0f, -0.03f), new Vector3(0.55f, 0.4f, 0.03f), art != null ? art : palette.coinGold);
        }

        private static void BuildRaisedBed(GameObject roomGo, MaterialPalette palette, Vector3 pos, Vector3 size)
        {
            var root = new GameObject("Prop_RaisedBed");
            root.transform.SetParent(roomGo.transform, false);
            root.transform.localPosition = pos;

            Prim(root, "Box", PrimitiveType.Cube,
                new Vector3(0f, size.y * 0.5f, 0f), size, palette.propDusty);
            Prim(root, "Soil", PrimitiveType.Cube,
                new Vector3(0f, size.y + 0.02f, 0f),
                new Vector3(size.x - 0.1f, 0.08f, size.z - 0.1f), palette.dirtGarden);
        }

        private static GameObject Prim(
            GameObject parent,
            string name,
            PrimitiveType type,
            Vector3 localPos,
            Vector3 scale,
            Material mat)
        {
            return Prim(parent.transform, name, type, localPos, scale, mat);
        }

        private static GameObject Prim(
            Transform parent,
            string name,
            PrimitiveType type,
            Vector3 localPos,
            Vector3 scale,
            Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            if (mat != null)
            {
                var renderer = go.GetComponent<MeshRenderer>();
                if (renderer != null)
                    renderer.sharedMaterial = mat;
            }

            return go;
        }

        private static void ShadowBlob(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = Prim(parent, name, PrimitiveType.Cylinder, pos, scale, mat);
            NoCollider(go);
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private static void ShadowBlob(GameObject parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            ShadowBlob(parent.transform, name, pos, scale, mat);
        }

        private static void NoCollider(GameObject go)
        {
            var col = go.GetComponent<Collider>();
            if (col != null)
                Object.DestroyImmediate(col);
        }

        /// <summary>
        /// Scale mesh UVs via material property block so shared mats stay tiled per surface size.
        /// Uses renderer material instance ST when possible without breaking the shared asset.
        /// </summary>
        private static void TileUVs(GameObject go, float tileX, float tileY)
        {
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer == null || renderer.sharedMaterial == null)
                return;

            // Prefer MPB so we don't clone 25 assets — works with PolyPets/CelShade _BaseMap_ST.
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetVector("_BaseMap_ST", new Vector4(tileX, tileY, 0f, 0f));
            renderer.SetPropertyBlock(block);
        }
    }
}
#endif
