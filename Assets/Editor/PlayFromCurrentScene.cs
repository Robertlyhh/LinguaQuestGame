using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class PlayFromCurrentScene
{
    static PlayFromCurrentScene()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode) return;

        if (state == PlayModeStateChange.ExitingEditMode)
        {
            SceneAsset currentScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(
                EditorSceneManager.GetActiveScene().path
            );

            if (currentScene != null)
            {
                EditorSceneManager.playModeStartScene = currentScene;
                UnityEngine.Debug.Log("Play mode start scene set to: " + currentScene.name);
            }
        }
    }
}