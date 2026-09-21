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

`BehaviourManager` must exist in the scene: it has no `[Singleton]` attribute, so
it is not auto-instantiated from `Resources`. Without it every `CustomBehaviour`
silently fails to register.

## The on-screen report

`BehaviourRegressionTests` spawns instrumented `ExampleProbe` behaviours, drives them
for a few frames and prints PASS/FAIL in the top-left corner. Each test reproduces a
bug the manager used to have, so all six fail against the pre-fix implementation:

1. **Removal mid-iteration does not skip** — disabling a behaviour from inside
   `OnBehaviourUpdate` used to swap the last entry into an already-visited slot,
   costing that behaviour a frame. Now removal leaves a tombstone and the list is
   compacted after the loop.
2. **Spawn mid-iteration runs after Start** — the loop cached `Count` is now read once,
   so a behaviour created during `Update` first ticks on the next frame, after Unity
   has called its `Start`.
3. **Duplicate `Register` is rejected** — a double registration used to append a second
   entry that could never be removed, leaving a zombie that ticked forever.
4. **Exception is isolated** — one throwing behaviour no longer aborts the frame for
   everything after it.
5. **Update / FixedUpdate / LateUpdate all fire.**
6. **Disable then enable re-registers.**

Two console messages are **expected** while the tests run: one warning from test 3
(`is already registered for Update`) and one exception from test 4
(`intentional test exception`).

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

`CustomBehaviour` keeps `OnEnable` / `Start` / `OnDisable` private on purpose — a
subclass declaring its own would shadow them and silently break registration. Use the
`OnBehaviour*` hooks above.
