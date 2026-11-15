using UnityEngine;
using UnityEngine.InputSystem;

namespace LunarEyes.Player
{
    /// <summary>
    /// Câmara orbital em terceira pessoa com colisão simples.
    /// Pode ser "sobreposta" por um ponto fixo (ex.: vista do terminal).
    /// </summary>
    public class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] float distance = 4f;
        [SerializeField] float height = 1.6f;
        [SerializeField] float sensitivity = 0.12f;
        [SerializeField] float minPitch = -20f;
        [SerializeField] float maxPitch = 60f;
        [SerializeField] float collisionRadius = 0.25f;
        [SerializeField] LayerMask collisionMask = ~0;
        [SerializeField] float overrideBlendSpeed = 4f;

        float yaw;
        float pitch = 15f;
        Transform overridePoint;

        public Transform Target { get => target; set => target = value; }

        void Start()
        {
            if (target != null) yaw = target.eulerAngles.y;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void SetOverride(Transform point)
        {
            overridePoint = point;
            bool free = point == null;
            Cursor.lockState = free ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !free;
        }

        void LateUpdate()
        {
            if (overridePoint != null)
            {
                float t = 1f - Mathf.Exp(-overrideBlendSpeed * Time.deltaTime);
                transform.position = Vector3.Lerp(transform.position, overridePoint.position, t);
                transform.rotation = Quaternion.Slerp(transform.rotation, overridePoint.rotation, t);
                return;
            }

            if (target == null) return;

            var mouse = Mouse.current;
            if (mouse != null && Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 delta = mouse.delta.ReadValue();
                yaw += delta.x * sensitivity;
                pitch = Mathf.Clamp(pitch - delta.y * sensitivity, minPitch, maxPitch);
            }

            Vector3 pivot = target.position + Vector3.up * height;
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 dir = rot * Vector3.back;

            float d = distance;
            if (Physics.SphereCast(pivot, collisionRadius, dir, out RaycastHit hit, distance, collisionMask, QueryTriggerInteraction.Ignore)
                && !hit.transform.IsChildOf(target))
            {
                d = Mathf.Max(0.5f, hit.distance);
            }

            transform.SetPositionAndRotation(pivot + dir * d, rot);
        }
    }
}
