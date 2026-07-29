using UnityEngine;
using UnityEngine.UI;
using PolyPets.Economy;
using PolyPets.House;
using PolyPets.Minigames;
using PolyPets.Pets;
using PolyPets.Shop;

namespace PolyPets.UI
{
    /// <summary>
    /// Wires Feed / Shop / Minigame buttons and shows hunger, happiness, coins.
    /// </summary>
    public sealed class CareHudController : MonoBehaviour
    {
        [SerializeField] private EconomyService economy;
        [SerializeField] private FoodInventory inventory;
        [SerializeField] private MinigameRouter minigames;
        [SerializeField] private HouseController house;
        [SerializeField] private HudController hud;
        [SerializeField] private Text hungerText;
        [SerializeField] private Text happinessText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text foodStockText;
        [SerializeField] private FoodItemDefinition defaultShopFood;
        [SerializeField] private UiChromeButton feedButton;
        [SerializeField] private UiChromeButton shopButton;
        [SerializeField] private UiChromeButton minigameButton;
        [SerializeField] private UiChromeButton playButton;

        private PetAgent ActivePet
        {
            get
            {
                var room = house != null ? house.ActiveRoom : null;
                return room != null ? room.Occupant : null;
            }
        }

        private void OnEnable()
        {
            if (economy != null)
                economy.CoinsChanged += OnCoinsChanged;
            if (inventory != null)
                inventory.InventoryChanged += RefreshNeedsUi;

            WireButtons();
            RefreshAll();
        }

        private void OnDisable()
        {
            if (economy != null)
                economy.CoinsChanged -= OnCoinsChanged;
            if (inventory != null)
                inventory.InventoryChanged -= RefreshNeedsUi;
        }

        private void Update()
        {
            // Meters drift — keep labels fresh without event spam every frame.
            if (Time.frameCount % 15 == 0)
                RefreshNeedsUi();
        }

        private static void BindButton(UiChromeButton btn, UnityEngine.Events.UnityAction action)
        {
            if (btn == null)
                return;
            btn.Button.onClick.RemoveListener(action);
            btn.Button.onClick.AddListener(action);
            btn.Button.onClick.AddListener(btn.PlayClickFeel);
        }

        private void OnCoinsChanged(int coins)
        {
            hud?.SetCoins(coins);
            RefreshNeedsUi();
        }

        private void OnFeedClicked()
        {
            var pet = ActivePet;
            if (pet == null)
                return;

            if (inventory != null && inventory.TryConsumeBestAvailable(out var food))
            {
                pet.Needs?.TryFeed(food);
                RefreshNeedsUi();
                return;
            }

            // No stock — nudge player to earn + buy.
            if (statusText != null)
                statusText.text = "No food! Play a minigame, then buy food.";
            Debug.Log("[PolyPets] No food in inventory. Earn coins in a minigame, then use Shop.");
        }

        private void OnShopClicked()
        {
            if (defaultShopFood == null || inventory == null)
            {
                Debug.LogWarning("[PolyPets] Shop food not configured.");
                return;
            }

            if (inventory.TryBuy(defaultShopFood))
            {
                if (statusText != null)
                    statusText.text = $"Bought {defaultShopFood.displayName}!";
                RefreshNeedsUi();
            }
            else
            {
                if (statusText != null)
                    statusText.text = $"Need {defaultShopFood.priceCoins} coins — play a minigame!";
                Debug.Log($"[PolyPets] Can't afford {defaultShopFood.displayName} ({defaultShopFood.priceCoins}c).");
            }
        }

        private void OnMinigameClicked()
        {
            if (ActivePet == null)
            {
                if (statusText != null)
                    statusText.text = "Finish the tutorial first!";
                return;
            }

            if (minigames == null)
                return;
            if (minigames.IsBusy)
                return;

            minigames.SetActivePet(ActivePet);
            minigames.PlayActivePetMinigame();
            var blurb = ActivePet.Definition != null ? ActivePet.Definition.minigameBlurb : "Play!";
            if (statusText != null)
                statusText.text = blurb;
        }

        public void RefreshAll()
        {
            if (economy != null)
                hud?.SetCoins(economy.Coins);
            RefreshNeedsUi();
        }

        private void RefreshNeedsUi()
        {
            var pet = ActivePet;
            var needs = pet != null ? pet.Needs : null;

            if (hungerText != null)
                hungerText.text = needs != null ? $"Hunger {needs.Hunger:0}" : "Hunger —";
            if (happinessText != null)
                happinessText.text = needs != null ? $"Happy {needs.Happiness:0}" : "Happy —";
            if (statusText != null && needs != null)
                statusText.text = needs.StatusLabel();

            if (foodStockText != null && inventory != null)
            {
                int total = 0;
                var stacks = inventory.Stacks;
                for (int i = 0; i < stacks.Count; i++)
                    total += stacks[i].count;
                foodStockText.text = $"Food x{total}";
            }
        }

        public void Bind(
            EconomyService eco,
            FoodInventory inv,
            MinigameRouter games,
            HouseController houseController,
            HudController hudController,
            FoodItemDefinition shopFood)
        {
            economy = eco;
            inventory = inv;
            minigames = games;
            house = houseController;
            hud = hudController;
            defaultShopFood = shopFood;
        }

        public void BindMeters(Text hunger, Text happiness, Text status, Text foodStock)
        {
            hungerText = hunger;
            happinessText = happiness;
            statusText = status;
            foodStockText = foodStock;
        }

        public void BindActionButtons(UiChromeButton feed, UiChromeButton shop, UiChromeButton minigame, UiChromeButton play)
        {
            feedButton = feed;
            shopButton = shop;
            minigameButton = minigame;
            playButton = play;
            WireButtons();
        }

        private void WireButtons()
        {
            BindButton(feedButton, OnFeedClicked);
            BindButton(shopButton, OnShopClicked);
            BindButton(minigameButton, OnMinigameClicked);
            BindButton(playButton, OnMinigameClicked);
        }
    }
}
