// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Build.Locator;

namespace ApiDocsSync.Libraries
{
    // Per the documentation: https://docs.microsoft.com/en-us/visualstudio/msbuild/updating-an-existing-application
    // Do not call any of these APIs from the same context where Microsoft.Build APIs are being called.
    public static class VSLoader
    {
        private static readonly string[] s_candidateExtensions = new[] { "ni.dll", "ni.exe", "dll", "exe" };
        private static readonly Dictionary<string, Assembly> s_pathsToAssemblies = new(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Assembly> s_namesToAssemblies = new();
        private static readonly object s_guard = new();

        public static VisualStudioInstance? VSInstance { get; private set; }

        // Per the documentation: https://docs.microsoft.com/en-us/visualstudio/msbuild/updating-an-existing-application
        // Cannot reference any MSBuild types (from Microsoft.Build namespace) in the same method that calls MSBuildLocator.
        public static void LoadVSInstance()
        {
            Log.Info("Querying for all Visual Studio instances...");
            IEnumerable<VisualStudioInstance> vsBuildInstances = MSBuildLocator.QueryVisualStudioInstances();

            if (!vsBuildInstances.Any())
            {
                throw new Exception("No VS instances found.");
            }

            Log.Info("Looking for the latest stable instance of Visual Studio, if there is one...");
            VSInstance = vsBuildInstances.Where(b => !b.MSBuildPath.Contains("-preview"))
                                         .OrderByDescending(b => b.Version)
                                         .FirstOrDefault() ??
                         vsBuildInstances.First();
            Log.Success($"Selected instance:{Environment.NewLine}  - MSBuildPath: {VSInstance.MSBuildPath}{Environment.NewLine}  - Version: {VSInstance.Version}");

            // Unit tests execute this multiple times, ensure we only register once
            if (MSBuildLocator.CanRegister)
            {
                Log.Info("Attempting to register assembly loader...");
                RegisterAssemblyLoader(VSInstance.MSBuildPath);
                Log.Info("Attempting to register Visual Studio instance");
                MSBuildLocator.RegisterInstance(VSInstance);
                Log.Success("Successful Visual Studio load!");
            }
            else
            {
                Log.Error("Could not register the Visual Studio instance (CanRegister=false).");
            }
        }

        // Register an assembly loader that will load assemblies with higher version than what was requested.
        private static void RegisterAssemblyLoader(string searchPath)
        {
            AssemblyLoadContext.Default.Resolving += (AssemblyLoadContext context, AssemblyName assemblyName) =>
            {
                lock (s_guard)
                {
                    if (s_namesToAssemblies.TryGetValue(assemblyName.FullName, out Assembly? cachedAssembly))
                    {
                        return cachedAssembly;
                    }

                    if (TryResolveAssemblyFromPaths(context, assemblyName, searchPath, out Assembly? assembly))
                    {
                        // Cache assembly
                        string? name = assembly.FullName;
                        if (name is null)
                        {
                            throw new Exception($"Could not get name for assembly '{assembly}'");
                        }

                        s_pathsToAssemblies[assembly.Location] = assembly;
                        s_namesToAssemblies[name] = assembly;

                        return assembly;
                    }

                    return null;
                }
            };
        }

        // Tries to find and return the specified assembly by looking in all the known locations where it could be found.
        private static bool TryResolveAssemblyFromPaths(AssemblyLoadContext context, AssemblyName assemblyName, string searchPath, [NotNullWhen(returnValue: true)] out Assembly? resolvedAssembly)
        {
            resolvedAssembly = null;
            foreach (string cultureSubfolder in GetCultureSubfolders(assemblyName))
            {
                foreach (string extension in s_candidateExtensions)
                {
                    string candidatePath = Path.Combine(searchPath, cultureSubfolder, $"{assemblyName.Name}.{extension}");
                    if (s_pathsToAssemblies.ContainsKey(candidatePath) || !File.Exists(candidatePath))
                    {
                        continue;
                    }

                    AssemblyName candidateAssemblyName = AssemblyLoadContext.GetAssemblyName(candidatePath);
                    if (candidateAssemblyName.Version < assemblyName.Version)
                    {
                        continue;
                    }

                    try
                    {
                        resolvedAssembly = context.LoadFromAssemblyPath(candidatePath);
                        return resolvedAssembly != null;
                    }
                    catch
                    {
                        if (assemblyName.Name != null)
                        {
                            // We were unable to load the assembly from the file path. It is likely that
                            // a different version of the assembly has already been loaded into the context.
                            // Be forgiving and attempt to load assembly by name without specifying a version.
                            resolvedAssembly = context.LoadFromAssemblyName(new AssemblyName(assemblyName.Name));
                            return resolvedAssembly != null;
                        }
                    }
                }
            }

            return false;
        }

        private static IEnumerable<string> GetCultureSubfolders(AssemblyName assemblyName)
        {
            if (!string.IsNullOrEmpty(assemblyName.CultureName))
            {
                // Search for satellite assemblies in culture subdirectories of the assembly search
                // directories, but fall back to the bare search directory if that fails.
                yield return assemblyName.CultureName;
            }
            // If no culture is specified, attempt to load directly from the known dependency paths.
            yield return string.Empty;
        }
    }
}
