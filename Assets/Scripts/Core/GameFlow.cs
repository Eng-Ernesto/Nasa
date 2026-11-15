using LunarEyes.Interaction;
using LunarEyes.Player;
using LunarEyes.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace LunarEyes.Core
{
    /// <summary>
    /// Fluxo da cena: ecrã inicial → exploração → terminal (aplicação web) → exploração.
    /// Atualiza o objetivo e o marcador do HUD conforme o progresso; Esc abre a pausa.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        enum State { Title, Playing, Paused }

        [SerializeField] PlayerController player;
        [SerializeField] ThirdPersonCamera playerCamera;
        [SerializeField] Transform titleCameraPoint;
        [SerializeField] TitleScreen title;
        [SerializeField] PauseMenu pause;
        [SerializeField] HUD hud;
        [SerializeField] StationDoor door;
        [SerializeField] MonitorTerminal terminal;

        State state;
        bool doorOpened;
        bool terminalUsed;

        void Start()
        {
            if (hud != null) hud.SetPlayer(player.transform);
            if (title != null) title.StartRequested += StartMission;
            if (pause != null)
            {
                pause.ResumeRequested += Resume;
                pause.RestartRequested += Restart;
            }
            EnterTitle();
        }

        void OnEnable()
        {
            StationDoor.Opened += OnDoorOpened;
            MonitorTerminal.Opened += OnTerminalOpened;
            MonitorTerminal.Closed += OnTerminalClosed;
        }

        void OnDisable()
        {
            StationDoor.Opened -= OnDoorOpened;
            MonitorTerminal.Opened -= OnTerminalOpened;
            MonitorTerminal.Closed -= OnTerminalClosed;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;
            if (MonitorTerminal.AnyInUse) return; // o terminal trata o seu próprio Esc
            if (state == State.Playing) Pause();
            else if (state == State.Paused) Resume();
        }

        void EnterTitle()
        {
            state = State.Title;
            player.Lock();
            playerCamera.SetOverride(titleCameraPoint);
            hud.SetVisible(false);
            title.Show(true);
            pause.Show(false);
        }

        void StartMission()
        {
            state = State.Playing;
            title.Show(false);
            playerCamera.SetOverride(null);
            player.Unlock();
            hud.SetVisible(true);
            UpdateObjective();
        }

        /// <summary>Botão de pausa do HUD.</summary>
        public void TogglePause()
        {
            if (state == State.Playing && !MonitorTerminal.AnyInUse) Pause();
            else if (state == State.Paused) Resume();
        }

        void Pause()
        {
            state = State.Paused;
            Time.timeScale = 0f;
            player.Lock();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            pause.Show(true);
        }

        void Resume()
        {
            state = State.Playing;
            Time.timeScale = 1f;
            pause.Show(false);
            player.Unlock();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>
        /// Atalho de teste (build web): começa a missão e usa o terminal de imediato.
        /// Chamado pelo navegador: unityInstance.SendMessage("Game Flow", "DevUseTerminal").
        /// </summary>
        public void DevUseTerminal()
        {
            if (state == State.Title) StartMission();
            var interactor = player.GetComponent<PlayerInteractor>();
            if (interactor != null && !MonitorTerminal.AnyInUse) terminal.Interact(interactor);
        }

        void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void OnDoorOpened(StationDoor _) { doorOpened = true; UpdateObjective(); }

        void OnTerminalOpened(MonitorTerminal _)
        {
            terminalUsed = true;
            hud.SetVisible(false);
        }

        void OnTerminalClosed(MonitorTerminal _)
        {
            hud.SetVisible(state == State.Playing);
            UpdateObjective();
        }

        void UpdateObjective()
        {
            if (terminalUsed)
            {
                hud.SetObjective("Missão em curso no terminal", "A investigar", UITheme.SignalCyan, 1, true);
                hud.SetMarker(terminal.transform, "Terminal");
            }
            else if (doorOpened)
            {
                hud.SetObjective("Vai até ao terminal de observação", "Alerta pendente", UITheme.AlertAmber, 0, true);
                hud.SetMarker(terminal.transform, "Terminal");
            }
            else
            {
                hud.SetObjective("Entra na estação e liga o terminal", "Alerta pendente", UITheme.AlertAmber, 0, true);
                hud.SetMarker(door.transform, "Estação");
            }
        }
    }
}
