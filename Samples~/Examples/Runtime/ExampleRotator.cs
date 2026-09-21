using UnityEngine;

namespace Vtcong.Core.Examples
{
    /// <summary>
    /// Smallest possible <see cref="IUpdateBehaviour"/>: no Unity Update() message,
    /// BehaviourManager drives it from a single cached loop.
    /// </summary>
    public class ExampleRotator : CustomBehaviour, IUpdateBehaviour
    {
        [SerializeField] private Vector3 degreesPerSecond = new(0f, 90f, 0f);

        public void OnBehaviourUpdate(float dt)
        {
            transform.Rotate(degreesPerSecond * dt, Space.Self);
        }
    }
}
