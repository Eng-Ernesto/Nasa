using LunarEyes.Interaction;
using UnityEngine;

namespace LunarEyes.Core
{
    /// <summary>
    /// Zumbido baixo dos equipamentos dentro da estação. Fica mais baixo enquanto o terminal está aberto,
    /// para a investigação ficar em silêncio (regra de som do design system).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class StationAmbience : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float volume = 0.22f;
        [SerializeField, Range(0f, 1f)] float duckedVolume = 0.05f;
        [SerializeField] float fadeSpeed = 1.5f;

        AudioSource source;
        float target;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.loop = true;
            target = volume;
            source.volume = volume;
            if (!source.isPlaying) source.Play();
        }

        void OnEnable()
        {
            MonitorTerminal.Opened += OnOpened;
            MonitorTerminal.Closed += OnClosed;
        }

        void OnDisable()
        {
            MonitorTerminal.Opened -= OnOpened;
            MonitorTerminal.Closed -= OnClosed;
        }

        void OnOpened(MonitorTerminal _) => target = duckedVolume;
        void OnClosed(MonitorTerminal _) => target = volume;

        void Update() => source.volume = Mathf.MoveTowards(source.volume, target, fadeSpeed * Time.deltaTime);
    }
}
