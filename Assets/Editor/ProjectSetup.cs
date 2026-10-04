using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// One-shot project setup, kept as code so the project can be rebuilt from scripts.
// Safe to run again: it only creates what is missing.
public static class ProjectSetup
{
    const string UrpPath = "Assets/Settings/URP_Asset.asset";
    const string ScenePath = "Assets/Scenes/Bootstrap.unity";
    const string TuningPath = "Assets/Resources/Tuning.asset";

    [MenuItem("RRP/Setup Project")]
    public static void Run()
    {
        Directory.CreateDirectory("Assets/Settings");
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Resources");
        AssetDatabase.Refresh();

        SetupUrp();
        SetupPlayerSettings();
        SetupScene();
        SetupTuning();

        AssetDatabase.SaveAssets();
        Debug.Log("RRP setup done.");
    }

    // The build to hand to other players. Builds/ is ignored by git.
    [MenuItem("RRP/Build Windows Player")]
    public static void Build()
    {
        var options = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = "Builds/rrp_game/rrp_game.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(options);
        Debug.Log("RRP build: " + report.summary.result + ", " + report.summary.totalErrors + " errors, "
            + (report.summary.totalSize / (1024 * 1024)) + " MB at " + options.locationPathName);
    }

    static void SetupUrp()
    {
        var urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(UrpPath);
        if (urp == null)
        {
            // URP only exposes renderer creation internally.
            var create = typeof(UniversalRenderPipelineAsset).GetMethod("CreateRendererAsset",
                BindingFlags.Static | BindingFlags.NonPublic);
            var renderer = (ScriptableRendererData)create.Invoke(null,
                new object[] { UrpPath, RendererType.UniversalRenderer, true, "Renderer" });
            urp = UniversalRenderPipelineAsset.Create(renderer);
            AssetDatabase.CreateAsset(urp, UrpPath);
        }

        GraphicsSettings.defaultRenderPipeline = urp;
        int current = QualitySettings.GetQualityLevel();
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = urp;
        }
        QualitySettings.SetQualityLevel(current, false);
    }

    static void SetupPlayerSettings()
    {
        // Several instances run side by side when testing multiplayer on one machine.
        PlayerSettings.runInBackground = true;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.resizableWindow = true;

        // 1 = new Input System only. Takes effect after an editor restart.
        var settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0];
        var so = new SerializedObject(settings);
        var handler = so.FindProperty("activeInputHandler");
        if (handler != null && handler.intValue != 1)
        {
            handler.intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static void SetupScene()
    {
        if (!File.Exists(ScenePath))
        {
            // Empty on purpose: Game creates everything at runtime.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
        }
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }

    static void SetupTuning()
    {
        if (AssetDatabase.LoadAssetAtPath<Tuning>(TuningPath) == null)
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<Tuning>(), TuningPath);
    }
}
