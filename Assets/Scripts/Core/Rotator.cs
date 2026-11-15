using UnityEngine;

namespace LunarEyes.Core
{
    /// <summary>Rotação lenta e contínua (ex.: a Terra vista da Lua).</summary>
    public class Rotator : MonoBehaviour
    {
        [SerializeField] Vector3 degreesPerSecond = new Vector3(0f, 1.5f, 0f);

        void Update() => transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self);
    }
}
