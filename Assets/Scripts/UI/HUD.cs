using UnityEngine;
using UnityEngine.UI;

namespace LunarEyes.UI
{
    /// <summary>HUD mínimo: prompt de interação e painel do terminal.</summary>
    public class HUD : MonoBehaviour
    {
        public static HUD Instance { get; private set; }

        [SerializeField] GameObject promptRoot;
        [SerializeField] Text promptText;
        [SerializeField] GameObject terminalPanel;

        void Awake()
        {
            Instance = this;
            SetPrompt(null);
            ShowTerminal(false);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetPrompt(string text)
        {
            bool show = !string.IsNullOrEmpty(text);
            if (promptRoot != null && promptRoot.activeSelf != show) promptRoot.SetActive(show);
            if (show && promptText != null) promptText.text = text;
        }

        public void ShowTerminal(bool show)
        {
            if (terminalPanel != null) terminalPanel.SetActive(show);
        }
    }
}
