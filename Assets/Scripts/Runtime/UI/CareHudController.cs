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
    /// Care chrome: Shop opens species food panel; Feed nudges you to click the bowl; Play runs minigame.
    /// </summary>
    public sealed class CareHudController : MonoBehaviour
    {
        [SerializeField] private EconomyService economy;
        [SerializeField] private FoodInventory inventory;
        [SerializeField] private MinigameRouter minigames;
        [SerializeField] private HouseController house;
        [SerializeField] private HudController hud;
        [SerializeField] private FoodShopPanel shopPanel;
        [SerializeField] private Text hungerText;
        [SerializeField] private Text happinessText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text foodStockText;
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
            if (shopPanel != null && shopPanel.IsOpen)
                shopPanel.Rebuild();
        }

        private void OnFeedClicked()
        {
            var pet = ActivePet;
            if (pet == null)
            {
                if (statusText != null)
                    statusText.text = "Finish the tutorial first!";
                return;
            }

            // Prefer the bowl interaction; button is a convenience shortcut to the same logic.
            var bowl = pet.GetComponentInChildren<PetFoodBowl>(true);
            if (bowl != null)
            {
                bool ok = bowl.TryFeedFromBowl();
                if (!ok && pet.Needs != null && pet.Needs.IsFull)
                {
                    if (statusText != null)
                        statusText.text = $"{pet.PetName} is full — wait until hunger drops.";
                }
                else if (!ok)
                {
                    if (statusText != null)
                        statusText.text = $"No {pet.Definition?.species} food — open Shop.";
                }
                else if (statusText != null)
                {
                    statusText.text = $"Fed {pet.PetName}!";
                }

                RefreshNeedsUi();
                return;
            }

            if (statusText != null)
                statusText.text = "Click the bowl next to your pet to feed.";
        }

        private void OnShopClicked()
        {
            if (ActivePet == null)
            {
                if (statusText != null)
                    statusText.text = "Finish the tutorial first!";
                return;
            }

            if (shopPanel == null)
            {
                Debug.LogWarning("[PolyPets] Food shop panel missing.");
                return;
            }

            shopPanel.Toggle();
        }

        private void OnMinigameClicked()
        {
            if (ActivePet == null)
            {
                if (statusText != null)
                    statusText.text = "Finish the tutorial first!";
                return;
            }

            if (minigames == null || minigames.IsBusy)
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
            if (statusText != null && needs != null && (shopPanel == null || !shopPanel.IsOpen))
                statusText.text = needs.StatusLabel();

            if (foodStockText != null && inventory != null)
            {
                var species = pet?.Definition != null ? pet.Definition.species : PetSpecies.Cat;
                foodStockText.text = $"{species} food x{inventory.CountForSpecies(species)}";
            }
        }

        public void Bind(
            EconomyService eco,
            FoodInventory inv,
            MinigameRouter games,
            HouseController houseController,
            HudController hudController,
            FoodShopPanel shop)
        {
            economy = eco;
            inventory = inv;
            minigames = games;
            house = houseController;
            hud = hudController;
            shopPanel = shop;
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
