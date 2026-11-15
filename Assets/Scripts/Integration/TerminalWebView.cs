using System.Collections;
using System.Runtime.InteropServices;
using LunarEyes.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LunarEyes.Integration
{
    /// <summary>
    /// Quando o jogador usa o terminal, mostra a aplicação web Lunar Eyes.
    /// - Build WebGL: abre a aplicação numa janela dentro do jogo (iframe em Plugins/WebGL/TerminalFrame.jslib).
    ///   Nunca abre o navegador nem outro separador.
    /// - Editor / desktop: não há navegador embutido; mostra um painel a indicar que a janela aparece na versão web.
    /// Sem URL configurado, mostra um estado "por configurar".
    /// </summary>
    public class TerminalWebView : MonoBehaviour
    {
        [Tooltip("Endereço da aplicação web Lunar Eyes (https://…). Vazio = ainda por configurar.")]
        [SerializeField] string appUrl = "https://lunar-eyes-view.vercel.app/";
        [SerializeField] float openDelay = 0.7f;

        [Header("UI")]
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text statusText;
        [SerializeField] TMP_Text urlText;
        [SerializeField] Button exitButton;

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void LunarEyes_OpenFrame(string url, string gameObjectName);
        [DllImport("__Internal")] static extern void LunarEyes_CloseFrame();
#endif

        MonitorTerminal current;
        Coroutine routine;

        public string AppUrl { get => appUrl; set => appUrl = value; }
        bool HasUrl => !string.IsNullOrWhiteSpace(appUrl);

        void Awake()
        {
            SetVisible(false, instant: true);
            if (exitButton != null) exitButton.onClick.AddListener(ExitTerminal);
        }

        void OnEnable()
        {
            MonitorTerminal.Opened += OnTerminalOpened;
            MonitorTerminal.Closed += OnTerminalClosed;
        }

        void OnDisable()
        {
            MonitorTerminal.Opened -= OnTerminalOpened;
            MonitorTerminal.Closed -= OnTerminalClosed;
        }

        void OnTerminalOpened(MonitorTerminal terminal)
        {
            current = terminal;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(OpenAfterDelay());
        }

        IEnumerator OpenAfterDelay()
        {
            yield return new WaitForSeconds(openDelay); // deixa a câmara chegar ao ecrã
            Refresh();
#if UNITY_WEBGL && !UNITY_EDITOR
            // A janela HTML desenha tudo; o painel Unity só aparece se faltar o URL.
            if (HasUrl) LunarEyes_OpenFrame(appUrl, gameObject.name);
            else SetVisible(true);
#else
            SetVisible(true);
#endif
            routine = null;
        }

        void OnTerminalClosed(MonitorTerminal terminal)
        {
            if (routine != null) { StopCoroutine(routine); routine = null; }
#if UNITY_WEBGL && !UNITY_EDITOR
            LunarEyes_CloseFrame();
#endif
            SetVisible(false);
            current = null;
        }

        /// <summary>Chamado pelo jslib quando o jogador fecha o iframe (botão "Sair do terminal" na página).</summary>
        public void OnFrameClosed() => ExitTerminal();

        void ExitTerminal()
        {
            if (current != null) current.Exit();
        }

        void Refresh()
        {
            if (statusText != null)
                statusText.text = HasUrl
                    ? "Na versão web, a aplicação Lunar Eyes abre aqui, numa janela dentro do jogo. O editor não tem navegador embutido."
                    : "A ligação à aplicação web ainda não está configurada.";
            if (urlText != null) urlText.text = HasUrl ? appUrl : "URL por configurar";
        }

        void SetVisible(bool visible, bool instant = false)
        {
            if (group == null) return;
            group.interactable = visible;
            group.blocksRaycasts = visible;
            if (instant) group.alpha = visible ? 1f : 0f;
            else StartCoroutine(Fade(visible ? 1f : 0f));
        }

        IEnumerator Fade(float to)
        {
            while (!Mathf.Approximately(group.alpha, to))
            {
                group.alpha = Mathf.MoveTowards(group.alpha, to, Time.unscaledDeltaTime * 5f);
                yield return null;
            }
        }
    }
}
