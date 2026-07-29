using UnityEngine;
using UnityEngine.UI;
using PolyPets.Economy;
using PolyPets.Pets;
using PolyPets.Shop;

namespace PolyPets.UI
{
    /// <summary>
    /// Species food shop: Budget / Medium / Super for Cat, Dog, Rabbit.
    /// Soft parchment cards instead of dark formal rows.
    /// </summary>
    public sealed class FoodShopPanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private Transform buttonRoot;
        [SerializeField] private FoodInventory inventory;
        [SerializeField] private EconomyService economy;
        [SerializeField] private House.HouseController house;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button dimmerButton;

        private readonly System.Collections.Generic.List<Button> _spawned = new();
        private Sprite _rowSprite;

        public bool IsOpen => root != null && root.activeSelf;

        public void Bind(
            GameObject panelRoot,
            Text title,
            Text body,
            Transform buttonsParent,
            Button close,
            FoodInventory inv,
            EconomyService eco,
            House.HouseController houseController,
            Button dimmer = null)
        {
            root = panelRoot;
            titleText = title;
            bodyText = body;
            buttonRoot = buttonsParent;
            closeButton = close;
            dimmerButton = dimmer;
            inventory = inv;
            economy = eco;
            house = houseController;
            WireCloseButtons();
        }

        private void OnEnable() => WireCloseButtons();

        private void WireCloseButtons()
        {
            if (closeButton == null && root != null)
            {
                var closeTf = root.transform.Find("ShopPanel/Close");
                if (closeTf != null)
                    closeButton = closeTf.GetComponent<Button>();
            }

            if (dimmerButton == null && root != null)
            {
                var dimTf = root.transform.Find("Dimmer");
                if (dimTf != null)
                    dimmerButton = dimTf.GetComponent<Button>();
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
                closeButton.onClick.AddListener(Hide);
            }

            if (dimmerButton != null)
            {
                dimmerButton.onClick.RemoveListener(Hide);
                dimmerButton.onClick.AddListener(Hide);
            }
        }

        public void Toggle()
        {
            if (IsOpen) Hide();
            else Show();
        }

        public void Show()
        {
            WireCloseButtons();
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

            var pet = house?.ActiveRoom?.Occupant;
            var species = pet?.Definition != null ? pet.Definition.species : PetSpecies.Cat;
            string petName = pet != null ? pet.PetName : "your pal";

            if (titleText != null)
                titleText.text = $"Little pantry";
            if (bodyText != null)
            {
                int coins = economy != null ? economy.Coins : 0;
                int stock = inventory != null ? inventory.CountForSpecies(species) : 0;
                bodyText.text = $"Treats for {petName} ({species})\n✦ {coins} · {stock} ready to serve";
            }

            if (inventory?.Catalog == null || buttonRoot == null)
                return;

            float y = 0.68f;
            foreach (var item in inventory.CatalogForSpecies(species))
            {
                var button = CreateRow(item, new Vector2(0.5f, y));
                _spawned.Add(button);
                y -= 0.16f;
            }
        }

        private Button CreateRow(FoodItemDefinition item, Vector2 anchor)
        {
            EnsureRowSprite();

            var go = new GameObject($"Buy_{item.foodId}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(buttonRoot, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(340f, 56f);
            var bg = go.GetComponent<Image>();
            bg.sprite = _rowSprite;
            bg.type = Image.Type.Sliced;
            bg.color = TierTint(item.tier);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(16f, 6f);
            lrt.offsetMax = new Vector2(-16f, -6f);
            var text = labelGo.GetComponent<Text>();
            int owned = inventory != null ? inventory.GetCount(item) : 0;
            text.text = $"{FriendlyTier(item.tier)}  ·  ✦{item.priceCoins}  ·  +{item.hungerRestore:0} tummy\n{owned} in the cupboard";
            text.alignment = TextAnchor.MiddleLeft;
            text.fontSize = 13;
            text.color = CozyUiTheme.Cocoa;
            text.font = CozyUiTheme.UiFont;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.98f, 0.94f, 1f);
            colors.pressedColor = new Color(0.94f, 0.88f, 0.8f, 1f);
            button.colors = colors;

            var captured = item;
            button.onClick.AddListener(() => OnBuy(captured));
            return button;
        }

        private void OnBuy(FoodItemDefinition item)
        {
            if (inventory == null)
                return;

            if (inventory.TryBuy(item))
            {
                if (bodyText != null)
                    bodyText.text = $"Got {item.displayName}! Tap the bowl when you're ready.";
                Rebuild();
            }
            else
            {
                if (bodyText != null)
                    bodyText.text = $"Need ✦{item.priceCoins} — a quick play should help!";
            }
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

        private void EnsureRowSprite()
        {
            if (_rowSprite != null)
                return;
            _rowSprite = CozyUiTheme.CreateRoundedSprite(64, 16, Color.white, CozyUiTheme.MeterTrack, 2);
        }

        private static string FriendlyTier(FoodTier tier) => tier switch
        {
            FoodTier.Medium => "Homey meal",
            FoodTier.Super => "Special treat",
            _ => "Simple snack",
        };

        private static Color TierTint(FoodTier tier) => tier switch
        {
            FoodTier.Medium => new Color(1f, 0.93f, 0.82f, 1f),
            FoodTier.Super => new Color(1f, 0.88f, 0.84f, 1f),
            _ => new Color(0.96f, 0.93f, 0.88f, 1f),
        };

        private void OnDestroy()
        {
            if (_rowSprite != null && _rowSprite.texture != null)
                Destroy(_rowSprite.texture);
        }
    }
}
