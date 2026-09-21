using System;
using System.Collections.Generic;
using UnityEngine;

namespace Vtcong.Core
{
    public class BehaviourManager : Singleton<BehaviourManager>
    {
        private const int InitialCapacity = 64;

        private readonly BehaviourList<IUpdateBehaviour> updateBehaviours =
            new("Update", b => b.UpdateIndex, (b, i) => b.UpdateIndex = i);

        private readonly BehaviourList<IFixedUpdateBehaviour> fixedUpdateBehaviours =
            new("FixedUpdate", b => b.FixedUpdateIndex, (b, i) => b.FixedUpdateIndex = i);

        private readonly BehaviourList<ILateUpdateBehaviour> lateUpdateBehaviours =
            new("LateUpdate", b => b.LateUpdateIndex, (b, i) => b.LateUpdateIndex = i);

        public override void Init()
        {
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            int count = updateBehaviours.BeginIteration();

            for (int i = 0; i < count; i++)
            {
                IUpdateBehaviour behaviour = updateBehaviours[i];
                if (behaviour == null) continue;

                try
                {
                    behaviour.OnBehaviourUpdate(dt);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, behaviour as UnityEngine.Object);
                }
            }

            updateBehaviours.EndIteration();
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            int count = fixedUpdateBehaviours.BeginIteration();

            for (int i = 0; i < count; i++)
            {
                IFixedUpdateBehaviour behaviour = fixedUpdateBehaviours[i];
                if (behaviour == null) continue;

                try
                {
                    behaviour.OnBehaviourFixedUpdate(dt);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, behaviour as UnityEngine.Object);
                }
            }

            fixedUpdateBehaviours.EndIteration();
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            int count = lateUpdateBehaviours.BeginIteration();

            for (int i = 0; i < count; i++)
            {
                ILateUpdateBehaviour behaviour = lateUpdateBehaviours[i];
                if (behaviour == null) continue;

                try
                {
                    behaviour.OnBehaviourLateUpdate(dt);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, behaviour as UnityEngine.Object);
                }
            }

            lateUpdateBehaviours.EndIteration();
        }

        public void Register(CustomBehaviour behaviour)
        {
            if (behaviour == null) return;

            if (behaviour is IUpdateBehaviour updateBehaviour)
                updateBehaviours.Add(behaviour, updateBehaviour);

            if (behaviour is IFixedUpdateBehaviour fixedUpdateBehaviour)
                fixedUpdateBehaviours.Add(behaviour, fixedUpdateBehaviour);

            if (behaviour is ILateUpdateBehaviour lateUpdateBehaviour)
                lateUpdateBehaviours.Add(behaviour, lateUpdateBehaviour);
        }

        public void Unregister(CustomBehaviour behaviour)
        {
            if (behaviour == null) return;

            if (behaviour is IUpdateBehaviour updateBehaviour)
                updateBehaviours.Remove(behaviour, updateBehaviour);

            if (behaviour is IFixedUpdateBehaviour fixedUpdateBehaviour)
                fixedUpdateBehaviours.Remove(behaviour, fixedUpdateBehaviour);

            if (behaviour is ILateUpdateBehaviour lateUpdateBehaviour)
                lateUpdateBehaviours.Remove(behaviour, lateUpdateBehaviour);
        }

        private void OnDestroy()
        {
            // Leaving behind stale indices would make every surviving behaviour look
            // "already registered" to the next manager, so reset them on the way out.
            updateBehaviours.Clear();
            fixedUpdateBehaviours.Clear();
            lateUpdateBehaviours.Clear();
        }

        /// <summary>
        /// A swap-remove list that stays safe to mutate while it is being iterated:
        /// removals leave a null tombstone (so every cached index stays valid) and
        /// additions land past the cached count (so they only run from the next frame).
        /// </summary>
        private sealed class BehaviourList<T> where T : class
        {
            private readonly List<T> items = new(InitialCapacity);
            private readonly List<CustomBehaviour> owners = new(InitialCapacity);
            private readonly Func<CustomBehaviour, int> getIndex;
            private readonly Action<CustomBehaviour, int> setIndex;
            private readonly string label;

            private bool isIterating;
            private bool hasTombstones;

            public BehaviourList(string label, Func<CustomBehaviour, int> getIndex,
                Action<CustomBehaviour, int> setIndex)
            {
                this.label = label;
                this.getIndex = getIndex;
                this.setIndex = setIndex;
            }

            public T this[int index] => items[index];

            public int BeginIteration()
            {
                isIterating = true;
                return items.Count;
            }

            public void EndIteration()
            {
                isIterating = false;
                if (hasTombstones) Compact();
            }

            public void Add(CustomBehaviour owner, T item)
            {
                if (getIndex(owner) >= 0)
                {
                    Debug.LogWarning(
                        $"[BehaviourManager] '{owner.name}' is already registered for {label}. Ignoring duplicate.",
                        owner);
                    return;
                }

                setIndex(owner, items.Count);
                items.Add(item);
                owners.Add(owner);
            }

            public void Remove(CustomBehaviour owner, T item)
            {
                int index = getIndex(owner);
                if (index < 0) return;

                setIndex(owner, -1);

                if (index >= items.Count || !ReferenceEquals(items[index], item))
                {
                    Debug.LogError(
                        $"[BehaviourManager] {label} index {index} of '{owner.name}' does not match its slot. " +
                        "The registry was mutated from outside; dropping the entry.", owner);
                    return;
                }

                if (isIterating)
                {
                    items[index] = null;
                    owners[index] = null;
                    hasTombstones = true;
                    return;
                }

                SwapRemoveAt(index);
            }

            public void Clear()
            {
                for (int i = 0; i < owners.Count; i++)
                {
                    CustomBehaviour owner = owners[i];
                    if (owner == null) continue;

                    setIndex(owner, -1);
                    owner.ResetRegistration();
                }

                items.Clear();
                owners.Clear();
                hasTombstones = false;
            }

            private void SwapRemoveAt(int index)
            {
                int lastIndex = items.Count - 1;

                if (index != lastIndex)
                {
                    items[index] = items[lastIndex];
                    owners[index] = owners[lastIndex];

                    CustomBehaviour moved = owners[index];
                    if (moved != null) setIndex(moved, index);
                }

                items.RemoveAt(lastIndex);
                owners.RemoveAt(lastIndex);
            }

            private void Compact()
            {
                int write = 0;

                for (int read = 0; read < items.Count; read++)
                {
                    if (items[read] == null) continue;

                    if (write != read)
                    {
                        items[write] = items[read];
                        owners[write] = owners[read];
                        setIndex(owners[write], write);
                    }

                    write++;
                }

                items.RemoveRange(write, items.Count - write);
                owners.RemoveRange(write, owners.Count - write);
                hasTombstones = false;
            }
        }
    }
}
