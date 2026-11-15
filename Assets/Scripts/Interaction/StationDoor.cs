using System.Collections;
using UnityEngine;

namespace LunarEyes.Interaction
{
    /// <summary>Porta da estação: o astronauta toca a animação de abrir e o painel desliza para dentro da parede.</summary>
    public class StationDoor : Interactable
    {
        [SerializeField] Transform panel;
        [Tooltip("Deslocamento local do painel quando aberto.")]
        [SerializeField] Vector3 openOffset = new Vector3(3.4f, 0f, 0f);
        [SerializeField] float openDuration = 1.6f;
        [SerializeField] float animationDelay = 0.8f;
        [SerializeField] float playerLockSeconds = 2.2f;
        [SerializeField] AudioSource sfx;

        bool isOpen;
        bool isMoving;
        Vector3 closedLocalPos;

        public bool IsOpen => isOpen;

        void Awake()
        {
            if (panel != null) closedLocalPos = panel.localPosition;
        }

        public override string Prompt => "Abrir porta da estação";
        public override bool CanInteract(PlayerInteractor player) => !isOpen && !isMoving;

        public override void Interact(PlayerInteractor player)
        {
            if (standPoint != null)
            {
                Vector3 look = transform.position - player.transform.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.01f) player.transform.rotation = Quaternion.LookRotation(look);
            }
            player.Controller.PlayAction("OpenDoor", playerLockSeconds);
            StartCoroutine(OpenRoutine());
        }

        IEnumerator OpenRoutine()
        {
            isMoving = true;
            yield return new WaitForSeconds(animationDelay);
            if (sfx != null) sfx.Play();

            Vector3 from = closedLocalPos;
            Vector3 to = closedLocalPos + openOffset;
            for (float t = 0f; t < 1f; t += Time.deltaTime / openDuration)
            {
                if (panel != null) panel.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
                yield return null;
            }
            if (panel != null) panel.localPosition = to;
            isOpen = true;
            isMoving = false;
        }
    }
}
