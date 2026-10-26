# VTC CustomBehaviour — Example

Open `CustomBehaviourExample.unity` and press **Play**.

## What the scene contains

| GameObject | Script | Loop |
| --- | --- | --- |
| `BehaviourManager` | `BehaviourManager` | drives all three loops |
| `Rotating Cube (Update)` | `ExampleRotator` | `IUpdateBehaviour` |
| `Bobbing Sphere (LateUpdate)` | `ExampleBobber` | `ILateUpdateBehaviour` |
| `Sliding Capsule (FixedUpdate)` | `ExampleFixedMover` | `IFixedUpdateBehaviour` |
| `Regression Tests` | `BehaviourRegressionTests` | on-screen test report |

The scene ships a `BehaviourManager`. Without one, the first `CustomBehaviour` to be
enabled creates a `[BehaviourManager]` (DontDestroyOnLoad) and logs a warning.

## The on-screen report

`BehaviourRegressionTests` spawns instrumented `ExampleProbe` behaviours, drives them
for a few frames and prints PASS/FAIL in the top-left corner. Each test reproduces a
bug the manager used to have, so all of them fail against the pre-fix implementation:

1. **Removal mid-iteration does not skip** — disabling a behaviour from inside
   `OnBehaviourUpdate` used to swap the last entry into an already-visited slot,
   costing that behaviour a frame. Now removal leaves a tombstone and the list is
   compacted before the next loop, keeping order.
2. **Spawn mid-iteration runs after Start** — the loop cached `Count` is now read once,
   so a behaviour created during `Update` first ticks on the next frame, after Unity
   has called its `Start`.
3. **Duplicate `Register` is rejected** — a double registration used to append a second
   entry that could never be removed, leaving a zombie that ticked forever.
4. **Exception is isolated** — one throwing behaviour no longer aborts the frame for
   everything after it.
5. **Update / FixedUpdate / LateUpdate all fire.**
6. **Disable then enable re-registers.**
7. **Removal keeps registration order** - removing from the middle used to swap the last
   entry into the gap, reordering everyone after it.
8. **`[DefaultExecutionOrder]` runs early first** - an `ExampleEarlyProbe` (order -100)
   registered after a default probe still ticks before it.
9. **Missing manager is created on demand** - runs last and destroys the scene's
   `BehaviourManager`; the next behaviour to enable creates a new one.

Three console messages are **expected** while the tests run: one warning from test 3
(`is already registered for Update`), one exception from test 4
(`intentional test exception`) and one warning from test 9 (`created one (DontDestroyOnLoad)`).

## Writing your own behaviour

```csharp
public class Spinner : CustomBehaviour, IUpdateBehaviour
{
    protected override void OnBehaviourStart() { /* instead of Start()  */ }
    protected override void OnBehaviourEnable() { /* instead of OnEnable() */ }
    protected override void OnBehaviourDisable() { /* instead of OnDisable() */ }

    public void OnBehaviourUpdate(float dt) => transform.Rotate(0f, 90f * dt, 0f);
}
```

Do not declare `OnEnable` / `Start` / `OnDisable` in a subclass: Unity would call only
yours and the behaviour would never register. Doing so raises compiler warning CS0108 and
an editor error naming the class. Use the `OnBehaviour*` hooks above.
