//-----------------------------------------------------------------------
// <copyright file="RuntimeSceneList.cs" company="Google">
// Copyright 2022 Google LLC
// </copyright>
//-----------------------------------------------------------------------
namespace Google.CreativeLab.BalloonPop.Common
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEngine;

#if UNITY_EDITOR
    using UnityEditor;
#endif

    /// <summary>
    /// Stores all active scenes as list then access in runtime for UI setup.
    /// </summary>
    public class RuntimeSceneList : ScriptableObject
    {
        /// <summary>
        /// A static instance for RuntimeSceneList.
        /// </summary>
        public static RuntimeSceneList Instance;

        /// <summary>
        /// A list contains all active scenes' name.
        /// </summary>
        public List<string> SceneNameList = new List<string>();

        private const string _assetName = "RuntimeSceneList.asset";

        /// <summary>
        /// Unity's OnEnable method.
        /// </summary>
        public void OnEnable()
        {
            Instance = this;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Upload given scene name list as a RuntimeSceneList asset into Preloaded Assets.
        /// </summary>
        /// <param name="sceneNameList">A list contains all active scene names.</param>
        public static void UploadActiveScenes(List<string> sceneNameList)
        {
            if (Instance == null)
            {
                _LoadInstance();
            }

            Instance.SceneNameList = sceneNameList;
            _UploadToPreloadedAsset();
        }

        private static void _LoadInstance()
        {
            string folderPath = "Assets/Configurations";
            if (!Directory.Exists(folderPath))
            {
                Debug.Log("Creating configurations directory.");
                Directory.CreateDirectory(folderPath);
            }

            string assetPath = folderPath + "/" + _assetName;
            if (!File.Exists(assetPath))
            {
                Debug.LogFormat("Creating RuntimeSceneList Asset in {0}.", folderPath);
                var newSceneList = CreateInstance<RuntimeSceneList>();
                AssetDatabase.CreateAsset(newSceneList, "Assets/Configurations/" +
                    _assetName);
                Instance = newSceneList;
            }
            else
            {
                var newSceneList = AssetDatabase.LoadAssetAtPath<RuntimeSceneList>(assetPath);
                Instance = newSceneList;
            }
        }

        private static void _UploadToPreloadedAsset()
        {
            var preloadedAssets = PlayerSettings.GetPreloadedAssets().ToList();
            preloadedAssets.RemoveAll(x => x.GetType() == typeof(RuntimeSceneList));
            preloadedAssets.Add(Instance);
            PlayerSettings.SetPreloadedAssets(preloadedAssets.ToArray());
        }
#endif
    }
}
