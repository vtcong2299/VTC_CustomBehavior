using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vtcong.Core.Examples
{
    /// <summary>
    /// Plays through the failure modes BehaviourManager used to have and reports the
    /// outcome on screen. Every test here fails against the pre-fix implementation.
    /// </summary>
    public class BehaviourRegressionTests : MonoBehaviour
    {
        private struct Result
        {
            public string Name;
            public bool Passed;
            public string Detail;
        }

        private readonly List<Result> results = new();
        private readonly List<ExampleProbe> probes = new();

        private bool isFinished;

        private IEnumerator Start()
        {
            if (BehaviourManager.Instance == null)
            {
                Report("Setup", false, "No BehaviourManager in the scene - nothing can register.");
                isFinished = true;
                yield break;
            }

            yield return RemovalDuringIterationDoesNotSkipAnyone();
            yield return NewBehaviourWaitsForItsOwnStart();
            yield return DuplicateRegisterIsRejected();
            yield return ExceptionDoesNotStopTheLoop();
            yield return AllThreeLoopsRun();
            yield return DisableThenEnableReRegisters();

            foreach (ExampleProbe probe in probes)
                if (probe != null)
                    Destroy(probe.gameObject);

            probes.Clear();
            isFinished = true;
        }

        // The old swap-remove moved the last entry into an already-visited slot,
        // so the moved behaviour silently lost a frame.
        private IEnumerator RemovalDuringIterationDoesNotSkipAnyone()
        {
            ExampleProbe first = NewProbe("T1_First");
            ExampleProbe middle = NewProbe("T1_Middle");
            ExampleProbe trigger = NewProbe("T1_Trigger");
            ExampleProbe last = NewProbe("T1_Last");

            yield return null;
            yield return null;

            trigger.DuringUpdate = _ => first.enabled = false;

            int middleBefore = middle.UpdateCount;
            int lastBefore = last.UpdateCount;

            yield return null;

            trigger.DuringUpdate = null;

            int middleGain = middle.UpdateCount - middleBefore;
            int lastGain = last.UpdateCount - lastBefore;

            Report("Removal mid-iteration does not skip", middleGain == 1 && lastGain == 1,
                $"middle +{middleGain}, last +{lastGain} (expected +1 each)");
        }

        // The old loop re-read Count every step, so a behaviour spawned from inside
        // Update was driven in that same frame - before Unity had called its Start.
        private IEnumerator NewBehaviourWaitsForItsOwnStart()
        {
            ExampleProbe host = NewProbe("T2_Host");
            yield return null;

            ExampleProbe spawned = null;
            host.DuringUpdate = _ => spawned ??= NewProbe("T2_SpawnedMidFrame");

            yield return null;
            host.DuringUpdate = null;
            yield return null;

            bool passed = spawned != null && !spawned.UpdatedBeforeStart && spawned.UpdateCount > 0;
            Report("Spawn mid-iteration runs after Start", passed,
                spawned == null
                    ? "probe was never spawned"
                    : $"updatedBeforeStart={spawned.UpdatedBeforeStart}, updates={spawned.UpdateCount}");
        }

        // Registering twice used to append a second entry that could never be removed,
        // so the behaviour ticked twice per frame and kept ticking after being disabled.
        private IEnumerator DuplicateRegisterIsRejected()
        {
            ExampleProbe probe = NewProbe("T3_Duplicate");
            yield return null;

            BehaviourManager.Instance.Register(probe); // expected: one warning in the console

            int before = probe.UpdateCount;
            yield return null;
            int perFrame = probe.UpdateCount - before;

            probe.enabled = false;
            int afterDisable = probe.UpdateCount;
            yield return null;
            bool stopped = probe.UpdateCount == afterDisable;

            Report("Duplicate Register is rejected", perFrame == 1 && stopped,
                $"{perFrame} tick/frame (expected 1), stopped on disable={stopped}");
        }

        // One throwing behaviour used to abort the whole frame for every behaviour after it.
        private IEnumerator ExceptionDoesNotStopTheLoop()
        {
            ExampleProbe thrower = NewProbe("T4_Thrower");
            ExampleProbe victim = NewProbe("T4_AfterThrower");
            yield return null;

            thrower.ThrowOnUpdate = true;
            int before = victim.UpdateCount;

            yield return null;

            thrower.ThrowOnUpdate = false;
            int gain = victim.UpdateCount - before;

            Report("Exception is isolated", gain == 1,
                $"behaviour after the thrower got +{gain} (expected +1); 1 exception is expected in the console");
        }

        private IEnumerator AllThreeLoopsRun()
        {
            ExampleProbe probe = NewProbe("T5_AllLoops");

            for (int i = 0; i < 10; i++)
                yield return null;

            bool passed = probe.UpdateCount > 0 && probe.FixedUpdateCount > 0 && probe.LateUpdateCount > 0;
            Report("Update / FixedUpdate / LateUpdate all fire", passed,
                $"update={probe.UpdateCount}, fixed={probe.FixedUpdateCount}, late={probe.LateUpdateCount}");
        }

        private IEnumerator DisableThenEnableReRegisters()
        {
            ExampleProbe probe = NewProbe("T6_Toggle");
            yield return null;

            probe.enabled = false;
            yield return null;
            int whileDisabled = probe.UpdateCount;
            yield return null;
            bool stopped = probe.UpdateCount == whileDisabled;

            probe.enabled = true;
            yield return null;
            yield return null;
            bool resumed = probe.UpdateCount > whileDisabled;

            Report("Disable then enable re-registers", stopped && resumed,
                $"stopped={stopped}, resumed={resumed}");
        }

        private ExampleProbe NewProbe(string probeName)
        {
            ExampleProbe probe = ExampleProbe.Create(probeName, transform);
            probes.Add(probe);
            return probe;
        }

        private void Report(string testName, bool passed, string detail)
        {
            results.Add(new Result { Name = testName, Passed = passed, Detail = detail });
        }

        private void OnGUI()
        {
            const float width = 620f;
            float height = 60f + results.Count * 56f;

            GUILayout.BeginArea(new Rect(12f, 12f, width, height), GUI.skin.box);

            int passedCount = 0;
            foreach (Result result in results)
                if (result.Passed)
                    passedCount++;

            string header = isFinished
                ? $"BehaviourManager regression tests: {passedCount}/{results.Count} passed"
                : $"BehaviourManager regression tests: running... ({results.Count} done)";

            GUILayout.Label($"<b>{header}</b>", RichLabel());

            foreach (Result result in results)
            {
                string tag = result.Passed ? "<color=#4CD964>PASS</color>" : "<color=#FF3B30>FAIL</color>";
                GUILayout.Label($"{tag}  {result.Name}\n      <i>{result.Detail}</i>", RichLabel());
            }

            GUILayout.EndArea();
        }

        private static GUIStyle richLabel;

        private static GUIStyle RichLabel()
        {
            richLabel ??= new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true };
            return richLabel;
        }
    }
}
