using UnityEngine;

namespace Vtcong.Core
{
    public abstract class CustomBehaviour : MonoBehaviour
    {
        internal int UpdateIndex { get; set; } = -1;
        internal int FixedUpdateIndex { get; set; } = -1;
        internal int LateUpdateIndex { get; set; } = -1;

        private BehaviourManager registeredManager;

        // Protected and non-virtual: a subclass that declares its own OnEnable/Start/OnDisable
        // hides these (compiler warning CS0108) and CustomBehaviourValidator logs an error in the
        // editor, because Unity would then call only the subclass and registration never happens.
        // Override the OnBehaviour* hooks below instead.
        protected void OnEnable()
        {
            Register();
            OnBehaviourEnable();
        }

        protected void Start()
        {
            // OnEnable can run before BehaviourManager.Awake; Start is the safety net.
            Register();
            OnBehaviourStart();
        }

        protected void OnDisable()
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
            if (registeredManager != null) return;

            BehaviourManager manager = BehaviourManager.GetOrCreate();
            if (manager == null) return;

            manager.Register(this);
            registeredManager = manager;
        }

        private void Unregister()
        {
            if (registeredManager != null)
                registeredManager.Unregister(this);

            registeredManager = null;
        }

        /// <summary>
        /// Called by <see cref="BehaviourManager"/> when it is torn down, so a behaviour
        /// that outlives the manager can register again with the next one.
        /// </summary>
        internal void ResetRegistration()
        {
            registeredManager = null;
        }
    }
}
