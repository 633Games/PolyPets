using UnityEngine;
using UnityEngine.UI;

namespace PolyPets.Minigames
{
    /// <summary>
    /// Simple overlay for minigame prompts + optional stage root for visuals.
    /// </summary>
    public sealed class MinigameHud : MonoBehaviour
    {
        [SerializeField] private Text promptText;
        [SerializeField] private Text scoreText;
        [SerializeField] private GameObject root;

        public GameObject Root => root;
        public Transform RootTransform => root != null ? root.transform : null;

        public void Bind(Text prompt, Text score, GameObject panelRoot)
        {
            promptText = prompt;
            scoreText = score;
            root = panelRoot;
        }

        public void Show(string prompt, string score = null)
        {
            if (root != null)
                root.SetActive(true);
            if (promptText != null)
                promptText.text = prompt;
            if (scoreText != null)
                scoreText.text = score ?? string.Empty;
        }

        public void SetScore(string score)
        {
            if (scoreText != null)
                scoreText.text = score;
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
        }
    }
}
