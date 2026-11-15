using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LunarEyes.UI
{
    /// <summary>
    /// HUD de exploração (design "02 · Exploração"): objetivo atual, marcador no mundo,
    /// prompt de interação e controlos. Fica escondido no ecrã inicial e no terminal.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        public static HUD Instance { get; private set; }

        [Header("Raiz")]
        [SerializeField] CanvasGroup gameplayGroup;

        [Header("Objetivo")]
        [SerializeField] TMP_Text objectiveText;
        [SerializeField] TMP_Text statusText;
        [SerializeField] Image statusDot;
        [SerializeField] Image[] progressSegments;

        [Header("Prompt")]
        [SerializeField] GameObject promptRoot;
        [SerializeField] TMP_Text promptTitle;
        [SerializeField] TMP_Text promptSubtitle;

        [Header("Marcador no mundo")]
        [SerializeField] RectTransform marker;
        [SerializeField] TMP_Text markerLabel;
        [SerializeField] float markerHeight = 4.2f;
        [Tooltip("Abaixo desta distância o marcador esconde-se (o prompt de interação chega).")]
        [SerializeField] float hideWithin = 6f;

        [Header("Controlos")]
        [SerializeField] CanvasGroup controlsGroup;

        Transform markerTarget;
        string markerName;
        Transform player;
        RectTransform canvasRect;
        float targetAlpha = 1f;

        void Awake()
        {
            Instance = this;
            canvasRect = (RectTransform)GetComponentInParent<Canvas>().transform;
            SetPrompt(null);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetPlayer(Transform p) => player = p;

        public void SetVisible(bool visible)
        {
            targetAlpha = visible ? 1f : 0f;
            if (gameplayGroup != null) gameplayGroup.blocksRaycasts = visible;
        }

        public void SetPrompt(string title, string subtitle = null)
        {
            bool show = !string.IsNullOrEmpty(title);
            if (promptRoot != null && promptRoot.activeSelf != show) promptRoot.SetActive(show);
            if (!show) return;
            if (promptTitle != null) promptTitle.text = title;
            if (promptSubtitle != null)
            {
                promptSubtitle.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
                promptSubtitle.text = subtitle ?? "";
            }
        }

        /// <summary>Atualiza o painel de objetivo. <paramref name="step"/>: segmentos completos (0–6); <paramref name="partial"/>: o seguinte meio preenchido.</summary>
        public void SetObjective(string text, string status, Color statusColor, int step, bool partial)
        {
            if (objectiveText != null) objectiveText.text = text;
            if (statusText != null) { statusText.text = status; statusText.color = Color.Lerp(statusColor, Color.white, 0.35f); }
            if (statusDot != null) statusDot.color = statusColor;
            if (progressSegments == null) return;
            for (int i = 0; i < progressSegments.Length; i++)
            {
                var c = i < step ? UITheme.SignalCyan
                      : i == step && partial ? UITheme.SignalCyan.WithAlpha(0.55f)
                      : UITheme.BorderSlate;
                progressSegments[i].color = c;
            }
        }

        public void SetMarker(Transform target, string label)
        {
            markerTarget = target;
            markerName = label;
            if (marker != null) marker.gameObject.SetActive(target != null);
        }

        public void HideControlsHint()
        {
            if (controlsGroup != null) controlsGroup.alpha = 0f;
        }

        void LateUpdate()
        {
            if (gameplayGroup != null)
                gameplayGroup.alpha = Mathf.MoveTowards(gameplayGroup.alpha, targetAlpha, Time.unscaledDeltaTime * 4f);

            UpdateMarker();
        }

        void UpdateMarker()
        {
            if (marker == null || markerTarget == null) return;
            var cam = Camera.main;
            if (cam == null) return;

            float dist = player != null ? Vector3.Distance(player.position, markerTarget.position) : float.MaxValue;
            Vector3 world = markerTarget.position + Vector3.up * markerHeight;
            Vector3 sp = cam.WorldToScreenPoint(world);
            bool visible = sp.z > 0f && dist > hideWithin;
            marker.gameObject.SetActive(visible);
            if (!visible) return;

            float scale = Screen.height / 810f;
            sp.x = Mathf.Clamp(sp.x, 100f * scale, Screen.width - 100f * scale);
            sp.y = Mathf.Clamp(sp.y, 60f * scale, Screen.height - 140f * scale);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, sp, null, out Vector2 local))
                marker.anchoredPosition = local;

            if (markerLabel != null)
            {
                markerLabel.text = player != null ? $"{markerName} · {Mathf.RoundToInt(dist)} m" : markerName;
            }
        }
    }
}
