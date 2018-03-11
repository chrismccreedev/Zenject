﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using ModestTree;
using System.Linq;
using UnityEngine.SceneManagement;

namespace Zenject.Internal
{
    [InitializeOnLoad]
    public static class SceneParentAutomaticLoader
    {
        static SceneParentAutomaticLoader()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                try
                {
                    ValidateMultiSceneSetupAndLoadDefaultSceneParents();
                }
                catch (Exception e)
                {
                    EditorApplication.isPlaying = false;
                    throw new ZenjectException(
                        "Failure occurred when attempting to load default scene parent contracts!", e);
                }
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                // It would be cool to restore the initial scene set up here but in order to do this
                // we would have to make sure that the user saves the scene before running which
                // would be too annoying, so just leave any changes we've made alone
            }
        }

        public static void ValidateMultiSceneSetupAndLoadDefaultSceneParents()
        {
            var defaultContractsMap = LoadDefaultContractsMap();

            // NOTE: Even if configs is empty we still want to do the below logic to validate the
            // multi scene setup

            var sceneInfos = GetLoadedZenjectSceneInfos();
            var contractMap = GetCurrentSceneContractsMap(sceneInfos);

            foreach (var sceneInfo in sceneInfos)
            {
                ProcessScene(sceneInfo, contractMap, defaultContractsMap);
            }
        }

        static Dictionary<string, LoadedSceneInfo> GetCurrentSceneContractsMap(
            List<LoadedSceneInfo> sceneInfos)
        {
            var contractMap = new Dictionary<string, LoadedSceneInfo>();

            foreach (var info in sceneInfos)
            {
                AddToContractMap(contractMap, info);
            }

            return contractMap;
        }

        static void ProcessScene(
            LoadedSceneInfo sceneInfo,
            Dictionary<string, LoadedSceneInfo> contractMap,
            Dictionary<string, string> defaultContractsMap)
        {
            foreach (var parentContractName in sceneInfo.SceneContext.ParentContractNames)
            {
                LoadedSceneInfo parentInfo;

                if (contractMap.TryGetValue(parentContractName, out parentInfo))
                {
                    ValidateParentChildMatch(parentInfo, sceneInfo);
                    continue;
                }

                string parentScenePath;

                if (!defaultContractsMap.TryGetValue(parentContractName, out parentScenePath))
                {
                    throw Assert.CreateException(
                        "Could not fill parent contract '{0}' for scene '{1}'.  No scenes with that contract name are loaded, and could not find a match in any default scene contract configs to auto load one either."
                        .Fmt(parentContractName, sceneInfo.Scene.name));
                }

                parentInfo = OpenParentScene(sceneInfo, parentScenePath);

                AddToContractMap(contractMap, parentInfo);

                EditorSceneManager.MoveSceneBefore(parentInfo.Scene, sceneInfo.Scene);

                ValidateParentChildMatch(parentInfo, sceneInfo);

                ProcessScene(parentInfo, contractMap, defaultContractsMap);
            }
        }

        static LoadedSceneInfo OpenParentScene(LoadedSceneInfo sceneInfo, string parentScenePath)
        {
            Scene parentScene;

            try
            {
                parentScene = EditorSceneManager.OpenScene(
                    parentScenePath, OpenSceneMode.Additive);
            }
            catch (Exception e)
            {
                throw new ZenjectException(
                    "Error while attempting to load parent contracts for scene '{0}'".Fmt(sceneInfo.Scene.name), e);
            }

            var sceneContext = ZenUnityEditorUtil.GetSceneContextForScene(parentScene);

            return new LoadedSceneInfo()
            {
                Scene = parentScene,
                SceneContext = sceneContext
            };
        }

        static void ValidateParentChildMatch(
            LoadedSceneInfo parentSceneInfo, LoadedSceneInfo sceneInfo)
        {
            var parentIndex = GetSceneIndex(parentSceneInfo.Scene);
            var childIndex = GetSceneIndex(sceneInfo.Scene);
            var activeIndex = GetSceneIndex(EditorSceneManager.GetActiveScene());

            Assert.That(parentIndex < childIndex,
                "Parent scene '{0}' must be loaded before child scene '{1}'.  Please drag it to be placed above its child in the scene hierarchy.", parentSceneInfo.Scene.name, sceneInfo.Scene.name);

            if (activeIndex > parentIndex)
            {
                EditorSceneManager.SetActiveScene(parentSceneInfo.Scene);
            }
        }

        static int GetSceneIndex(Scene scene)
        {
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                if (EditorSceneManager.GetSceneAt(i) == scene)
                {
                    return i;
                }
            }

            throw Assert.CreateException();
        }

        static Dictionary<string, string> LoadDefaultContractsMap()
        {
            var configs = Resources.LoadAll<DefaultSceneContractConfig>(DefaultSceneContractConfig.ResourcePath);

            var map = new Dictionary<string, string>();

            foreach (var config in configs)
            {
                foreach (var info in config.DefaultContracts)
                {
                    if (info.ContractName.Trim().IsEmpty())
                    {
                        Log.Warn("Found empty contract name in default scene contract config");
                        continue;
                    }

                    Assert.That(!map.ContainsKey(info.ContractName),
                        "Found multiple default scenes for contract '{0}' in default scene contract configs at resource path 'Resources/{0}'!", DefaultSceneContractConfig.ResourcePath);

                    map.Add(info.ContractName, info.ScenePath);
                }
            }

            return map;
        }

        static List<LoadedSceneInfo> GetLoadedZenjectSceneInfos()
        {
            var result = new List<LoadedSceneInfo>();

            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);

                if (!scene.isLoaded)
                {
                    continue;
                }

                var sceneContext = ZenUnityEditorUtil.TryGetSceneContextForScene(scene);

                if (sceneContext != null)
                {
                    var info = new LoadedSceneInfo()
                    {
                        SceneContext = sceneContext,
                        Scene = scene
                    };

                    result.Add(info);
                }
            }

            return result;
        }

        public class LoadedSceneInfo
        {
            public SceneContext SceneContext;
            public Scene Scene;
        }

        static void AddToContractMap(
            Dictionary<string, LoadedSceneInfo> contractMap, LoadedSceneInfo info)
        {
            foreach (var contractName in info.SceneContext.ContractNames)
            {
                LoadedSceneInfo currentInfo;

                if (contractMap.TryGetValue(contractName, out currentInfo))
                {
                    throw Assert.CreateException(
                        "Found multiple scene contracts with name '{0}'. Scene '{1}' and scene '{2}'",
                        contractName, currentInfo.Scene.name, info.Scene.name);
                }

                contractMap.Add(contractName, info);
            }
        }
    }
}
