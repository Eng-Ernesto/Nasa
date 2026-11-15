using System;
using UnityEngine;
using UnityEngine.UI;

namespace LunarEyes.UI
{
    /// <summary>Ecrã inicial (design "01 · Ecrã inicial"): modo de jogo, iniciar missão e ajuda.</summary>
    public class TitleScreen : MonoBehaviour
    {
        public enum Mode { Explorer, Scientist }
        const string ModeKey = "lunareyes.mode";

        public event Action StartRequested;

        [SerializeField] CanvasGroup group;
        [SerializeField] Button startButton;
        [SerializeField] Button helpButton;
        [SerializeField] GameObject helpPanel;
        [SerializeField] Button closeHelpButton;
        [SerializeField] Toggle explorerToggle;
        [SerializeField] Toggle scientistToggle;
        [SerializeField] Image explorerCard;
        [SerializeField] Image scientistCard;

        public static Mode SelectedMode { get; private set; } = Mode.Explorer;

        void Awake()
        {
            SelectedMode = PlayerPrefs.GetInt(ModeKey, 0) == 1 ? Mode.Scientist : Mode.Explorer;
            if (explorerToggle != null) explorerToggle.SetIsOnWithoutNotify(SelectedMode == Mode.Explorer);
            if (scientistToggle != null) scientistToggle.SetIsOnWithoutNotify(SelectedMode == Mode.Scientist);
            RefreshCards();

            if (startButton != null) startButton.onClick.AddListener(() => StartRequested?.Invoke());
            if (helpButton != null) helpButton.onClick.AddListener(() => SetHelp(true));
            if (closeHelpButton != null) closeHelpButton.onClick.AddListener(() => SetHelp(false));
            if (explorerToggle != null) explorerToggle.onValueChanged.AddListener(on => { if (on) SetMode(Mode.Explorer); });
            if (scientistToggle != null) scientistToggle.onValueChanged.AddListener(on => { if (on) SetMode(Mode.Scientist); });
            SetHelp(false);
        }

        public void Show(bool show)
        {
            gameObject.SetActive(true);
            if (group != null)
            {
                group.alpha = show ? 1f : 0f;
                group.interactable = show;
                group.blocksRaycasts = show;
            }
            if (show && startButton != null) startButton.Select();
        }

        void SetMode(Mode mode)
        {
            SelectedMode = mode;
            PlayerPrefs.SetInt(ModeKey, mode == Mode.Scientist ? 1 : 0);
            RefreshCards();
        }

        void RefreshCards()
        {
            Paint(explorerCard, SelectedMode == Mode.Explorer);
            Paint(scientistCard, SelectedMode == Mode.Scientist);
        }

        static void Paint(Image card, bool selected)
        {
            if (card == null) return;
            card.color = selected ? UITheme.SignalCyan.WithAlpha(0.14f) : UITheme.DeepSpace.WithAlpha(0.7f);
            var outline = card.transform.Find("Border")?.GetComponent<Image>();
            if (outline != null) outline.color = selected ? UITheme.SignalCyan : UITheme.BorderSlate;
        }

        void SetHelp(bool show)
        {
            if (helpPanel != null) helpPanel.SetActive(show);
        }
    }
}
