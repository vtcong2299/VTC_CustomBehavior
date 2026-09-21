using UnityEngine;

namespace Vtcong.Core
{
    public abstract class CustomBehaviour : MonoBehaviour
    {
        internal int UpdateIndex { get; set; } = -1;
        internal int FixedUpdateIndex { get; set; } = -1;
        internal int LateUpdateIndex { get; set; } = -1;

        private bool isRegistered;

        // Sealed on purpose: a subclass that declares its own OnEnable/OnDisable/Start
        // would silently shadow these and never reach BehaviourManager.
        // Override the OnBehaviour* hooks below instead.
        private void OnEnable()
        {
            Register();
            OnBehaviourEnable();
        }

        private void Start()
        {
            // OnEnable can run before BehaviourManager.Awake; Start is the safety net.
            Register();
            OnBehaviourStart();
        }

        private void OnDisable()
        {
            Unregister();
            OnBehaviourDisable();
        }

        protected virtual void OnBehaviourEnable()
        {
        }

        protected virtual void OnBehaviourStart()
        {
        }

        protected virtual void OnBehaviourDisable()
        {
        }

        private void Register()
        {
            if (isRegistered) return;

            BehaviourManager manager = BehaviourManager.Instance;
            if (manager == null) return;

            manager.Register(this);
            isRegistered = true;
        }

        private void Unregister()
        {
            if (!isRegistered) return;

            BehaviourManager manager = BehaviourManager.Instance;
            if (manager != null)
                manager.Unregister(this);

            isRegistered = false;
        }

        /// <summary>
        /// Called by <see cref="BehaviourManager"/> when it is torn down, so a behaviour
        /// that outlives the manager can register again with the next one.
        /// </summary>
        internal void ResetRegistration()
        {
            isRegistered = false;
        }
    }
}
