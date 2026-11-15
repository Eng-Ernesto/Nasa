using LunarEyes.Player;
using LunarEyes.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LunarEyes.Interaction
{
    /// <summary>Encontra o Interactable mais próximo, mostra o prompt e dispara com E.</summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] ThirdPersonCamera playerCamera;
        [Header("Som")]
        [SerializeField] AudioSource uiAudio;
        [SerializeField] AudioClip promptClip;
        [SerializeField] AudioClip confirmClip;

        public PlayerController Controller { get; private set; }
        public ThirdPersonCamera Camera => playerCamera;

        Interactable current;
        Interactable lastPrompted;

        void Awake()
        {
            Controller = GetComponent<PlayerController>();
            if (playerCamera == null) playerCamera = FindAnyObjectByType<ThirdPersonCamera>();
        }

        void Update()
        {
            current = Controller.IsLocked ? null : FindBest();
            if (current != null) HUD.Instance?.SetPrompt(current.Prompt, current.Subtitle);
            else HUD.Instance?.SetPrompt(null);

            // Tique discreto só quando um novo prompt aparece
            if (current != null && current != lastPrompted) Play(promptClip, 0.35f);
            lastPrompted = current;

            if (current != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                var target = current;
                HUD.Instance?.SetPrompt(null);
                Play(confirmClip, 0.5f);
                target.Interact(this);
            }
        }

        void Play(AudioClip clip, float volume)
        {
            if (uiAudio != null && clip != null) uiAudio.PlayOneShot(clip, volume);
        }

        Interactable FindBest()
        {
            Interactable best = null;
            float bestDist = float.MaxValue;
            Vector3 pos = transform.position;
            foreach (var it in Interactable.All)
            {
                Vector3 a = it.Anchor;
                float d = Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(a.x, a.z));
                if (d <= it.Radius && d < bestDist && it.CanInteract(this))
                {
                    best = it;
                    bestDist = d;
                }
            }
            return best;
        }
    }
}
