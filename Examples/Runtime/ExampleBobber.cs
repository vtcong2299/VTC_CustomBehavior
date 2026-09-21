using UnityEngine;

namespace Vtcong.Core.Examples
{
    /// <summary>
    /// Runs in the LateUpdate loop. Also shows the new lifecycle hooks: override
    /// OnBehaviourStart instead of Start, so registration can never be shadowed.
    /// </summary>
    public class ExampleBobber : CustomBehaviour, ILateUpdateBehaviour
    {
        [SerializeField] private float amplitude = 0.75f;
        [SerializeField] private float frequency = 1.2f;

        private Vector3 origin;
        private float time;

        protected override void OnBehaviourStart()
        {
            origin = transform.position;
        }

        public void OnBehaviourLateUpdate(float dt)
        {
            time += dt;
            transform.position = origin + Vector3.up * (Mathf.Sin(time * frequency * Mathf.PI * 2f) * amplitude);
        }
    }
}
