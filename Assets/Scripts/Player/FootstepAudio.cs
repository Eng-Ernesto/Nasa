using UnityEngine;

namespace LunarEyes.Player
{
    /// <summary>
    /// Passos por distância percorrida: regolito no terreno lunar, metal dentro da estação.
    /// A passada estica entre andar e correr para acompanhar a animação.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FootstepAudio : MonoBehaviour
    {
        [SerializeField] AudioSource source;
        [SerializeField] AudioClip[] regolith;
        [SerializeField] AudioClip[] metal;
        [SerializeField] float walkSpeed = 1.4f;
        [SerializeField] float runSpeed = 4.0f;
        [SerializeField] float walkStride = 0.6f;
        [SerializeField] float runStride = 1.25f;
        [SerializeField, Range(0f, 1f)] float volume = 0.45f;

        CharacterController controller;
        Vector3 lastPos;
        float travelled;
        int lastIndex = -1;

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            lastPos = transform.position;
        }

        void Update()
        {
            Vector3 pos = transform.position;
            Vector3 delta = pos - lastPos; delta.y = 0f;
            lastPos = pos;

            float speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            if (!controller.isGrounded || speed < 0.3f || delta.magnitude > 2f) { travelled = 0f; return; }

            travelled += delta.magnitude;
            float t = Mathf.InverseLerp(walkSpeed, runSpeed, speed);
            if (travelled >= Mathf.Lerp(walkStride, runStride, t))
            {
                travelled = 0f;
                Play(t);
            }
        }

        void Play(float runAmount)
        {
            var set = OnMetal() ? metal : regolith;
            if (source == null || set == null || set.Length == 0) return;
            int i = Random.Range(0, set.Length);
            if (set.Length > 1 && i == lastIndex) i = (i + 1) % set.Length;
            lastIndex = i;
            source.pitch = Random.Range(0.92f, 1.08f);
            source.PlayOneShot(set[i], volume * Mathf.Lerp(0.85f, 1.15f, runAmount));
        }

        bool OnMetal()
        {
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 1.5f, ~0, QueryTriggerInteraction.Ignore))
                return !(hit.collider is TerrainCollider);
            return false;
        }
    }
}
