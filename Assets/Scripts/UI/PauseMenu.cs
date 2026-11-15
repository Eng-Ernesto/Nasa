using System;
using UnityEngine;
using UnityEngine.UI;

namespace LunarEyes.UI
{
    /// <summary>Menu de pausa: continuar, ligar/desligar som e voltar ao início.</summary>
    public class PauseMenu : MonoBehaviour
    {
        public event Action ResumeRequested;
        public event Action RestartRequested;

        [SerializeField] CanvasGroup group;
        [SerializeField] Button resumeButton;
        [SerializeField] Button soundButton;
        [SerializeField] TMPro.TMP_Text soundLabel;
        [SerializeField] Button restartButton;

        void Awake()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(() => ResumeRequested?.Invoke());
            if (restartButton != null) restartButton.onClick.AddListener(() => RestartRequested?.Invoke());
            if (soundButton != null) soundButton.onClick.AddListener(ToggleSound);
            RefreshSound();
            Show(false);
        }

        public void Show(bool show)
        {
            if (group == null) return;
            group.alpha = show ? 1f : 0f;
            group.interactable = show;
            group.blocksRaycasts = show;
            if (show && resumeButton != null) resumeButton.Select();
        }

        void ToggleSound()
        {
            AudioListener.volume = AudioListener.volume > 0f ? 0f : 1f;
            RefreshSound();
        }

        void RefreshSound()
        {
            if (soundLabel != null) soundLabel.text = AudioListener.volume > 0f ? "Som: ligado" : "Som: desligado";
        }
    }
}
