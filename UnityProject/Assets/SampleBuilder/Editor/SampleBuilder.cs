using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModestTree;
using UnityEditor;
using UnityEngine;

#if UNITY_2018_OR_NEWER
using UnityEditor.Build.Reporting;
#endif

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

        //[MenuItem("ZenjectSamples/Enable Net 46")]
        static void EnableNet46()
        {
            PlayerSettings.scriptingRuntimeVersion = ScriptingRuntimeVersion.Latest;
            EditorApplication.Exit(0);
        }

        //[MenuItem("ZenjectSamples/Enable Net 35")]
        static void EnableNet35()
        {
            PlayerSettings.scriptingRuntimeVersion = ScriptingRuntimeVersion.Legacy;
            EditorApplication.Exit(0);
        }

        static void BuildInternal(bool isRelease)
        {
            var scenePaths = UnityEditor.EditorBuildSettings.scenes
                .Select(x => x.path).ToList();

            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneWindows:
                {
                    string scriptingRuntimeDir;

                    if (PlayerSettings.scriptingRuntimeVersion == ScriptingRuntimeVersion.Latest)
                    {
                        scriptingRuntimeDir = "Net46";
                    }
                    else
                    {
                        scriptingRuntimeDir = "Net35";
                    }

                    BuildGeneric(
                        "Windows/{0}/ZenjectSamples.exe".Fmt(scriptingRuntimeDir), scenePaths, isRelease);
                    break;
                }
                case BuildTarget.WebGL:
                {
                    BuildGeneric("WebGl", scenePaths, isRelease);
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
                    BuildGeneric("WSA", scenePaths, isRelease);
                    break;
                }
                default:
                {
                    throw new Exception(
                        "Cannot build on platform '{0}'".Fmt(EditorUserBuildSettings.activeBuildTarget));
                }
            }
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

#if UNITY_2018_OR_NEWER
            bool succeeded = (buildResult.summary.result == BuildResult.Succeeded);
#else
            bool succeeded = (buildResult.Length == 0);
#endif

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
