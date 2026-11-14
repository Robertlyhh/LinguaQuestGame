using UnityEngine;

public enum ExperienceEndingMode
{
    Finish,
    Survey
}

[CreateAssetMenu(
    fileName = "Experience Ending Config",
    menuName = "LinguaQuest/Experience Ending Config")]
public sealed class ExperienceEndingConfig : ScriptableObject
{
    [Header("Behavior")]
    public ExperienceEndingMode mode = ExperienceEndingMode.Finish;
    [Tooltip("Required only in Survey mode. Only absolute HTTP and HTTPS URLs are accepted.")]
    public string surveyUrl;
    public string restartSceneName = "Hokkien_Tutorial_Scene";
    public string saveFileName = "hokkien_save.json";

    [Header("Content")]
    public string title = "Journey Complete!";
    [TextArea(2, 5)]
    public string message =
        "You've reached the end of the current Hokkien experience.\n\n" +
        "Thank you for exploring the night market and practicing Taiwanese Hokkien with us.";
    public string finishButtonLabel = "Finish";
    public string surveyButtonLabel = "Continue to Survey";
    public string restartButtonLabel = "Restart Experience";

    [Header("Completion States")]
    public string finishedTitle = "Thanks for Playing!";
    [TextArea(2, 4)]
    public string finishedMessage = "You may now close this page.";
    public string surveyOpenedTitle = "Thank You!";
    [TextArea(2, 4)]
    public string surveyOpenedMessage =
        "The survey has been opened in your browser. Thank you for participating.";
    [TextArea(2, 4)]
    public string invalidSurveyMessage =
        "The survey is not available right now. You may now close this page.";

    [Header("Restart Confirmation")]
    public string restartConfirmationTitle = "Restart the Experience?";
    [TextArea(2, 4)]
    public string restartConfirmationMessage =
        "All local game progress will be cleared. This cannot be undone.";
    public string restartConfirmLabel = "Restart";
    public string restartCancelLabel = "Cancel";
}
