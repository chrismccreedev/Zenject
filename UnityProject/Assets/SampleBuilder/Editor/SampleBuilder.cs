using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ModestTree;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

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

        static void EnableNet46()
        {
            PlayerSettings.scriptingRuntimeVersion = ScriptingRuntimeVersion.Latest;
            EditorApplication.Exit(0);
        }

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
                    string path = Path.Combine(
                        Application.dataPath, "../../SampleBuilds/Windows/ZenjectSamples.exe");

                    BuildGeneric(path, scenePaths, isRelease);
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
            string path, List<string> scenePaths, bool isRelease)
        {
            var options = BuildOptions.None;

            // Create the directory if it doesn't exist
            // Otherwise the build fails
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            if (!isRelease)
            {
                options |= BuildOptions.Development;
            }

            var report = BuildPipeline.BuildPlayer(scenePaths.ToArray(), path, EditorUserBuildSettings.activeBuildTarget, options);

            bool succeeded = (report.summary.result == BuildResult.Succeeded);

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
