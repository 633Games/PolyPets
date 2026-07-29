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
                            || (cleanScrubber != null && cleanScrubber.IsScrubMode);
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
            PetCleanScrubber scrubber = null)
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
            UiChromeButton nextRoom = null)
        {
            feedButton = feed;
            shopButton = shop;
            minigameButton = minigame;
            playButton = play;
            cleanButton = clean;
            decorateButton = decorate;
            prevRoomButton = prevRoom;
            nextRoomButton = nextRoom;
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
        }
    }
}
