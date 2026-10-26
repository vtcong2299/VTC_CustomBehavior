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

        private static readonly Dictionary<Type, int> Orders = new();
        private static bool isQuitting;

        public override void Init()
        {
        }

        /// <summary>
        /// The manager a behaviour should register with: the live instance, one in the scene that
        /// has not run Awake yet, or a new DontDestroyOnLoad one created on demand (with a warning).
        /// Returns null outside play mode and while the application is quitting.
        /// </summary>
        public static BehaviourManager GetOrCreate()
        {
            if (Instance != null) return Instance;

#if UNITY_2023_1_OR_NEWER
            BehaviourManager existing = FindAnyObjectByType<BehaviourManager>();
#else
            BehaviourManager existing = FindObjectOfType<BehaviourManager>();
#endif
            if (existing != null) return existing;
            if (!Application.isPlaying || isQuitting) return null;

            GameObject host = new("[BehaviourManager]");
            DontDestroyOnLoad(host);
            Debug.LogWarning(
                "[BehaviourManager] No BehaviourManager in the scene, created one (DontDestroyOnLoad). " +
                "Add a BehaviourManager to the scene to control its lifetime.", host);
            return host.AddComponent<BehaviourManager>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            isQuitting = false;
            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;
        }

        private static void OnQuitting()
        {
            isQuitting = true;
        }

        // same attribute Unity uses for script order; read once per type, not inherited (as in Unity)
        private static int OrderOf(Type type)
        {
            if (Orders.TryGetValue(type, out int order)) return order;

            var attribute = (DefaultExecutionOrder)Attribute.GetCustomAttribute(type, typeof(DefaultExecutionOrder), false);
            order = attribute != null ? attribute.order : 0;
            Orders[type] = order;
            return order;
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
        }

        public void Register(CustomBehaviour behaviour)
        {
            if (behaviour == null) return;

            int order = OrderOf(behaviour.GetType());

            if (behaviour is IUpdateBehaviour updateBehaviour)
                updateBehaviours.Add(behaviour, updateBehaviour, order);

            if (behaviour is IFixedUpdateBehaviour fixedUpdateBehaviour)
                fixedUpdateBehaviours.Add(behaviour, fixedUpdateBehaviour, order);

            if (behaviour is ILateUpdateBehaviour lateUpdateBehaviour)
                lateUpdateBehaviours.Add(behaviour, lateUpdateBehaviour, order);
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
        /// A list kept in (order, registration) sequence that stays safe to mutate while it is
        /// being iterated: removals leave a null tombstone (so every cached index stays valid) and
        /// additions are appended past the cached count (so they only run from the next frame).
        /// Tombstones and new entries are folded in once, at the start of the next iteration,
        /// with a stable merge - so removing never reorders the survivors.
        /// </summary>
        private sealed class BehaviourList<T> where T : class
        {
            private struct Entry
            {
                public T Item;
                public CustomBehaviour Owner;
                public int Order;
                public long Sequence;
            }

            private static readonly Comparison<Entry> ByOrder = (a, b) =>
                a.Order != b.Order ? a.Order.CompareTo(b.Order) : a.Sequence.CompareTo(b.Sequence);

            private List<Entry> entries = new(InitialCapacity);
            private List<Entry> buffer = new(InitialCapacity);
            private readonly List<Entry> pending = new();
            private readonly Func<CustomBehaviour, int> getIndex;
            private readonly Action<CustomBehaviour, int> setIndex;
            private readonly string label;

            private int sortedCount;
            private bool hasTombstones;
            private long nextSequence;

            public BehaviourList(string label, Func<CustomBehaviour, int> getIndex,
                Action<CustomBehaviour, int> setIndex)
            {
                this.label = label;
                this.getIndex = getIndex;
                this.setIndex = setIndex;
            }

            public T this[int index] => entries[index].Item;

            public int BeginIteration()
            {
                if (hasTombstones || sortedCount < entries.Count) Normalize();
                return entries.Count;
            }

            public void Add(CustomBehaviour owner, T item, int order)
            {
                if (getIndex(owner) >= 0)
                {
                    Debug.LogWarning(
                        $"[BehaviourManager] '{owner.name}' is already registered for {label}. Ignoring duplicate.",
                        owner);
                    return;
                }

                setIndex(owner, entries.Count);
                entries.Add(new Entry { Item = item, Owner = owner, Order = order, Sequence = nextSequence++ });
            }

            public void Remove(CustomBehaviour owner, T item)
            {
                int index = getIndex(owner);
                if (index < 0) return;

                setIndex(owner, -1);

                if (index >= entries.Count || !ReferenceEquals(entries[index].Item, item))
                {
                    Debug.LogError(
                        $"[BehaviourManager] {label} index {index} of '{owner.name}' does not match its slot. " +
                        "The registry was mutated from outside; dropping the entry.", owner);
                    return;
                }

                entries[index] = default;
                hasTombstones = true;
            }

            public void Clear()
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    CustomBehaviour owner = entries[i].Owner;
                    if (owner == null) continue;

                    setIndex(owner, -1);
                    owner.ResetRegistration();
                }

                entries.Clear();
                sortedCount = 0;
                hasTombstones = false;
            }

            private void Normalize()
            {
                if (!hasTombstones && TailIsInOrder())
                {
                    sortedCount = entries.Count;
                    return;
                }

                pending.Clear();
                for (int i = sortedCount; i < entries.Count; i++)
                    if (entries[i].Item != null)
                        pending.Add(entries[i]);
                pending.Sort(ByOrder);

                buffer.Clear();
                int head = 0;
                int tail = 0;

                while (true)
                {
                    while (head < sortedCount && entries[head].Item == null) head++;

                    bool hasHead = head < sortedCount;
                    bool hasTail = tail < pending.Count;
                    if (!hasHead && !hasTail) break;

                    if (hasHead && (!hasTail || ByOrder(entries[head], pending[tail]) <= 0))
                        buffer.Add(entries[head++]);
                    else
                        buffer.Add(pending[tail++]);
                }

                (entries, buffer) = (buffer, entries);
                buffer.Clear();
                pending.Clear();

                for (int i = 0; i < entries.Count; i++)
                    setIndex(entries[i].Owner, i);

                sortedCount = entries.Count;
                hasTombstones = false;
            }

            // the common case - everything new appended in order - needs no merge and no reindex
            private bool TailIsInOrder()
            {
                for (int i = Math.Max(sortedCount, 1); i < entries.Count; i++)
                    if (ByOrder(entries[i - 1], entries[i]) > 0)
                        return false;

                return true;
            }
        }
    }
}
