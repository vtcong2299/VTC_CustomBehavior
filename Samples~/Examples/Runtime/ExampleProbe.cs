using System;
using UnityEngine;

namespace Vtcong.Core.Examples
{
    /// <summary>
    /// Instrumented behaviour used by <see cref="BehaviourRegressionTests"/>.
    /// Counts every loop it is driven from and can run an arbitrary action
    /// from inside the update loop, which is how the tests mutate the registry
    /// mid-iteration.
    /// </summary>
    public class ExampleProbe : CustomBehaviour, IUpdateBehaviour, IFixedUpdateBehaviour, ILateUpdateBehaviour
    {
        public int UpdateCount { get; private set; }
        public int FixedUpdateCount { get; private set; }
        public int LateUpdateCount { get; private set; }

        public bool HasStarted { get; private set; }

        /// <summary>True if the manager drove this probe before Unity called its Start.</summary>
        public bool UpdatedBeforeStart { get; private set; }

        /// <summary>When set, the probe throws after counting, to test exception isolation.</summary>
        public bool ThrowOnUpdate { get; set; }

        /// <summary>Runs inside OnBehaviourUpdate, i.e. while the manager is iterating.</summary>
        public Action<ExampleProbe> DuringUpdate { get; set; }

        public static ExampleProbe Create(string probeName, Transform parent) =>
            Create<ExampleProbe>(probeName, parent);

        public static T Create<T>(string probeName, Transform parent) where T : ExampleProbe
        {
            GameObject go = new(probeName);
            go.transform.SetParent(parent, false);
            return go.AddComponent<T>();
        }

        protected override void OnBehaviourStart()
        {
            HasStarted = true;
        }

        public void OnBehaviourUpdate(float dt)
        {
            if (!HasStarted) UpdatedBeforeStart = true;

            UpdateCount++;
            DuringUpdate?.Invoke(this);

            if (ThrowOnUpdate)
                throw new InvalidOperationException($"{name}: intentional test exception (expected).");
        }

        public void OnBehaviourFixedUpdate(float dt)
        {
            FixedUpdateCount++;
        }

        public void OnBehaviourLateUpdate(float dt)
        {
            LateUpdateCount++;
        }
    }
}
