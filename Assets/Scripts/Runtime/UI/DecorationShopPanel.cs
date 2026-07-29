using UnityEngine;
using UnityEngine.UI;
using PolyPets.Economy;
using PolyPets.House;
using PolyPets.Shop;

namespace PolyPets.UI
{
    /// <summary>
    /// Shop for room decorations. Buy then auto-place into the active room's free slot.
    /// </summary>
    public sealed class DecorationShopPanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Transform buttonRoot;
        [SerializeField] private DecorationInventory inventory;
        [SerializeField] private EconomyService economy;
        [SerializeField] private HouseController house;
        [SerializeField] private Button closeButton;

        private readonly System.Collections.Generic.List<Button> _spawned = new();
        private System.Func<string, Material> _materialLookup;

        public bool IsOpen => root != null && root.activeSelf;

        public void Bind(
            GameObject panelRoot,
            Text title,
            Text body,
            Transform buttonsParent,
            Button close,
            DecorationInventory inv,
            EconomyService eco,
            HouseController houseController,
            System.Func<string, Material> materialLookup = null)
        {
            root = panelRoot;
            titleText = title;
            bodyText = body;
            buttonRoot = buttonsParent;
            closeButton = close;
            inventory = inv;
            economy = eco;
            house = houseController;
            _materialLookup = materialLookup;

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Hide);
            }
        }

        public void Toggle()
        {
            if (IsOpen) Hide();
            else Show();
        }

        public void Show()
        {
            if (root != null)
                root.SetActive(true);
            Rebuild();
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
        }

        public void Rebuild()
        {
            ClearButtons();
            var room = house?.ActiveRoom;
            string roomName = room != null ? room.DisplayName : "Room";
            string roomId = room != null ? room.RoomId : "";
            int free = room != null ? room.FreeSlotCount : 0;

            if (titleText != null)
                titleText.text = "Décor Shop";
            if (bodyText != null)
            {
                int coins = economy != null ? economy.Coins : 0;
                float mult = HouseBuffs.Instance != null ? HouseBuffs.Instance.CoinEarnMultiplier : 1f;
                bodyText.text =
                    $"Decorating: {roomName} ({free} free slots)\n" +
                    $"Coins: {coins} · House coin bonus ×{mult:0.00}\n" +
                    "Decorations boost happiness comfort & minigame earnings.";
            }

            if (inventory?.Catalog == null || buttonRoot == null)
                return;

            float y = 0.72f;
            foreach (var item in inventory.Catalog)
            {
                if (item == null)
                    continue;
                if (room != null && !item.AllowedInRoom(roomId))
                    continue;
                var button = CreateRow(item, new Vector2(0.5f, y));
                _spawned.Add(button);
                y -= 0.14f;
                if (y < 0.18f)
                    break;
            }
        }

        private Button CreateRow(DecorationDefinition item, Vector2 anchor)
        {
            var go = new GameObject($"Buy_{item.decorationId}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(buttonRoot, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(360f, 48f);
            go.GetComponent<Image>().color = new Color(0.2f, 0.24f, 0.22f, 1f);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(8f, 2f);
            lrt.offsetMax = new Vector2(-8f, -2f);
            var text = labelGo.GetComponent<Text>();
            int owned = inventory != null ? inventory.GetCount(item) : 0;
            text.text =
                $"{item.displayName} — {item.priceCoins}c   " +
                $"+{item.coinEarnBonus * 100f:0}% coins · own {owned}";
            text.alignment = TextAnchor.MiddleLeft;
            text.fontSize = 13;
            text.color = new Color(0.95f, 0.92f, 0.88f);
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                        ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.raycastTarget = false;

            var button = go.GetComponent<Button>();
            var captured = item;
            button.onClick.AddListener(() => OnBuy(captured));
            return button;
        }

        private void OnBuy(DecorationDefinition item)
        {
            var room = house?.ActiveRoom;
            if (room == null)
            {
                if (bodyText != null)
                    bodyText.text = "No active room.";
                return;
            }

            if (room.FreeSlotCount <= 0)
            {
                if (bodyText != null)
                    bodyText.text = $"{room.DisplayName} is full — try another room.";
                return;
            }

            if (inventory == null || !inventory.TryBuy(item))
            {
                if (bodyText != null)
                    bodyText.text = $"Need {item.priceCoins} coins — play a minigame!";
                return;
            }

            // Place immediately from purchase (demo-friendly).
            inventory.TryConsume(item, 1);
            Material mat = null;
            if (_materialLookup != null && !string.IsNullOrEmpty(item.materialName))
                mat = _materialLookup(item.materialName);

            if (room.TryPlaceDecoration(item, mat))
            {
                if (bodyText != null)
                    bodyText.text = $"Placed {item.displayName} in {room.DisplayName}!";
            }
            else
            {
                inventory.Add(item, 1);
                if (bodyText != null)
                    bodyText.text = "Couldn't place — slot full or wrong room.";
            }

            Rebuild();
        }

        private void ClearButtons()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                    Destroy(_spawned[i].gameObject);
            }

            _spawned.Clear();
        }
    }
}
