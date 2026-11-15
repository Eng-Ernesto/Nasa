using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LunarEyes.Player
{
    /// <summary>
    /// Movimento em terceira pessoa do astronauta (WASD + Shift para correr).
    /// O movimento é relativo à câmara. Ações (abrir porta, usar terminal) bloqueiam o movimento.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movimento")]
        [SerializeField] float walkSpeed = 2.2f;
        [SerializeField] float runSpeed = 4.5f;
        [SerializeField] float turnSpeed = 10f;
        [Tooltip("Gravidade lunar (m/s²).")]
        [SerializeField] float gravity = 1.62f;

        [Header("Referências")]
        [SerializeField] Animator animator;
        [SerializeField] Transform cameraTransform;

        static readonly int SpeedHash = Animator.StringToHash("Speed");

        CharacterController controller;
        float verticalVelocity;
        Coroutine actionRoutine;

        /// <summary>Verdadeiro enquanto o jogador está numa ação ou no terminal.</summary>
        public bool IsLocked { get; private set; }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        }

        void Update()
        {
            Vector2 input = IsLocked ? Vector2.zero : ReadMoveInput();
            bool running = !IsLocked && Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;

            Vector3 move = Vector3.zero;
            if (input.sqrMagnitude > 0.01f)
            {
                Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
                Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
                forward.y = 0f; right.y = 0f;
                forward.Normalize(); right.Normalize();

                move = (forward * input.y + right * input.x).normalized;
                Quaternion target = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * Time.deltaTime);
            }

            float speed = move.sqrMagnitude > 0f ? (running ? runSpeed : walkSpeed) : 0f;

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity -= gravity * Time.deltaTime;

            Vector3 velocity = move * speed + Vector3.up * verticalVelocity;
            controller.Move(velocity * Time.deltaTime);

            if (animator != null) animator.SetFloat(SpeedHash, speed, 0.1f, Time.deltaTime);
        }

        static Vector2 ReadMoveInput()
        {
            var kb = Keyboard.current;
            if (kb == null) return Vector2.zero;
            float x = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
            float y = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
            return Vector2.ClampMagnitude(new Vector2(x, y), 1f);
        }

        /// <summary>Toca uma animação de ação e bloqueia o movimento durante <paramref name="lockSeconds"/>.</summary>
        public void PlayAction(string stateName, float lockSeconds)
        {
            if (actionRoutine != null) StopCoroutine(actionRoutine);
            actionRoutine = StartCoroutine(ActionRoutine(stateName, lockSeconds));
        }

        IEnumerator ActionRoutine(string stateName, float lockSeconds)
        {
            IsLocked = true;
            if (animator != null) animator.CrossFadeInFixedTime(stateName, 0.2f);
            yield return new WaitForSeconds(lockSeconds);
            if (animator != null) animator.CrossFadeInFixedTime("Locomotion", 0.3f);
            IsLocked = false;
            actionRoutine = null;
        }

        /// <summary>Bloqueia o jogador indefinidamente (ex.: terminal aberto) e toca um estado em loop.</summary>
        public void Lock(string holdState = null)
        {
            if (actionRoutine != null) { StopCoroutine(actionRoutine); actionRoutine = null; }
            IsLocked = true;
            if (animator != null && !string.IsNullOrEmpty(holdState)) animator.CrossFadeInFixedTime(holdState, 0.2f);
        }

        public void Unlock()
        {
            IsLocked = false;
            if (animator != null) animator.CrossFadeInFixedTime("Locomotion", 0.3f);
        }

        /// <summary>Teleporta o jogador para um ponto (desativa o CharacterController durante a mudança).</summary>
        public void SnapTo(Transform point)
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(point.position, Quaternion.Euler(0f, point.eulerAngles.y, 0f));
            controller.enabled = true;
        }
    }
}
