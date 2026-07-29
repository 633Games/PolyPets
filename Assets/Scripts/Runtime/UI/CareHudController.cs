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
        [SerializeField] private NeedMeterView hungerMeter;
        [SerializeField] private NeedMeterView happinessMeter;
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

        private void Awake()
        {
            ResolveMissingRefs();
        }

        private void OnEnable()
        {
            ResolveMissingRefs();

            if (economy != null)
                economy.CoinsChanged += OnCoinsChanged;
            if (inventory != null)
                inventory.InventoryChanged += RefreshNeedsUi;

            WireButtons();
            RefreshAll();
        }

        private void ResolveMissingRefs()
        {
            if (shopPanel == null)
                shopPanel = GetComponentInChildren<FoodShopPanel>(true);
            if (shopPanel == null)
                shopPanel = FindFirstObjectByType<FoodShopPanel>(FindObjectsInactive.Include);

            if (hud == null)
                hud = GetComponent<HudController>();

            if (house == null)
                house = FindFirstObjectByType<HouseController>();
            if (economy == null)
                economy = FindFirstObjectByType<EconomyService>();
            if (inventory == null)
                inventory = FindFirstObjectByType<FoodInventory>();
            if (minigames == null)
                minigames = FindFirstObjectByType<MinigameRouter>();

            foreach (var chrome in GetComponentsInChildren<UiChromeButton>(true))
            {
                switch (chrome.ButtonId)
                {
                    case UiButtonId.Feed when feedButton == null:
                        feedButton = chrome;
                        break;
                    case UiButtonId.Shop when shopButton == null:
                        shopButton = chrome;
                        break;
                    case UiButtonId.Play when playButton == null:
                        playButton = chrome;
                        break;
                    case UiButtonId.Minigame when minigameButton == null:
                        minigameButton = chrome;
                        break;
                }
            }

            // Name fallback when ButtonId was never serialized (legacy / incomplete HUD).
            if (feedButton == null)
                feedButton = FindChromeByName("Btn_Feed", "Feed");
            if (shopButton == null)
                shopButton = FindChromeByName("Btn_Shop", "Shop");
            if (playButton == null)
                playButton = FindChromeByName("Btn_Play", "Play");
            if (minigameButton == null)
                minigameButton = FindChromeByName("Btn_Minigame", "Minigame");
        }

        private UiChromeButton FindChromeByName(params string[] names)
        {
            foreach (var chrome in GetComponentsInChildren<UiChromeButton>(true))
            {
                if (chrome == null)
                    continue;
                var n = chrome.gameObject.name;
                for (int i = 0; i < names.Length; i++)
                {
                    if (string.Equals(n, names[i], System.StringComparison.OrdinalIgnoreCase))
                        return chrome;
                }
            }

            return null;
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
            ResolveMissingRefs();

            if (shopPanel == null)
            {
                if (statusText != null)
                    statusText.text = "Shop isn't ready — re-run Cozy HUD.";
                Debug.LogWarning("[PolyPets] Food shop panel missing.");
                return;
            }

            shopPanel.Toggle();
            if (statusText != null && shopPanel.IsOpen)
                statusText.text = "Little pantry open";
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
                hungerText.text = needs != null ? $"Tummy {needs.Hunger:0}" : "Tummy —";
            if (happinessText != null)
                happinessText.text = needs != null ? $"Mood {needs.Happiness:0}" : "Mood —";

            hungerMeter?.Set(needs != null ? needs.Hunger : 0f);
            happinessMeter?.Set(needs != null ? needs.Happiness : 0f);

            if (statusText != null && needs != null && (shopPanel == null || !shopPanel.IsOpen))
                statusText.text = CozyStatus(needs.StatusLabel());

            if (foodStockText != null && inventory != null)
            {
                var species = pet?.Definition != null ? pet.Definition.species : PetSpecies.Cat;
                foodStockText.text = $"Treats ×{inventory.CountForSpecies(species)}";
            }
        }

        private static string CozyStatus(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return "Settling in…";

            return raw switch
            {
                "Okay" => "Just hanging out",
                "Content" => "Feeling cozy ♥",
                "Starving…" => "Really wants a snack",
                "Full — wait a bit" => "Full & sleepy",
                "Hungry & sad" => "Needs food and a little cheer",
                "Hungry — use the bowl" => "A little peckish…",
                "Needs cheer" => "Could use some playtime",
                _ => raw
            };
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

        public void BindNeedMeters(NeedMeterView hunger, NeedMeterView happiness)
        {
            hungerMeter = hunger;
            happinessMeter = happiness;
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
