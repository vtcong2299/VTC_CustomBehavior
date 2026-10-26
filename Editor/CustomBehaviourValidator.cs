using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Vtcong.Core
{
    internal static class CustomBehaviourValidator
    {
        private const BindingFlags Declared =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private static readonly (string Message, string Hook)[] Reserved =
        {
            ("OnEnable", "OnBehaviourEnable"),
            ("Start", "OnBehaviourStart"),
            ("OnDisable", "OnBehaviourDisable"),
        };

        [InitializeOnLoadMethod]
        private static void Validate()
        {
            foreach (Type type in TypeCache.GetTypesDerivedFrom<CustomBehaviour>())
            foreach (var (message, hook) in Reserved)
                if (type.GetMethod(message, Declared, null, Type.EmptyTypes, null) != null)
                    Debug.LogError(
                        $"[CustomBehaviour] {type.FullName}.{message}() replaces CustomBehaviour.{message}, " +
                        $"so this behaviour never registers with BehaviourManager. Override {hook}() instead.");
        }
    }
}
