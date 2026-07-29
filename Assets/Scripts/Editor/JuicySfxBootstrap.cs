#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PolyPets.Audio;

namespace PolyPets.EditorTools
{
    public static class JuicySfxBootstrap
    {
        private const string Folder = "Assets/Audio/Sfx";

        public static JuicySfx EnsureOn(GameObject systems)
        {
            var juicy = systems.GetComponent<JuicySfx>() ?? systems.AddComponent<JuicySfx>();
            WireClips(juicy);
            return juicy;
        }

        public static void WireClips(JuicySfx juicy)
        {
            if (juicy == null)
                return;

            AudioClip Load(string name) =>
                AssetDatabase.LoadAssetAtPath<AudioClip>($"{Folder}/{name}.ogg");

            var ladder = new[]
            {
                Load("Sfx_CoinDing_0"),
                Load("Sfx_CoinDing_1"),
                Load("Sfx_CoinDing_2"),
                Load("Sfx_CoinDing_3"),
                Load("Sfx_CoinDing_4"),
                Load("Sfx_CoinDing_5"),
            };

            juicy.BindClips(
                Load("Sfx_UiClick"),
                Load("Sfx_CoinSpawn"),
                Load("Sfx_CoinDing"),
                ladder,
                Load("Sfx_CoinCascade"),
                Load("Sfx_Purchase"),
                Load("Sfx_Deny"),
                Load("Sfx_Feed"),
                Load("Sfx_CleanSparkle"),
                Load("Sfx_ScrubTick"),
                Load("Sfx_LevelUp"),
                Load("Sfx_MinigameWin"),
                Load("Sfx_Hit"),
                Load("Sfx_Miss"),
                Load("Sfx_RoomWhoosh"),
                Load("Sfx_Celebrate"),
                Load("Sfx_Pop"),
                Load("Sfx_ReelTick"));

            EditorUtility.SetDirty(juicy);
        }
    }
}
#endif
