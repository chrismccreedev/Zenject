using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModestTree;
using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;

namespace Zenject.Internal
{
    public class SampleBuilder
    {
        [MenuItem("ZenjectSamples/Build Debug")]
        public static void BuildDebug()
        {
            BuildInternal(false);
        }

        [MenuItem("ZenjectSamples/Build Release")]
        public static void BuildRelease()
        {
            BuildInternal(true);
        }

        static void EnableBackendIl2cpp()
        {
            PlayerSettings.SetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup, ScriptingImplementation.IL2CPP);
            EditorApplication.Exit(0);

        }

        static void EnableBackendNet()
        {
            PlayerSettings.SetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup, ScriptingImplementation.WinRTDotNET);
            EditorApplication.Exit(0);
        }

        static void BuildInternal(bool isRelease)
        {
            var scenePaths = UnityEditor.EditorBuildSettings.scenes
                .Select(x => x.path).ToList();

            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.StandaloneOSX: 
                {
                    BuildGeneric(
                        "OsX/{0}/ZenjectSamples".Fmt(GetScriptingBackendString()), scenePaths, isRelease);
                    break;
                }
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneWindows:
                {
                    BuildGeneric(
                        "Windows/{0}/ZenjectSamples.exe".Fmt(GetScriptingBackendString()), scenePaths, isRelease);
                    break;
                }
                case BuildTarget.WebGL:
                {
                    BuildGeneric("WebGl/{0}".Fmt(GetScriptingBackendString()), scenePaths, isRelease);
                    break;
                }
                case BuildTarget.Android:
                {
                    BuildGeneric("Android/ZenjectSamples.apk", scenePaths, isRelease);
                    break;
                }
                case BuildTarget.iOS:
                {
                    BuildGeneric("iOS", scenePaths, isRelease);
                    break;
                }
                case BuildTarget.WSAPlayer:
                {
                    BuildGeneric("WSA/{0}".Fmt(GetScriptingBackendString()), scenePaths, isRelease);
                    break;
                }
                default:
                {
                    throw new Exception(
                        "Cannot build on platform '{0}'".Fmt(EditorUserBuildSettings.activeBuildTarget));
                }
            }
        }

        static string GetScriptingBackendString()
        {
            var scriptingBackend = PlayerSettings.GetScriptingBackend(EditorUserBuildSettings.selectedBuildTargetGroup);

            if (scriptingBackend == ScriptingImplementation.IL2CPP) return "Il2cpp";
            if (scriptingBackend == ScriptingImplementation.Mono2x) return "Mono";

            Assert.IsEqual(scriptingBackend, ScriptingImplementation.WinRTDotNET);
            return ".NET";
        }

        static bool BuildGeneric(
            string relativePath, List<string> scenePaths, bool isRelease)
        {
            var options = BuildOptions.None;

            var path = Path.Combine(Path.Combine(Application.dataPath, "../../SampleBuilds"), relativePath);

            // Create the directory if it doesn't exist
            // Otherwise the build fails
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            if (!isRelease)
            {
                options |= BuildOptions.Development;
            }

            var buildResult = BuildPipeline.BuildPlayer(scenePaths.ToArray(), path, EditorUserBuildSettings.activeBuildTarget, options);

            bool succeeded = (buildResult.summary.result == BuildResult.Succeeded);

            if (succeeded)
            {
                Log.Info("Build completed successfully");
            }
            else
            {
                Log.Error("Error occurred while building");
            }

            if (UnityEditorInternal.InternalEditorUtility.inBatchMode)
            {
                EditorApplication.Exit(succeeded ? 0 : 1);
            }

            return succeeded;
        }
    }
}
