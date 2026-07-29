using System;
using UnityEngine;
using UnityEngine.UI;
using PolyPets.House;
using PolyPets.Minigames;
using PolyPets.Pets;
using PolyPets.Rendering;

namespace PolyPets.Tutorial
{
    /// <summary>
    /// First-run tutorial:
    /// Welcome → name your Poly Pet → choose Cat / Dog / Rabbit → spawn + tip.
    /// </summary>
    public sealed class StarterTutorial : MonoBehaviour
    {
        public enum Step
        {
            Welcome = 0,
            Name = 1,
            ChooseSpecies = 2,
            Done = 3,
        }

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private InputField nameInput;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button catButton;
        [SerializeField] private Button dogButton;
        [SerializeField] private Button rabbitButton;
        [SerializeField] private Text nextButtonLabel;

        [SerializeField] private Transform petParent;
        [SerializeField] private RoomRoot starterRoom;
        [SerializeField] private MinigameRouter minigameRouter;
        [SerializeField] private PetDefinition catDef;
        [SerializeField] private PetDefinition dogDef;
        [SerializeField] private PetDefinition rabbitDef;
        [SerializeField] private MaterialPalette materialPalette;
        [SerializeField] private Material petPrimary;
        [SerializeField] private Material petSecondary;

        [SerializeField] private Step step = Step.Welcome;
        [SerializeField] private string petName = "Mochi";
        [SerializeField] private bool completed;

        public bool IsComplete => completed;
        public PetAgent SpawnedPet { get; private set; }

        public event Action<PetAgent> TutorialCompleted;

        public void BindUi(
            GameObject root,
            Text title,
            Text body,
            InputField input,
            Button next,
            Text nextLabel,
            Button cat,
            Button dog,
            Button rabbit)
        {
            panelRoot = root;
            titleText = title;
            bodyText = body;
            nameInput = input;
            nextButton = next;
            nextButtonLabel = nextLabel;
            catButton = cat;
            dogButton = dog;
            rabbitButton = rabbit;
        }

        public void BindWorld(
            Transform petsRoot,
            RoomRoot room,
            MinigameRouter router,
            PetDefinition cat,
            PetDefinition dog,
            PetDefinition rabbit,
            MaterialPalette palette)
        {
            petParent = petsRoot;
            starterRoom = room;
            minigameRouter = router;
            catDef = cat;
            dogDef = dog;
            rabbitDef = rabbit;
            materialPalette = palette;
            if (palette != null)
                palette.GetPetPair(PetSpecies.Cat, out petPrimary, out petSecondary);
        }

        private void OnEnable()
        {
            WireButtons();
            if (!completed)
                ShowStep(Step.Welcome);
            else
                Hide();
        }

        private void WireButtons()
        {
            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnNext);
            }

            BindSpecies(catButton, PetSpecies.Cat);
            BindSpecies(dogButton, PetSpecies.Dog);
            BindSpecies(rabbitButton, PetSpecies.Rabbit);
        }

        private void BindSpecies(Button button, PetSpecies species)
        {
            if (button == null)
                return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnChooseSpecies(species));
        }

        private void OnNext()
        {
            switch (step)
            {
                case Step.Welcome:
                    ShowStep(Step.Name);
                    break;
                case Step.Name:
                    if (nameInput != null && !string.IsNullOrWhiteSpace(nameInput.text))
                        petName = nameInput.text.Trim();
                    if (string.IsNullOrWhiteSpace(petName))
                        petName = "Mochi";
                    ShowStep(Step.ChooseSpecies);
                    break;
            }
        }

        private void OnChooseSpecies(PetSpecies species)
        {
            var def = species switch
            {
                PetSpecies.Dog => dogDef,
                PetSpecies.Rabbit => rabbitDef,
                _ => catDef,
            };

            if (materialPalette != null)
                materialPalette.GetPetPair(species, out petPrimary, out petSecondary);

            // Prefer ceramic bowl mat from palette when feeding visuals are built.
            var bowlMat = materialPalette != null ? materialPalette.bowlCeramic : petSecondary;
            SpawnedPet = StarterPetFactory.Spawn(species, petName, def, petParent, petPrimary, petSecondary);
            if (SpawnedPet != null && bowlMat != null)
                SpawnedPet.EnsureFoodBowl(bowlMat);
            if (starterRoom != null)
                starterRoom.SetOccupant(SpawnedPet);
            minigameRouter?.SetActivePet(SpawnedPet);

            completed = true;
            step = Step.Done;
            Hide();
            TutorialCompleted?.Invoke(SpawnedPet);

            Debug.Log($"[PolyPets] Welcome, {petName} the {species}! Minigame: {def?.SignatureMinigameName}");
        }

        private void ShowStep(Step next)
        {
            step = next;
            if (panelRoot != null)
                panelRoot.SetActive(true);

            bool naming = next == Step.Name;
            bool choosing = next == Step.ChooseSpecies;

            if (nameInput != null)
            {
                nameInput.gameObject.SetActive(naming);
                if (naming)
                {
                    nameInput.text = petName;
                    nameInput.ActivateInputField();
                }
            }

            if (nextButton != null)
                nextButton.gameObject.SetActive(!choosing);
            SetSpeciesButtons(choosing);

            switch (next)
            {
                case Step.Welcome:
                    SetCopy(
                        "Welcome to PolyPets",
                        "A cozy desktop home for box-headed pals.\nEarn coins in minigames, buy food, keep them happy.");
                    SetNextLabel("Let's go");
                    break;
                case Step.Name:
                    SetCopy(
                        "Name your Poly Pet",
                        "What should we call your new friend?");
                    SetNextLabel("Next");
                    break;
                case Step.ChooseSpecies:
                    SetCopy(
                        $"Pick a pal for {petName}",
                        "Cat — Fishing (nibbles → bite → reel)\nDog — Dig + Snap (holes fill in)\nRabbit — Carrot Farm (harvest before spoil)\n\nMinigames earn the coins you spend on food.");
                    break;
            }
        }

        private void SetSpeciesButtons(bool visible)
        {
            if (catButton != null) catButton.gameObject.SetActive(visible);
            if (dogButton != null) dogButton.gameObject.SetActive(visible);
            if (rabbitButton != null) rabbitButton.gameObject.SetActive(visible);
        }

        private void SetCopy(string title, string body)
        {
            if (titleText != null) titleText.text = title;
            if (bodyText != null) bodyText.text = body;
        }

        private void SetNextLabel(string label)
        {
            if (nextButtonLabel != null)
                nextButtonLabel.text = label;
            else if (nextButton != null)
            {
                var text = nextButton.GetComponentInChildren<Text>();
                if (text != null) text.text = label;
            }
        }

        private void Hide()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }
    }
}
