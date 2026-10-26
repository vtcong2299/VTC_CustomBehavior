using UnityEngine;

namespace Vtcong.Core.Examples
{
    /// <summary>
    /// <see cref="ExampleProbe"/> with a lower execution order, used to check that
    /// BehaviourManager drives it before default-order behaviours.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class ExampleEarlyProbe : ExampleProbe
    {
    }
}
