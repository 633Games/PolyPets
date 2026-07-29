using UnityEngine;

namespace PolyPets.UI
{
    /// <summary>
    /// Root HUD that holds the sprite pack and can refresh all chrome buttons under it.
    /// </summary>
    public sealed class UiHudRoot : MonoBehaviour
    {
        [SerializeField] private UiSpritePack spritePack;
        [SerializeField] private Transform buttonRoot;

        public UiSpritePack SpritePack => spritePack;

        public void SetSpritePack(UiSpritePack pack)
        {
            spritePack = pack;
            RefreshButtons();
        }

        public void RefreshButtons()
        {
            if (spritePack == null)
                return;

            var buttons = GetComponentsInChildren<UiChromeButton>(true);
            for (int i = 0; i < buttons.Length; i++)
                buttons[i].SetPack(spritePack);
        }

        private void OnEnable() => RefreshButtons();
    }
}
