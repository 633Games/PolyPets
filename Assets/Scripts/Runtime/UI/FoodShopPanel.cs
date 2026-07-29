using UnityEngine;
using UnityEngine.UI;
using PolyPets.Economy;
using PolyPets.Pets;
using PolyPets.Shop;

namespace PolyPets.UI
{
    /// <summary>
    /// Species food shop: Budget / Medium / Super for Cat, Dog, Rabbit.
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

        private readonly System.Collections.Generic.List<Button> _spawned = new();

        public bool IsOpen => root != null && root.activeSelf;

        public void Bind(
            GameObject panelRoot,
            Text title,
            Text body,
            Transform buttonsParent,
            Button close,
            FoodInventory inv,
            EconomyService eco,
            House.HouseController houseController)
        {
            root = panelRoot;
            titleText = title;
            bodyText = body;
            buttonRoot = buttonsParent;
            closeButton = close;
            inventory = inv;
            economy = eco;
            house = houseController;

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

            var pet = house?.ActiveRoom?.Occupant;
            var species = pet?.Definition != null ? pet.Definition.species : PetSpecies.Cat;
            string petName = pet != null ? pet.PetName : "your pet";

            if (titleText != null)
                titleText.text = $"{species} Food Shop";
            if (bodyText != null)
            {
                int coins = economy != null ? economy.Coins : 0;
                int stock = inventory != null ? inventory.CountForSpecies(species) : 0;
                bodyText.text = $"Buying for {petName}.\nCoins: {coins} · {species} food owned: {stock}\nBudget / Medium / Super restore different hunger.";
            }

            if (inventory?.Catalog == null || buttonRoot == null)
                return;

            float y = 0.72f;
            foreach (var item in inventory.CatalogForSpecies(species))
            {
                var button = CreateRow(item, new Vector2(0.5f, y));
                _spawned.Add(button);
                y -= 0.18f;
            }
        }

        private Button CreateRow(FoodItemDefinition item, Vector2 anchor)
        {
            var go = new GameObject($"Buy_{item.foodId}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(buttonRoot, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(340f, 52f);
            go.GetComponent<Image>().color = TierColor(item.tier);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(10f, 4f);
            lrt.offsetMax = new Vector2(-10f, -4f);
            var text = labelGo.GetComponent<Text>();
            int owned = inventory != null ? inventory.GetCount(item) : 0;
            text.text = $"{item.TierLabel} — {item.priceCoins}c   +{item.hungerRestore:0} hunger   (own {owned})";
            text.alignment = TextAnchor.MiddleLeft;
            text.fontSize = 14;
            text.color = new Color(0.95f, 0.92f, 0.88f);
            text.font = UiFonts.Body;
            text.raycastTarget = false;

            var button = go.GetComponent<Button>();
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
                    bodyText.text = $"Bought {item.displayName}! Click the bowl to feed.";
                Rebuild();
            }
            else
            {
                if (bodyText != null)
                    bodyText.text = $"Need {item.priceCoins} coins — play a minigame first!";
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

        private static Color TierColor(FoodTier tier) => tier switch
        {
            FoodTier.Medium => new Color(0.28f, 0.32f, 0.4f, 1f),
            FoodTier.Super => new Color(0.42f, 0.3f, 0.18f, 1f),
            _ => new Color(0.22f, 0.2f, 0.18f, 1f),
        };
    }
}
