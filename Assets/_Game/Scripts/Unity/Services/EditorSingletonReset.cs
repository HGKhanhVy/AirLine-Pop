#if UNITY_EDITOR
using System;
using System.Reflection;
using ASTeams.Base;
using UnityEngine;

namespace ASTeams.SingleLine.Unity
{
    /// <summary>
    /// Editor-only repair for the SDK singletons when Domain Reload is off.
    ///
    /// MonoSingleton raises a static shuttingDown flag on quit and never lowers it. With
    /// the domain kept alive between play sessions, every session after the first starts
    /// with the flag up, Instance answers null, and AudioController reads its saved mute
    /// flags off a missing profile. Builds always start fresh, so this never ships.
    /// </summary>
    internal static class EditorSingletonReset
    {
        private const string FlagName = "shuttingDown";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetShutdownFlags()
        {
            Type open = typeof(MonoSingleton<>);

            foreach (Type type in open.Assembly.GetTypes())
            {
                Type closed = FindClosedSingleton(type, open);

                if (closed == null || closed.ContainsGenericParameters)
                {
                    continue;
                }

                FieldInfo flag = closed.GetField(FlagName, BindingFlags.NonPublic | BindingFlags.Static);
                flag?.SetValue(null, false);
            }
        }

        private static Type FindClosedSingleton(Type type, Type open)
        {
            for (Type current = type.BaseType; current != null; current = current.BaseType)
            {
                if (current.IsGenericType && current.GetGenericTypeDefinition() == open)
                {
                    return current;
                }
            }

            return null;
        }
    }
}
#endif
