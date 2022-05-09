//-----------------------------------------------------------------------
// <copyright file="LoadingOpenScenePreprocess.cs" company="Google">
//
// Copyright 2022 Google LLC
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

namespace Google.CreativeLab.BalloonPop.Editor
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using BalloonPop.Common;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;

    /// <summary>
    /// Loading open scene preprocess which reads all enabled scenes in build settings and
    /// saves them as RuntimeSceneList and uploaded to preloaded assets.
    /// </summary>
    internal class LoadingOpenScenePreprocess : IPreprocessBuildWithReport
    {
        [SuppressMessage("UnityRules.UnityStyleRules", "US1109:PublicPropertiesMustBeUpperCamelCase",
         Justification = "Overriden property.")]
        public int callbackOrder
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Reads and uploads active <see cref="EditorBuildSettingsScene"/> on building preprocess.
        /// </summary>
        /// <param name="report">A report contains build information.</param>
        public void OnPreprocessBuild(BuildReport report)
        {
            EditorBuildSettingsScene[] editorBuildSettingsScenes = EditorBuildSettings.scenes;
            List<string> activeSceneNameList = new List<string>();
            foreach (var scene in editorBuildSettingsScenes)
            {
                if (!scene.enabled)
                {
                    continue;
                }

                activeSceneNameList.Add(Path.GetFileNameWithoutExtension(scene.path));
            }

            RuntimeSceneList.UploadActiveScenes(activeSceneNameList);
        }
    }
}
