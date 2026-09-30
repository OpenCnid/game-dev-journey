using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Journey.Editor
{
    public static class ProjectSetup
    {
        public const string EditorVersion = "6000.3.25f1";
        public const string ScenePath = "Assets/Prototype/Scenes/Arena.unity";
        const string PipelinePath = "Assets/Prototype/Settings/JourneyPipeline.asset";

        [MenuItem("Journey/Prepare project")]
        public static void Prepare()
        {
            if (Application.unityVersion != EditorVersion)
                throw new InvalidOperationException("Use pinned Unity " + EditorVersion + ", found " + Application.unityVersion);
            Directory.CreateDirectory("Assets/Prototype/Settings");
            Directory.CreateDirectory("Assets/Prototype/Scenes");
            Directory.CreateDirectory("Assets/Prototype/Resources");
            AssetDatabase.Refresh();
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, "Assets/Prototype/Settings/JourneyRenderer.asset");
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                pipeline.msaaSampleCount = 1;
                pipeline.renderScale = 1;
                pipeline.supportsHDR = false;
                pipeline.mainLightShadowmapResolution = 512;
                pipeline.shadowDistance = 0;
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            QualitySettings.vSyncCount = 0;
            QualitySettings.antiAliasing = 0;
            QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;
            QualitySettings.pixelLightCount = 1;
            PlayerSettings.companyName = "Game Dev Journey";
            PlayerSettings.productName = "Stack Lab";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.runInBackground = true;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.usePlayerLog = true;
            PlayerSettings.enableFrameTimingStats = true;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, new[] { GraphicsDeviceType.Direct3D11 });
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            SerializedProperty input = settings.FindProperty("activeInputHandler");
            if (input != null) { input.intValue = 1; settings.ApplyModifiedPropertiesWithoutUndo(); }

            // Serialized material references preserve the runtime-created primitive shaders in player builds.
            KeepMaterial("Body", "Universal Render Pipeline/Simple Lit");
            KeepMaterial("Trail", "Universal Render Pipeline/Unlit");
            var graphics = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            SerializedProperty stripping = graphics.FindProperty("m_InstancingStripping");
            if (stripping != null) { stripping.intValue = 0; graphics.ApplyModifiedPropertiesWithoutUndo(); }

            if (!File.Exists(ScenePath))
            {
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("Stack Lab").AddComponent<PrototypeGame>();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("PROJECT_READY " + EditorVersion + " " + ScenePath);
        }

        [MenuItem("Journey/Open arena")]
        public static void OpenArena()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Prepare(); EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Journey/Build Windows player")]
        public static void BuildWindows()
        {
            Prepare();
            var stamp = new BuildStamp { builtUtc = DateTime.UtcNow.ToString("o") };
            try
            {
                stamp.revision = Git("rev-parse HEAD").Trim();
                stamp.dirty = Git("status --porcelain").Length > 0;
            }
            catch (Exception e) { Debug.LogWarning("Build revision unavailable: " + e.Message); }
            File.WriteAllText("Assets/Prototype/Resources/BuildInfo.json", JsonUtility.ToJson(stamp, true));
            AssetDatabase.ImportAsset("Assets/Prototype/Resources/BuildInfo.json", ImportAssetOptions.ForceSynchronousImport);
            string path = Path.GetFullPath("build/StackLab/StackLab.exe");
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-output") path = Path.GetFullPath(args[i + 1]);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { ScenePath }, locationPathName = path,
                target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Build failed: " + report.summary.result);
            Debug.Log("BUILD_READY " + path);
        }

        static string Git(string arguments)
        {
            using (var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("git", arguments) {
                WorkingDirectory = Directory.GetCurrentDirectory(), UseShellExecute = false,
                RedirectStandardOutput = true, CreateNoWindow = true }))
            {
                string text = process.StandardOutput.ReadToEnd(); process.WaitForExit();
                if (process.ExitCode != 0) throw new InvalidOperationException("git " + arguments + " failed");
                return text;
            }
        }

        static void KeepMaterial(string name, string shaderName)
        {
            string path = "Assets/Prototype/Resources/" + name + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
            Shader shader = Shader.Find(shaderName);
            if (shader == null) throw new InvalidOperationException("Shader unavailable: " + shaderName);
            var material = new Material(shader) { enableInstancing = true };
            AssetDatabase.CreateAsset(material, path);
        }
    }
}
