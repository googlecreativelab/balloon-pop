//-----------------------------------------------------------------------
// <copyright file="CustomGradleHelper.cs" company="Google LLC">
// Copyright 2022 Google LLC
// </copyright>
//-----------------------------------------------------------------------

namespace Google.CreativeLab.BalloonPop.Editor
{
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using UnityEditor;
    using UnityEditor.Build;
    using UnityEditor.Build.Reporting;
    using UnityEngine;

    internal class CustomGradleHelper : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private const string _mainGradle = "Plugins/Android/mainTemplate.gradle";
        private const string _launcherGradle = "Plugins/Android/launcherTemplate.gradle";
        private const string _disableExtension = ".DISABLED";
        private const string _metaExtensions = ".meta";

        private const string _mainGradleTemplate =
#if UNITY_2020_2
            "Plugins/Android/mainGradle_2020_2.template";
#else
            "Plugins/Android/mainGradle.template";
#endif

        private const string _launcherGradleTemplate =
#if UNITY_2020_2
            "Plugins/Android/launcherGradle_2020_2.template";
#else
            "Plugins/Android/launcherGradle.template";
#endif

        [SuppressMessage("UnityRules.UnityStyleRules",
         "US1109:PublicPropertiesMustBeUpperCamelCase",
         Justification = "Overriden property.")]
        public int callbackOrder
        {
            get
            {
                return 0;
            }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.Android)
            {
#if UNITY_2019_4_OR_NEWER && !UNITY_2020_3_OR_NEWER
                // Use gradle plugin 4.0.1 in both main gradle and launcher gradle.
                EnableTemplate(true, _mainGradleTemplate, _mainGradle);
                EnableTemplate(true, _launcherGradleTemplate, _launcherGradle);
#else
                // Disable gradle template.
                EnableTemplate(false, _mainGradleTemplate, _mainGradle);
                EnableTemplate(false, _launcherGradleTemplate, _launcherGradle);
#endif
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.Android)
            {
                ResetGradleTemplate();
            }
        }

        private void EnableTemplate(bool enable, string tempalte, string gradle)
        {
            Debug.LogFormat(
                "{0} {1} in this build.", enable ? "Enabling" : "Disabling", gradle);

            string gradleFullPath = Path.Combine(Application.dataPath, gradle);
            string disableFullPath = gradleFullPath + _disableExtension;
            if (File.Exists(gradleFullPath) && !enable)
            {
                File.Copy(gradleFullPath, disableFullPath, true);
                File.Delete(gradleFullPath);
                File.Delete(gradleFullPath + _metaExtensions);
                AssetDatabase.Refresh();
            }

            if (enable)
            {
                if (File.Exists(disableFullPath))
                {
                    File.Delete(disableFullPath);
                    File.Delete(disableFullPath + _metaExtensions);
                }

                string sourceFullPath = Path.Combine(Application.dataPath, tempalte);
                if (!File.Exists(sourceFullPath))
                {
                    throw new BuildFailedException(
                        "Cannot find custom gradle template at " + sourceFullPath);
                }

                File.Copy(sourceFullPath, gradleFullPath, true);
                AssetDatabase.Refresh();
            }
        }

        private void ResetGradleTemplate()
        {
            Debug.Log("Reset Main Gradle Template and Launcher Gradle Template.");

            // The default state (checked in to Google3) for the gradle and launcher template has
            // both enabled.
            //
            // The default state must be with gradle and launcher templates enabled so that
            // AndroidSupportPreprocessBuild.cs (which runs before CustomGradleHelper.cs) works on
            // Unity 2019.4, 2020.1, 2020.2.
            EnableTemplate(true, _mainGradleTemplate, _mainGradle);
            EnableTemplate(true, _launcherGradleTemplate, _launcherGradle);
        }
    }
}
