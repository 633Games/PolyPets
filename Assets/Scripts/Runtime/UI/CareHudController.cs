using UnityEngine;
using UnityEngine.UI;
using PolyPets.Desktop;
using PolyPets.Economy;
using PolyPets.House;
using PolyPets.Minigames;
using PolyPets.Pets;
using PolyPets.Shop;

namespace PolyPets.UI
{
    /// <summary>
    /// Care chrome: food shop, décor shop, feed, clean scrub, play, room change.
    /// </summary>
    public sealed class CareHudController : MonoBehaviour
    {
        [SerializeField] private EconomyService economy;
        [SerializeField] private FoodInventory inventory;
        [SerializeField] private DecorationInventory decorationInventory;
        [SerializeField] private MinigameRouter minigames;
        [SerializeField] private HouseController house;
        [SerializeField] private HudController hud;
        [SerializeField] private FoodShopPanel shopPanel;
        [SerializeField] private DecorationShopPanel decorationShopPanel;
        [SerializeField] private PetCleanScrubber cleanScrubber;
        [SerializeField] private DesktopWindowController desktopWindow;
        [SerializeField] private SettingsStubPanel settingsPanel;
        [SerializeField] private Text hungerText;
        [SerializeField] private Text happinessText;
        [SerializeField] private Text cleanText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text foodStockText;
        [SerializeField] private UiChromeButton feedButton;
        [SerializeField] private UiChromeButton shopButton;
        [SerializeField] private UiChromeButton decorateButton;
        [SerializeField] private UiChromeButton cleanButton;
        [SerializeField] private UiChromeButton minigameButton;
        [SerializeField] private UiChromeButton playButton;
        [SerializeField] private UiChromeButton prevRoomButton;
        [SerializeField] private UiChromeButton nextRoomButton;
        [SerializeField] private UiChromeButton homeButton;
        [SerializeField] private UiChromeButton settingsButton;
        [SerializeField] private UiChromeButton alwaysOnTopButton;

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
            if (decorationInventory != null)
                decorationInventory.InventoryChanged += RefreshNeedsUi;

            WireButtons();
            RefreshAll();
        }

        private void OnDisable()
        {
            if (economy != null)
                economy.CoinsChanged -= OnCoinsChanged;
            if (inventory != null)
                inventory.InventoryChanged -= RefreshNeedsUi;
            if (decorationInventory != null)
                decorationInventory.InventoryChanged -= RefreshNeedsUi;
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
            btn.Button.onClick.RemoveAllListeners();
            btn.Button.onClick.AddListener(action);
            btn.Button.onClick.AddListener(btn.PlayClickFeel);
        }

        private void OnCoinsChanged(int coins)
        {
            hud?.SetCoins(coins);
            RefreshNeedsUi();
            if (shopPanel != null && shopPanel.IsOpen)
                shopPanel.Rebuild();
            if (decorationShopPanel != null && decorationShopPanel.IsOpen)
                decorationShopPanel.Rebuild();
        }

        private void OnFeedClicked()
        {
            var pet = ActivePet;
            if (pet == null)
            {
                SetStatus("Finish the tutorial first!");
                return;
            }

            var bowl = pet.GetComponentInChildren<PetFoodBowl>(true);
            if (bowl != null)
            {
                bool ok = bowl.TryFeedFromBowl();
                if (!ok && pet.Needs != null && pet.Needs.IsFull)
                    SetStatus($"{pet.PetName} is full — wait until hunger drops.");
                else if (!ok)
                    SetStatus($"No {pet.Definition?.species} food — open Shop.");
                else
                    SetStatus($"Fed {pet.PetName}!");
                RefreshNeedsUi();
                return;
            }

            SetStatus("Click the bowl next to your pet to feed.");
        }

        private void OnShopClicked()
        {
            if (ActivePet == null)
            {
                SetStatus("Finish the tutorial first!");
                return;
            }

            decorationShopPanel?.Hide();
            settingsPanel?.Hide();
            if (shopPanel == null)
            {
                Debug.LogWarning("[PolyPets] Food shop panel missing.");
                return;
            }

            shopPanel.Toggle();
        }

        private void OnDecorateClicked()
        {
            if (ActivePet == null)
            {
                SetStatus("Finish the tutorial first!");
                return;
            }

            shopPanel?.Hide();
            settingsPanel?.Hide();
            if (decorationShopPanel == null)
            {
                Debug.LogWarning("[PolyPets] Décor shop missing.");
                return;
            }

            decorationShopPanel.Toggle();
        }

        private void OnCleanClicked()
        {
            var pet = ActivePet;
            if (pet == null)
            {
                SetStatus("Finish the tutorial first!");
                return;
            }

            cleanScrubber ??= PetCleanScrubber.Instance ?? FindFirstObjectByType<PetCleanScrubber>();
            if (cleanScrubber == null)
            {
                SetStatus("Clean system missing.");
                return;
            }

            if (cleanScrubber.IsScrubMode)
            {
                cleanScrubber.CancelClean();
                return;
            }

            shopPanel?.Hide();
            decorationShopPanel?.Hide();
            cleanScrubber.BeginClean(pet);
        }

        private void OnMinigameClicked()
        {
            if (ActivePet == null)
            {
                SetStatus("Finish the tutorial first!");
                return;
            }

            if (minigames == null || minigames.IsBusy)
                return;

            minigames.SetActivePet(ActivePet);
            minigames.PlayActivePetMinigame();
            var blurb = ActivePet.Definition != null ? ActivePet.Definition.minigameBlurb : "Play!";
            SetStatus(blurb);
        }

        private void OnHomeClicked()
        {
            if (house == null || house.Rooms == null || house.Rooms.Count == 0)
                return;
            house.SetActiveRoom(0);
            hud?.SetRoomName(house.ActiveRoom?.DisplayName ?? "Living Room");
            decorationShopPanel?.Rebuild();
            SetStatus("Home — Living Room");
            RefreshNeedsUi();
        }

        private void OnSettingsClicked()
        {
            shopPanel?.Hide();
            decorationShopPanel?.Hide();
            if (settingsPanel != null)
                settingsPanel.Toggle();
            else
                SetStatus("Settings — Coming soon");
        }

        private void OnAlwaysOnTopClicked()
        {
            desktopWindow ??= FindFirstObjectByType<DesktopWindowController>();
            if (desktopWindow == null)
            {
                SetStatus("Always-on-top — Coming soon on this platform");
                return;
            }

            bool next = !desktopWindow.AlwaysOnTop;
            desktopWindow.SetAlwaysOnTop(next);
            SetStatus(next ? "Always on top — ON" : "Always on top — OFF");
        }

        private void OnPrevRoom()
        {
            house?.PrevRoom();
            hud?.SetRoomName(house?.ActiveRoom?.DisplayName ?? "Room");
            decorationShopPanel?.Rebuild();
            RefreshNeedsUi();
        }

        private void OnNextRoom()
        {
            house?.NextRoom();
            hud?.SetRoomName(house?.ActiveRoom?.DisplayName ?? "Room");
            decorationShopPanel?.Rebuild();
            RefreshNeedsUi();
        }

        public void RefreshAll()
        {
            if (economy != null)
                hud?.SetCoins(economy.Coins);
            hud?.SetRoomName(house?.ActiveRoom?.DisplayName ?? "Room");
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
            if (cleanText != null)
                cleanText.text = needs != null ? $"Clean {needs.Cleanliness:0}" : "Clean —";

            var progression = pet != null ? pet.GetComponent<PetProgression>() : null;
            if (levelText != null)
                levelText.text = progression != null ? progression.StatusLabel() : "Lv —";

            bool shopOpen = (shopPanel != null && shopPanel.IsOpen)
                            || (decorationShopPanel != null && decorationShopPanel.IsOpen)
                            || (cleanScrubber != null && cleanScrubber.IsScrubMode)
                            || (settingsPanel != null && settingsPanel.IsOpen);
            if (statusText != null && needs != null && !shopOpen)
                statusText.text = needs.StatusLabel();

            if (foodStockText != null && inventory != null)
            {
                var species = pet?.Definition != null ? pet.Definition.species : PetSpecies.Cat;
                float mult = HouseBuffs.Instance != null ? HouseBuffs.Instance.CoinEarnMultiplier : 1f;
                foodStockText.text = $"{species} food x{inventory.CountForSpecies(species)} · coins ×{mult:0.00}";
            }
        }

        private void SetStatus(string msg)
        {
            if (statusText != null)
                statusText.text = msg;
        }

        public void Bind(
            EconomyService eco,
            FoodInventory inv,
            MinigameRouter games,
            HouseController houseController,
            HudController hudController,
            FoodShopPanel shop,
            DecorationShopPanel decorShop = null,
            DecorationInventory decorInv = null,
            PetCleanScrubber scrubber = null,
            DesktopWindowController desktop = null,
            SettingsStubPanel settings = null)
        {
            economy = eco;
            inventory = inv;
            minigames = games;
            house = houseController;
            hud = hudController;
            shopPanel = shop;
            decorationShopPanel = decorShop;
            decorationInventory = decorInv;
            cleanScrubber = scrubber;
            desktopWindow = desktop;
            settingsPanel = settings;
            cleanScrubber?.BindHint(statusText);
        }

        public void BindMeters(Text hunger, Text happiness, Text status, Text foodStock, Text clean = null, Text level = null)
        {
            hungerText = hunger;
            happinessText = happiness;
            statusText = status;
            foodStockText = foodStock;
            cleanText = clean;
            levelText = level;
            cleanScrubber?.BindHint(statusText);
        }

        public void BindActionButtons(
            UiChromeButton feed,
            UiChromeButton shop,
            UiChromeButton minigame,
            UiChromeButton play,
            UiChromeButton clean = null,
            UiChromeButton decorate = null,
            UiChromeButton prevRoom = null,
            UiChromeButton nextRoom = null,
            UiChromeButton home = null,
            UiChromeButton settings = null,
            UiChromeButton alwaysOnTop = null)
        {
            feedButton = feed;
            shopButton = shop;
            minigameButton = minigame;
            playButton = play;
            cleanButton = clean;
            decorateButton = decorate;
            prevRoomButton = prevRoom;
            nextRoomButton = nextRoom;
            homeButton = home;
            settingsButton = settings;
            alwaysOnTopButton = alwaysOnTop;
            WireButtons();
        }

        /// <summary>Scan canvas chrome and bind every known interactive id; grey out the rest.</summary>
        public void BindAllChrome(Transform canvasRoot)
        {
            if (canvasRoot == null)
                return;

            foreach (var chrome in canvasRoot.GetComponentsInChildren<UiChromeButton>(true))
            {
                switch (chrome.ButtonId)
                {
                    case UiButtonId.Feed: feedButton ??= chrome; break;
                    case UiButtonId.Shop: shopButton ??= chrome; break;
                    case UiButtonId.Play: playButton ??= chrome; break;
                    case UiButtonId.Minigame: minigameButton ??= chrome; break;
                    case UiButtonId.Clean: cleanButton ??= chrome; break;
                    case UiButtonId.Renovate: decorateButton ??= chrome; break;
                    case UiButtonId.PrevRoom: prevRoomButton ??= chrome; break;
                    case UiButtonId.NextRoom: nextRoomButton ??= chrome; break;
                    case UiButtonId.Home: homeButton ??= chrome; break;
                    case UiButtonId.Settings: settingsButton ??= chrome; break;
                    case UiButtonId.AlwaysOnTop: alwaysOnTopButton ??= chrome; break;
                    case UiButtonId.AcceptWant:
                    case UiButtonId.SnoozeWant:
                    case UiButtonId.Adopt:
                    case UiButtonId.CollectAll:
                    case UiButtonId.PauseTime:
                    case UiButtonId.Inventory:
                    case UiButtonId.Back:
                        UiComingSoon.Apply(chrome, "Coming soon");
                        break;
                }
            }

            WireButtons();
        }

        private void WireButtons()
        {
            BindButton(feedButton, OnFeedClicked);
            BindButton(shopButton, OnShopClicked);
            BindButton(minigameButton, OnMinigameClicked);
            BindButton(playButton, OnMinigameClicked);
            BindButton(cleanButton, OnCleanClicked);
            BindButton(decorateButton, OnDecorateClicked);
            BindButton(prevRoomButton, OnPrevRoom);
            BindButton(nextRoomButton, OnNextRoom);
            BindButton(homeButton, OnHomeClicked);
            BindButton(settingsButton, OnSettingsClicked);
            BindButton(alwaysOnTopButton, OnAlwaysOnTopClicked);
        }
    }
}
