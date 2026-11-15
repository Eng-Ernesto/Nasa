using System;
using System.Collections;
using LunarEyes.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LunarEyes.Interaction
{
    /// <summary>
    /// Monitor da estação: ponto de entrada da missão.
    /// Ao interagir, o jogador fica no terminal, a câmara foca o ecrã e o painel da missão abre.
    /// </summary>
    public class MonitorTerminal : Interactable
    {
        /// <summary>Disparado quando o terminal abre/fecha. O sistema de missão (U02) liga-se aqui.</summary>
        public static event Action<MonitorTerminal> Opened;
        public static event Action<MonitorTerminal> Closed;

        [SerializeField] Transform cameraPoint;
        [SerializeField] GameObject screenIdle;
        [SerializeField] GameObject screenActive;

        [Header("Som")]
        [SerializeField] AudioSource sfx;
        [SerializeField] AudioSource computerLoop;
        [SerializeField] AudioClip onClip;
        [SerializeField] AudioClip offClip;
        [SerializeField] AudioClip alertClip;
        [SerializeField] float alertDelay = 0.7f;

        PlayerInteractor user;
        Coroutine alertRoutine;

        public bool InUse => user != null;
        /// <summary>Verdadeiro enquanto algum terminal está aberto.</summary>
        public static bool AnyInUse { get; private set; }

        public override string Prompt => "Usar terminal";
        public override string Subtitle => "Abre a aplicação Lunar Eyes";
        public override bool CanInteract(PlayerInteractor player) => !InUse;

        void Start() => SetScreen(false);

        public override void Interact(PlayerInteractor player)
        {
            user = player;
            if (standPoint != null) player.Controller.SnapTo(standPoint);
            player.Controller.Lock("Interact");
            if (cameraPoint != null) player.Camera?.SetOverride(cameraPoint);
            SetScreen(true);
            AnyInUse = true;

            PlayOneShot(onClip, 0.8f);
            if (computerLoop != null) computerLoop.Play();
            alertRoutine = StartCoroutine(AlertAfterDelay());

            Opened?.Invoke(this);
        }

        IEnumerator AlertAfterDelay()
        {
            yield return new WaitForSeconds(alertDelay);
            PlayOneShot(alertClip, 0.7f);
            alertRoutine = null;
        }

        void Update()
        {
            if (user == null) return;
            var kb = Keyboard.current;
            if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.qKey.wasPressedThisFrame)) Exit();
        }

        public void Exit()
        {
            if (user == null) return;
            if (alertRoutine != null) { StopCoroutine(alertRoutine); alertRoutine = null; }
            if (computerLoop != null) computerLoop.Stop();
            PlayOneShot(offClip, 0.7f);

            user.Camera?.SetOverride(null);
            user.Controller.Unlock();
            user = null;
            SetScreen(false);
            AnyInUse = false;
            Closed?.Invoke(this);
        }

        void PlayOneShot(AudioClip clip, float volume)
        {
            if (sfx != null && clip != null) sfx.PlayOneShot(clip, volume);
        }

        void SetScreen(bool active)
        {
            if (screenIdle != null) screenIdle.SetActive(!active);
            if (screenActive != null) screenActive.SetActive(active);
        }
    }
}
