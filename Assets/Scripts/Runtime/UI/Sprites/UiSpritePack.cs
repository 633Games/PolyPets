using System;
using UnityEngine;

namespace PolyPets.UI
{
    public enum UiButtonId
    {
        Shop = 0,
        Back = 1,
        Close = 2,
        Home = 3,
        Adopt = 4,
        Feed = 5,
        Play = 6,
        Clean = 7,
        Settings = 8,
        CollectAll = 9,
        PrevRoom = 10,
        NextRoom = 11,
        AcceptWant = 12,
        SnoozeWant = 13,
        AlwaysOnTop = 14,
        PauseTime = 15,
        Inventory = 16,
        Renovate = 17,
        Minigame = 18,
    }

    [Serializable]
    public struct UiButtonSpriteEntry
    {
        public UiButtonId id;
        public Sprite icon;
        public string label;
    }

    /// <summary>
    /// Drop your UI sprite atlas/pack here. Buttons read icons + optional 9-slices from this asset.
    /// </summary>
    [CreateAssetMenu(menuName = "PolyPets/UI Sprite Pack", fileName = "UiSpritePack")]
    public sealed class UiSpritePack : ScriptableObject
    {
        [Header("Chrome")]
        public Sprite panelBackground;
        public Sprite buttonBackground;
        public Sprite buttonBackgroundPressed;
        public Sprite buttonBackgroundDisabled;

        [Header("Colors")]
        public Color labelColor = new(0.40f, 0.32f, 0.26f, 1f);
        public Color accentColor = new(0.50f, 0.42f, 0.35f, 1f);
        public Color dangerColor = new(0.78f, 0.52f, 0.46f, 1f);

        [Header("Buttons")]
        public UiButtonSpriteEntry[] buttons =
        {
            new() { id = UiButtonId.Shop, label = "Shop" },
            new() { id = UiButtonId.Back, label = "Back" },
            new() { id = UiButtonId.Close, label = "Close" },
            new() { id = UiButtonId.Home, label = "Home" },
            new() { id = UiButtonId.Adopt, label = "Adopt" },
            new() { id = UiButtonId.Feed, label = "Feed" },
            new() { id = UiButtonId.Play, label = "Play" },
            new() { id = UiButtonId.Clean, label = "Clean" },
            new() { id = UiButtonId.Settings, label = "Settings" },
            new() { id = UiButtonId.CollectAll, label = "Collect" },
            new() { id = UiButtonId.PrevRoom, label = "Prev" },
            new() { id = UiButtonId.NextRoom, label = "Next" },
            new() { id = UiButtonId.AcceptWant, label = "Let's go" },
            new() { id = UiButtonId.SnoozeWant, label = "Later" },
            new() { id = UiButtonId.AlwaysOnTop, label = "Pin" },
            new() { id = UiButtonId.PauseTime, label = "Pause" },
            new() { id = UiButtonId.Inventory, label = "Bag" },
            new() { id = UiButtonId.Renovate, label = "Fix up" },
            new() { id = UiButtonId.Minigame, label = "Play" },
        };

        public bool TryGet(UiButtonId id, out UiButtonSpriteEntry entry)
        {
            if (buttons != null)
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i].id == id)
                    {
                        entry = buttons[i];
                        return true;
                    }
                }
            }

            entry = default;
            return false;
        }

        public Sprite GetIcon(UiButtonId id)
        {
            return TryGet(id, out var entry) ? entry.icon : null;
        }

        public string GetLabel(UiButtonId id)
        {
            if (TryGet(id, out var entry) && !string.IsNullOrWhiteSpace(entry.label))
                return entry.label;
            return id.ToString();
        }
    }
}
