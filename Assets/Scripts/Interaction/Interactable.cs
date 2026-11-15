using System.Collections.Generic;
using UnityEngine;

namespace LunarEyes.Interaction
{
    /// <summary>Base para objetos com que o jogador pode interagir (tecla E).</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        [SerializeField] string prompt = "Interagir";
        [SerializeField] float radius = 2.2f;
        [Tooltip("Ponto de referência para distância e posicionamento do jogador.")]
        [SerializeField] protected Transform standPoint;

        public virtual string Prompt => prompt;
        /// <summary>Linha secundária do prompt (ex.: onde fica). Vazio = sem subtítulo.</summary>
        public virtual string Subtitle => null;
        public float Radius => radius;
        public Vector3 Anchor => standPoint != null ? standPoint.position : transform.position;

        public virtual bool CanInteract(PlayerInteractor player) => true;
        public abstract void Interact(PlayerInteractor player);

        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(Anchor, radius);
        }
    }
}
