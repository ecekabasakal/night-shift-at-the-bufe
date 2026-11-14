using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NightShift.EditorTools
{
    // Creates the one-object scene, applies the player settings and makes the WebGL build, from the menu or from the command line (-executeMethod).
    public static class ProjectBuilder
    {
        private const string SceneFolder = "Assets/Game/Scenes";
        private const string ScenePath = SceneFolder + "/Main.unity";
        private const string BuildFolder = "Builds/WebGL";
        private const string LogPrefix = "[ProjectBuilder] ";

        [MenuItem("Night Shift/Create Scene")]
        public static void CreateScene()
        {
            Run(nameof(CreateScene), () =>
            {
                CreateSceneCore();
                return true;
            });
        }

        [MenuItem("Night Shift/Configure Player Settings")]
        public static void ConfigurePlayerSettings()
        {
            Run(nameof(ConfigurePlayerSettings), () =>
            {
                ConfigurePlayerSettingsCore();
                return true;
            });
        }

        [MenuItem("Night Shift/Build WebGL")]
        public static void BuildWebGL()
        {
            Run(nameof(BuildWebGL), BuildWebGLCore);
        }

        // Runs one step, logs "<Method> done" or the failure, and in batch mode turns a failure into exit code 1.
        private static void Run(string methodName, Func<bool> step)
        {
            bool succeeded;
            try
            {
                succeeded = step();
            }
            catch (Exception exception)
            {
                Debug.LogError(LogPrefix + methodName + " FAILED: " + exception);
                succeeded = false;
            }

            if (succeeded)
            {
                Debug.Log(LogPrefix + methodName + " done");
            }
            else if (Application.isBatchMode)
            {
                EditorApplication.Exit(1);
            }
        }

        private static void CreateSceneCore()
        {
            Directory.CreateDirectory(SceneFolder);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("GameBootstrap").AddComponent<GameBootstrap>();

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new IOException("Could not save " + ScenePath);
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        private static void ConfigurePlayerSettingsCore()
        {
            PlayerSettings.productName = "Night Shift at the Büfe";
            PlayerSettings.companyName = "ecekabasakal";
            PlayerSettings.runInBackground = true;

            // Gamma is the simplest choice for flat pixel art and works in every browser.
            PlayerSettings.colorSpace = ColorSpace.Gamma;

            // The canvas matches the game's logical screen (GameLayout: 960 x 540).
            PlayerSettings.defaultWebScreenWidth = 960;
            PlayerSettings.defaultWebScreenHeight = 540;

            // itch.io does not send the "Content-Encoding" header that compressed builds need.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.template = "APPLICATION:Default";

            // The splash screen can only be turned off with a paid licence.
            if (Application.HasProLicense())
            {
                PlayerSettings.SplashScreen.show = false;
            }

            AssetDatabase.SaveAssets();
        }

        private static bool BuildWebGLCore()
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                throw new InvalidOperationException("WebGL Build Support is not installed for this Unity version.");
            }

            ConfigurePlayerSettingsCore();
            if (!File.Exists(ScenePath))
            {
                CreateSceneCore();
            }

            // Works whatever platform the project was last opened with.
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL
                && !EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.WebGL, BuildTarget.WebGL))
            {
                throw new InvalidOperationException("Could not switch the active build target to WebGL.");
            }

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = BuildFolder,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildSummary summary = BuildPipeline.BuildPlayer(options).summary;
            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError(LogPrefix + "BUILD FAILED: " + summary.totalErrors + " errors");
                return false;
            }

            Debug.Log(LogPrefix + "BUILD SUCCEEDED: " + summary.totalSize + " bytes, "
                + summary.totalTime.TotalSeconds.ToString("F1") + "s");
            return true;
        }
    }
}
