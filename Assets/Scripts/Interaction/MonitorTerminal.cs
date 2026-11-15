using System;
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

        PlayerInteractor user;

        public bool InUse => user != null;

        public override string Prompt => "Usar terminal";
        public override bool CanInteract(PlayerInteractor player) => !InUse;

        void Start() => SetScreen(false);

        public override void Interact(PlayerInteractor player)
        {
            user = player;
            if (standPoint != null) player.Controller.SnapTo(standPoint);
            player.Controller.Lock("Interact");
            if (cameraPoint != null) player.Camera?.SetOverride(cameraPoint);
            SetScreen(true);
            HUD.Instance?.ShowTerminal(true);
            Opened?.Invoke(this);
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
            user.Camera?.SetOverride(null);
            user.Controller.Unlock();
            user = null;
            SetScreen(false);
            HUD.Instance?.ShowTerminal(false);
            Closed?.Invoke(this);
        }

        void SetScreen(bool active)
        {
            if (screenIdle != null) screenIdle.SetActive(!active);
            if (screenActive != null) screenActive.SetActive(active);
        }
    }
}
