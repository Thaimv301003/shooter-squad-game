using System;
using System.Collections;
using BFunCoreKit;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
#if BFUN_INSTALLED_TRUE
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif
using UnityEngine.SceneManagement;


namespace BFunCoreKit
{
    // ------------------------------------------------------------------------------------
    // PHẦN 2: LOAD MANAGER (Code chính)
    // ------------------------------------------------------------------------------------
    public class LoadManager : Singleton<LoadManager>
    {
        public static IEnumerator LoadScene(string scenePath, LoadSceneMode mode = LoadSceneMode.Single)
        {
            Application.backgroundLoadingPriority = ThreadPriority.High;

            switch (mode)
            {
                case LoadSceneMode.Single:
                    yield return Instance.StartCoroutine(LoadSceneInternal(scenePath));
                    break;

                case LoadSceneMode.Additive:
                    yield return SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
            Application.backgroundLoadingPriority = ThreadPriority.Low;
        }

        private static IEnumerator LoadSceneInternal(string scenePath)
        {
            var buildIndex = SceneUtility.GetBuildIndexByScenePath(scenePath);
            if (Debug.isDebugBuild)
                Debug.Log($"Loading Scene {scenePath} at Build Index {buildIndex}");

            // get current scene and set a loading scene as active
            var currentScene = SceneManager.GetActiveScene();
            var loadingScene = SceneManager.CreateScene("Loading_Background");
            SceneManager.SetActiveScene(loadingScene);

            // unload last scene
            var unload = SceneManager.UnloadSceneAsync(currentScene, UnloadSceneOptions.None);
            while (!unload.isDone)
            {
                yield return null;
            }

            // clean up
            var clean = Resources.UnloadUnusedAssets();
            while (!clean.isDone) { yield return null; }

            // load new scene
            var load = new AsyncOperation();
#if UNITY_EDITOR
            if (buildIndex == -1)
            {
                load = EditorSceneManager.LoadSceneAsyncInPlayMode(scenePath,
                    new LoadSceneParameters(LoadSceneMode.Single));
            }
            else
            {
                load = SceneManager.LoadSceneAsync(buildIndex);
            }
#else
            load = SceneManager.LoadSceneAsync(scenePath);
#endif
            while (!load.isDone)
            {
                yield return null;
            }
        }
#if BFUN_INSTALLED_TRUE
        public static IEnumerator LoadPrefab<T>(AssetReference assetRef, AsyncOperationHandle assetLoading, Transform parent = null)
        {
            if (typeof(T) == typeof(GameObject))
            {
                assetLoading = assetRef.InstantiateAsync(parent);
            }
            else
            {
                assetLoading = assetRef.LoadAssetAsync<T>();
            }
            yield return assetLoading;
        }
#endif

        private static void CmdArgs()
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length <= 0) return;
            foreach (var argRaw in args)
            {
                if (string.IsNullOrEmpty(argRaw) || argRaw[0] != '-') continue;
                var arg = argRaw.Split(':');

                switch (arg[0])
                {
                    case "-loadlevel":
                        LoadScene(arg[1]);
                        break;
                    case "-benchmarkFlythrough":
                        LoadScene("benchmark_island-flythrough");
                        break;
                }
            }
        }
    }
}