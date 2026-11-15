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

        public PlayerController Controller { get; private set; }
        public ThirdPersonCamera Camera => playerCamera;

        Interactable current;

        void Awake()
        {
            Controller = GetComponent<PlayerController>();
            if (playerCamera == null) playerCamera = FindAnyObjectByType<ThirdPersonCamera>();
        }

        void Update()
        {
            current = Controller.IsLocked ? null : FindBest();
            HUD.Instance?.SetPrompt(current != null ? $"[E]  {current.Prompt}" : null);

            if (current != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                var target = current;
                HUD.Instance?.SetPrompt(null);
                target.Interact(this);
            }
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
