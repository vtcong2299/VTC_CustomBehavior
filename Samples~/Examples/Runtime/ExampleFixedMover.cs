using UnityEngine;

namespace Vtcong.Core.Examples
{
    /// <summary>
    /// Runs in the FixedUpdate loop and ping-pongs along X, so the fixed-step list
    /// is exercised alongside the other two.
    /// </summary>
    public class ExampleFixedMover : CustomBehaviour, IFixedUpdateBehaviour
    {
        [SerializeField] private float distance = 2.5f;
        [SerializeField] private float speed = 1.5f;

        private Vector3 origin;
        private float time;

        protected override void OnBehaviourStart()
        {
            origin = transform.position;
        }

        public void OnBehaviourFixedUpdate(float dt)
        {
            time += dt;
            transform.position = origin + Vector3.right * (Mathf.Sin(time * speed) * distance);
        }
    }
}
