using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ExperienceCompleteValidator
{
    private const string ScenePath = "Assets/Scenes/ExperienceComplete.unity";
    private const string ConfigPath =
        "Assets/Hokkien/EndingConfigs/Ending Config.asset";
    private const string LevelFourDataPath =
        "Assets/Hokkien/LessonContent/Level 4 Data.asset";

    [MenuItem("LinguaQuest/Validate Experience Complete Scene")]
    public static void Validate()
    {
        AssetDatabase.Refresh();

        ExperienceEndingConfig endingConfig =
            AssetDatabase.LoadAssetAtPath<ExperienceEndingConfig>(ConfigPath);
        HokkienQuizLevelData levelFour =
            AssetDatabase.LoadAssetAtPath<HokkienQuizLevelData>(LevelFourDataPath);

        Require(endingConfig != null, "Ending Config is missing.");
        Require(levelFour != null && levelFour.successSceneName == "ExperienceComplete",
            "Level 4 does not navigate to ExperienceComplete.");
        Require(EditorBuildSettings.scenes.Any(
                scene => scene.enabled && scene.path == ScenePath),
            "ExperienceComplete is not enabled in Build Settings.");

        Require(ExperienceCompleteController.IsValidSurveyUrl("https://example.com/survey"),
            "HTTPS survey URLs should be accepted.");
        Require(ExperienceCompleteController.IsValidSurveyUrl("http://example.com/survey"),
            "HTTP survey URLs should be accepted.");
        Require(!ExperienceCompleteController.IsValidSurveyUrl("javascript:alert(1)"),
            "Non-HTTP survey URLs must be rejected.");
        Require(!ExperienceCompleteController.IsValidSurveyUrl("/relative-survey"),
            "Relative survey URLs must be rejected.");

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject[] roots = scene.GetRootGameObjects();
        GameObject world = roots.FirstOrDefault(root => root.name == "EndingWorld");
        ExperienceCompleteController controller =
            UnityEngine.Object.FindFirstObjectByType<ExperienceCompleteController>();

        Require(world != null, "EndingWorld is missing from the ending scene.");
        Require(controller != null, "ExperienceCompleteController is missing.");
        Require(controller.Config == endingConfig,
            "The ending scene is not using Ending Config.");
        Require(controller.DialogueBoxSprite != null,
            "The ending scene is missing the Hokkien menu dialogue-box sprite.");
        Require(UnityEngine.Object.FindObjectsByType<Camera>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 1,
            "The ending scene must contain exactly one camera.");

        MonoBehaviour[] gameplayBehaviours = world
            .GetComponentsInChildren<MonoBehaviour>(true)
            .Where(behaviour => behaviour != null && !IsRenderingComponent(behaviour))
            .ToArray();
        Require(gameplayBehaviours.Length == 0,
            "EndingWorld contains gameplay scripts: " +
            string.Join(", ", gameplayBehaviours.Select(item => item.GetType().Name).Distinct()));

        InvokeAwake(controller);
        Require(controller.transform.Find("ExperienceCompleteCanvas") != null,
            "The runtime ending canvas was not created.");
        Transform primaryButton = FindDescendant(controller.transform, "PrimaryButton");
        Transform restartButton = FindDescendant(controller.transform, "RestartButton");
        Transform restartConfirmation = FindDescendant(controller.transform, "RestartConfirmation");
        Require(primaryButton != null,
            "The primary ending action was not created.");
        Require(restartButton != null,
            "The restart action was not created.");
        Require(restartConfirmation != null,
            "The restart confirmation was not created.");
        Transform endingCard = FindDescendant(controller.transform, "EndingCard");
        Require(endingCard.GetComponent<UnityEngine.UI.Image>().sprite == controller.DialogueBoxSprite,
            "The ending card is not using the Hokkien minigame dialogue-box sprite.");
        Require(primaryButton.GetComponent<UnityEngine.UI.Image>().color ==
                new Color32(248, 220, 177, 255),
            "The primary action is not using the minigame panel button fill.");
        UnityEngine.UI.Outline primaryOutline =
            primaryButton.GetComponent<UnityEngine.UI.Outline>();
        Require(primaryOutline != null && primaryOutline.effectColor ==
                new Color32(177, 83, 43, 255),
            "The primary action is not using the minigame panel button outline.");
        string expectedPrimaryLabel = endingConfig.mode == ExperienceEndingMode.Survey
            ? endingConfig.surveyButtonLabel
            : endingConfig.finishButtonLabel;
        Require(primaryButton.GetComponentInChildren<TMPro.TMP_Text>().text == expectedPrimaryLabel,
            "The selected ending mode did not apply its primary button label.");

        InvokePrivate(controller, "ShowRestartConfirmation");
        Require(restartConfirmation.gameObject.activeSelf,
            "Restart confirmation did not open.");
        InvokePrivate(controller, "HideRestartConfirmation");
        Require(!restartConfirmation.gameObject.activeSelf,
            "Restart confirmation did not close.");

        if (endingConfig.mode == ExperienceEndingMode.Finish)
        {
            InvokePrivate(controller, "HandlePrimaryAction");
            Require(!primaryButton.gameObject.activeSelf && !restartButton.gameObject.activeSelf,
                "Finish did not enter a terminal state.");
            TMP_TextSnapshot snapshot = ReadCardText(
                FindDescendant(controller.transform, "EndingCard"));
            Require(snapshot.title == endingConfig.finishedTitle,
                "Finish did not apply the configured terminal title.");
            Require(snapshot.message == endingConfig.finishedMessage,
                "Finish did not apply the configured terminal message.");
        }

        ValidateSurveyFallback(endingConfig);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Debug.Log("[ExperienceCompleteValidator] All validation checks passed.");
    }

    public static void CapturePreview()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ExperienceCompleteController controller =
            UnityEngine.Object.FindFirstObjectByType<ExperienceCompleteController>();
        Camera camera = UnityEngine.Object.FindFirstObjectByType<Camera>();
        Require(controller != null && camera != null,
            "A controller and camera are required to capture the ending preview.");

        InvokeAwake(controller);
        Canvas canvas = controller.GetComponentInChildren<Canvas>(true);
        Require(canvas != null, "The runtime ending canvas was not created.");
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1f;

        RenderTexture renderTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
        Texture2D screenshot = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;

        try
        {
            camera.targetTexture = renderTexture;
            camera.Render();
            RenderTexture.active = renderTexture;
            screenshot.ReadPixels(new Rect(0f, 0f, 1920f, 1080f), 0, 0);
            screenshot.Apply();

            string outputPath = Path.GetFullPath("Logs/ExperienceCompletePreview.png");
            File.WriteAllBytes(outputPath, screenshot.EncodeToPNG());
            Debug.Log($"[ExperienceCompleteValidator] Preview written to {outputPath}");
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(screenshot);
            UnityEngine.Object.DestroyImmediate(renderTexture);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }
    }

    public static void BuildWebGLSmoke()
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
        Require(scenes.Contains(ScenePath),
            "ExperienceComplete must be enabled before the WebGL smoke build.");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = Path.GetFullPath("Temp/ExperienceCompleteWebGLSmoke"),
            target = BuildTarget.WebGL,
            options = BuildOptions.Development
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        Require(report.summary.result == BuildResult.Succeeded,
            $"WebGL smoke build failed with {report.summary.totalErrors} error(s).");
        Debug.Log(
            "[ExperienceCompleteValidator] WebGL smoke build passed. " +
            $"Scenes: {scenes.Length}, size: {report.summary.totalSize} bytes.");
    }

    private static void InvokeAwake(ExperienceCompleteController controller)
    {
        MethodInfo awake = typeof(ExperienceCompleteController).GetMethod(
            "Awake", BindingFlags.Instance | BindingFlags.NonPublic);
        Require(awake != null, "Could not locate ExperienceCompleteController.Awake.");

        try
        {
            awake.Invoke(controller, null);
        }
        catch (TargetInvocationException exception)
        {
            throw new InvalidOperationException(
                "The runtime ending view could not be constructed.",
                exception.InnerException ?? exception);
        }
    }

    private static void ValidateSurveyFallback(ExperienceEndingConfig sourceConfig)
    {
        if (sourceConfig == null)
        {
            sourceConfig = AssetDatabase.LoadAssetAtPath<ExperienceEndingConfig>(ConfigPath);
        }
        Require(sourceConfig != null, "Ending Config could not be reloaded.");
        ExperienceEndingConfig surveyConfig = UnityEngine.Object.Instantiate(sourceConfig);
        surveyConfig.mode = ExperienceEndingMode.Survey;
        surveyConfig.surveyUrl = string.Empty;
        surveyConfig.hideFlags = HideFlags.HideAndDontSave;

        GameObject surveyObject = new GameObject("SurveyEndingValidation");
        ExperienceCompleteController surveyController =
            surveyObject.AddComponent<ExperienceCompleteController>();
        FieldInfo configField = typeof(ExperienceCompleteController).GetField(
            "config", BindingFlags.Instance | BindingFlags.NonPublic);
        Require(configField != null, "Could not locate the ending Config field.");
        configField.SetValue(surveyController, surveyConfig);

        InvokeAwake(surveyController);
        Transform primaryButton = FindDescendant(surveyController.transform, "PrimaryButton");
        Require(primaryButton.GetComponentInChildren<TMPro.TMP_Text>().text == "Continue to Survey",
            "Survey mode did not apply the expected primary button label.");

        InvokePrivate(surveyController, "HandlePrimaryAction");
        TMP_TextSnapshot snapshot = ReadCardText(
            FindDescendant(surveyController.transform, "EndingCard"));
        Require(snapshot.message == surveyConfig.invalidSurveyMessage,
            "An empty Survey URL did not fall back to the safe terminal state.");
        Require(!primaryButton.gameObject.activeSelf,
            "The invalid Survey action remained interactable.");
        UnityEngine.Object.DestroyImmediate(surveyConfig);
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Require(method != null, $"Could not locate {target.GetType().Name}.{methodName}.");
        method.Invoke(target, null);
    }

    private static TMP_TextSnapshot ReadCardText(Transform card)
    {
        Require(card != null, "The ending card is missing.");
        TMPro.TMP_Text[] texts = card.GetComponentsInChildren<TMPro.TMP_Text>(true);
        TMPro.TMP_Text title = texts.FirstOrDefault(text => text.name == "Title");
        TMPro.TMP_Text message = texts.FirstOrDefault(text => text.name == "Message");
        Require(title != null && message != null, "The ending card text is incomplete.");
        return new TMP_TextSnapshot(title.text, message.text);
    }

    private readonly struct TMP_TextSnapshot
    {
        public readonly string title;
        public readonly string message;

        public TMP_TextSnapshot(string title, string message)
        {
            this.title = title;
            this.message = message;
        }
    }

    private static bool IsRenderingComponent(MonoBehaviour behaviour)
    {
        string componentNamespace = behaviour.GetType().Namespace ?? string.Empty;
        return componentNamespace.StartsWith("UnityEngine.Rendering")
               || componentNamespace.StartsWith("UnityEngine.U2D");
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        return root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(item => item.name == objectName);
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException("[ExperienceCompleteValidator] " + message);
        }
    }
}
