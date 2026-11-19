using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ExperienceCompleteSceneBuilder
{
    private const string SourceScenePath = "Assets/Scenes/Hokkien4.unity";
    private const string OutputScenePath = "Assets/Scenes/ExperienceComplete.unity";
    private const string ConfigFolder = "Assets/Hokkien/EndingConfigs";
    private const string ConfigPath = ConfigFolder + "/Ending Config.asset";
    private const string DialogueBoxPath = "Assets/Hokkien/Art/Menu/DialogueBoxSimple.png";

    [MenuItem("LinguaQuest/Build Experience Complete Scene")]
    public static void Build()
    {
        EnsureConfigFolder();
        ExperienceEndingConfig endingConfig = CreateOrUpdateConfig();

        Scene sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
        GameObject sourceWorld = sourceScene.GetRootGameObjects()
            .FirstOrDefault(root => root.name == "World");
        Camera sourceCamera = Object.FindFirstObjectByType<Camera>();

        if (sourceWorld == null)
        {
            throw new MissingReferenceException(
                $"The source scene '{SourceScenePath}' does not contain a World root.");
        }

        Scene endingScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        endingScene.name = "ExperienceComplete";

        bool sourceWorldWasActive = sourceWorld.activeSelf;
        sourceWorld.SetActive(false);
        GameObject world = Object.Instantiate(sourceWorld);
        sourceWorld.SetActive(sourceWorldWasActive);
        world.name = "EndingWorld";
        SceneManager.MoveGameObjectToScene(world, endingScene);
        RemoveGameplayBehavior(world);

        Camera endingCamera = world.GetComponentInChildren<Camera>(true);
        if (endingCamera == null && sourceCamera != null)
        {
            GameObject cameraObject = Object.Instantiate(sourceCamera.gameObject);
            cameraObject.name = "Main Camera";
            SceneManager.MoveGameObjectToScene(cameraObject, endingScene);
            endingCamera = cameraObject.GetComponent<Camera>();
            RemoveGameplayBehavior(cameraObject);
        }

        if (endingCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject, endingScene);
            endingCamera = cameraObject.GetComponent<Camera>();
            endingCamera.orthographic = true;
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        }

        endingCamera.tag = "MainCamera";
        endingCamera.enabled = true;

        GameObject controllerObject = new GameObject("ExperienceCompleteController");
        SceneManager.MoveGameObjectToScene(controllerObject, endingScene);
        ExperienceCompleteController controller =
            controllerObject.AddComponent<ExperienceCompleteController>();
        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("config").objectReferenceValue = endingConfig;
        serializedController.FindProperty("dialogueBoxSprite").objectReferenceValue =
            LoadSprite(DialogueBoxPath);
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.CloseScene(sourceScene, true);
        world.SetActive(sourceWorldWasActive);
        SceneManager.SetActiveScene(endingScene);
        EditorSceneManager.SaveScene(endingScene, OutputScenePath);
        AddSceneToBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ExperienceCompleteSceneBuilder] Built {OutputScenePath}");
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        if (sprite == null)
        {
            throw new MissingReferenceException($"Could not load UI sprite at '{path}'.");
        }

        return sprite;
    }

    private static void RemoveGameplayBehavior(GameObject root)
    {
        foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
        {
            Object.DestroyImmediate(canvas.gameObject);
        }

        foreach (AudioSource audioSource in root.GetComponentsInChildren<AudioSource>(true))
        {
            audioSource.enabled = false;
        }

        foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null || IsRenderingComponent(behaviour))
            {
                continue;
            }

            Object.DestroyImmediate(behaviour);
        }
    }

    private static bool IsRenderingComponent(MonoBehaviour behaviour)
    {
        string componentNamespace = behaviour.GetType().Namespace ?? string.Empty;
        return componentNamespace.StartsWith("UnityEngine.Rendering")
               || componentNamespace.StartsWith("UnityEngine.U2D");
    }

    private static void EnsureConfigFolder()
    {
        if (!AssetDatabase.IsValidFolder(ConfigFolder))
        {
            AssetDatabase.CreateFolder("Assets/Hokkien", "EndingConfigs");
        }
    }

    private static ExperienceEndingConfig CreateOrUpdateConfig()
    {
        ExperienceEndingConfig config = LoadOrCreateConfig(ConfigPath, out bool created);
        if (created)
        {
            ApplySharedDefaults(config);
            config.mode = ExperienceEndingMode.Finish;
            config.surveyUrl = string.Empty;
        }
        EditorUtility.SetDirty(config);
        return config;
    }

    private static ExperienceEndingConfig LoadOrCreateConfig(string path, out bool created)
    {
        ExperienceEndingConfig config = AssetDatabase.LoadAssetAtPath<ExperienceEndingConfig>(path);
        if (config != null)
        {
            created = false;
            return config;
        }

        config = ScriptableObject.CreateInstance<ExperienceEndingConfig>();
        AssetDatabase.CreateAsset(config, path);
        created = true;
        return config;
    }

    private static void ApplySharedDefaults(ExperienceEndingConfig config)
    {
        config.restartSceneName = "Hokkien_Tutorial_Scene";
        config.saveFileName = HokkienLocalProgress.DefaultSaveFileName;
        config.title = "Journey Complete!";
        config.message =
            "You've reached the end of the current Hokkien experience.\n\n" +
            "Thank you for exploring the night market and practicing Taiwanese Hokkien with us.";
        config.finishButtonLabel = "Finish";
        config.surveyButtonLabel = "Continue to Survey";
        config.restartButtonLabel = "Restart Experience";
        config.finishedTitle = "Thanks for Playing!";
        config.finishedMessage = "You may now close this page.";
        config.surveyOpenedTitle = "Thank You!";
        config.surveyOpenedMessage =
            "The survey has been opened in your browser. Thank you for participating.";
        config.invalidSurveyMessage =
            "The survey is not available right now. You may now close this page.";
        config.restartConfirmationTitle = "Restart the Experience?";
        config.restartConfirmationMessage =
            "All local game progress will be cleared. This cannot be undone.";
        config.restartConfirmLabel = "Restart";
        config.restartCancelLabel = "Cancel";
    }

    private static void AddSceneToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(scene => scene.path == OutputScenePath))
        {
            return;
        }

        int insertionIndex = scenes.FindIndex(scene => scene.path.EndsWith("/Hokkien4.unity"));
        insertionIndex = insertionIndex >= 0 ? insertionIndex + 1 : scenes.Count;
        scenes.Insert(insertionIndex, new EditorBuildSettingsScene(OutputScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
