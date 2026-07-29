#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using PolyPets.Core;

namespace PolyPets.EditorTools
{
    /// <summary>
    /// Applies 633 Games company / product branding to Player Settings and pings brand art.
    /// </summary>
    public static class StudioBrandingSetup
    {
        private const string RootMenu = "PolyPets/Studio/";

        [MenuItem(RootMenu + "Apply 633 Games Player Branding", priority = 0)]
        public static void ApplyMenu()
        {
            ApplyPlayerBranding();
            EditorUtility.DisplayDialog(
                StudioBrand.StudioName,
                $"Player Settings updated:\n• Company: {StudioBrand.StudioName}\n• Product: {StudioBrand.ProductName}\n\nBrand art: Assets/Art/UI/Brand/",
                "OK");
        }

        public static void ApplyPlayerBranding()
        {
            PlayerSettings.companyName = StudioBrand.StudioName;
            PlayerSettings.productName = StudioBrand.ProductName;
#if UNITY_2021_2_OR_NEWER
            PlayerSettings.applicationIdentifier = $"com.{StudioBrand.StudioId.ToLowerInvariant()}.{StudioBrand.ProductName.ToLowerInvariant()}";
#endif
            Debug.Log($"[PolyPets] Studio branding applied — {StudioBrand.StudioName} / {StudioBrand.ProductName}");
        }
    }
}
#endif
