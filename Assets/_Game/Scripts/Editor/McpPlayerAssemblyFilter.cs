#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.Build;
using UnityEngine;

namespace ASTeams.SingleLine.Editor
{
    /// <summary>
    /// Keeps the Unity MCP editor tooling out of player builds.
    ///
    /// The MCP package ships a runtime assembly whose asmdef targets every platform, and it
    /// drags in SignalR, System.Text.Json, the Microsoft.Extensions container and Roslyn. All
    /// of that is generic and reflection heavy, so IL2CPP expands it into tens of megabytes of
    /// native code: a measured 259 MB APK carried 113 MB of arm64 libil2cpp.so for a game
    /// whose own assets are 9.8 MB.
    ///
    /// The obvious fixes do not hold. The package re-adds its UNITY_MCP_READY gate define to
    /// every build target on each domain reload, and it rewrites the PluginImporter settings of
    /// its NuGet DLLs from its own manifest, so editing either by hand is undone on the next
    /// compile. Filtering at build time is the one place the decision sticks.
    ///
    /// Nothing under _Game, _SDK or UXUI references these assemblies, so dropping them cannot
    /// leave a dangling reference. The editor keeps them: this callback only runs for players.
    /// </summary>
    public sealed class McpPlayerAssemblyFilter : IFilterBuildAssemblies
    {
        /// <summary>Late, so anything that wants to inspect the full list still sees it.</summary>
        public int callbackOrder => 100;

        /// <summary>
        /// Assemblies whose name starts with one of these is editor tooling. Prefixes rather
        /// than exact names: the MCP dependency set changes between package versions, and a
        /// list of exact names would silently stop matching after an upgrade.
        /// </summary>
        private static readonly string[] DroppedPrefixes =
        {
            "com.IvanMurzak.",
            "McpPlugin",
            "ReflectorNet",
            "R3",
            "Microsoft.AspNetCore.",
            "Microsoft.Extensions.",
            "Microsoft.Bcl.",
            "Microsoft.CodeAnalysis",
            "System.Text.Json",
            "System.Text.Encodings.Web",
            "System.Text.Encoding.CodePages",
            "System.IO.Pipelines",
            "System.IO.Hashing",
            "System.Threading.Channels",
            "System.Reflection.Metadata",
            "System.Collections.Immutable",
            "System.Diagnostics.DiagnosticSource",
            "System.ComponentModel.Annotations"
        };

        /// <summary>
        /// Names that survive a prefix match above. Newtonsoft is its own Unity package and the
        /// project compiles against it (the NEWTONSOFT define), and Unsafe backs UniTask.
        /// </summary>
        private static readonly string[] KeptNames =
        {
            "Newtonsoft.Json",
            "System.Runtime.CompilerServices.Unsafe"
        };

        public string[] OnFilterAssemblies(UnityEditor.BuildOptions buildOptions, string[] assemblies)
        {
            var kept = new List<string>(assemblies.Length);
            int dropped = 0;

            for (int i = 0; i < assemblies.Length; i++)
            {
                if (IsEditorTooling(Path.GetFileNameWithoutExtension(assemblies[i])))
                {
                    dropped++;
                    continue;
                }

                kept.Add(assemblies[i]);
            }

            if (dropped > 0)
            {
                Debug.Log($"[McpPlayerAssemblyFilter] Left {dropped} editor-only assemblies out of the player build.");
            }

            return kept.ToArray();
        }

        private static bool IsEditorTooling(string assemblyName)
        {
            for (int i = 0; i < KeptNames.Length; i++)
            {
                if (string.Equals(assemblyName, KeptNames[i], StringComparison.Ordinal))
                {
                    return false;
                }
            }

            for (int i = 0; i < DroppedPrefixes.Length; i++)
            {
                if (assemblyName.StartsWith(DroppedPrefixes[i], StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif
