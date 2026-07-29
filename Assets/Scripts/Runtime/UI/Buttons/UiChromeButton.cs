using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using PolyPets.Feel;

namespace PolyPets.UI
{
    /// <summary>
    /// Prefab-friendly chrome button. Bind a <see cref="UiSpritePack"/> (or inherit from parent),
    /// set <see cref="buttonId"/>, and the icon/label refresh from the pack.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class UiChromeButton : MonoBehaviour
    {
        [SerializeField] private UiButtonId buttonId = UiButtonId.Shop;
        [SerializeField] private UiSpritePack spritePack;
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private Text label;
        [SerializeField] private bool useDangerColor;
        [SerializeField] private bool applyFeelPop = true;

        public UiButtonId ButtonId => buttonId;
        public Button Button => _button != null ? _button : _button = GetComponent<Button>();

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            ApplyPack();
            if (applyFeelPop && GetComponent<FeelTag>() == null)
                FeelTagBinder.EnsureTagOn(gameObject, FeelTagType.Punch, autoPlay: false);
        }

        private void OnEnable() => ApplyPack();

        public void SetPack(UiSpritePack pack)
        {
            spritePack = pack;
            ApplyPack();
        }

        public void SetId(UiButtonId id)
        {
            buttonId = id;
            ApplyPack();
        }

        public void ApplyPack()
        {
            if (spritePack == null)
                return;

            if (background != null)
            {
                if (spritePack.buttonBackground != null)
                    background.sprite = spritePack.buttonBackground;
                background.color = useDangerColor ? spritePack.dangerColor : Color.white;
            }

            if (icon != null)
            {
                var spr = spritePack.GetIcon(buttonId);
                icon.enabled = spr != null;
                icon.sprite = spr;
                icon.color = spritePack.accentColor;
            }

            if (label != null)
            {
                label.text = spritePack.GetLabel(buttonId);
                label.color = spritePack.labelColor;
            }

            if (_button == null)
                _button = GetComponent<Button>();

            if (_button != null)
            {
                var colors = _button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 0.97f, 0.92f, 1f);
                colors.pressedColor = new Color(0.94f, 0.88f, 0.78f, 1f);
                colors.disabledColor = new Color(0.85f, 0.82f, 0.78f, 0.55f);
                _button.colors = colors;
            }
        }

        public void AddListener(UnityAction action)
        {
            Button.onClick.AddListener(action);
        }

        public void PlayClickFeel()
        {
            var tag = GetComponent<FeelTag>();
            if (tag != null)
                tag.Play();
            else
                GetComponent<FeelPunchScale>()?.Play();
        }
    }
}
