using UnityEngine;

// One asset (Resources/BackendConfig) picks the backend for every Hokkien scene.
[CreateAssetMenu(menuName = "Hokkien/Backend Config")]
public class BackendConfig : ScriptableObject
{
    public enum BackendTarget { Render, Localhost }

    private const string DefaultRenderUrl = "https://nightmarket-1.onrender.com";

    public BackendTarget target = BackendTarget.Render;
    public string renderUrl = DefaultRenderUrl;
    public string localhostUrl = "http://localhost:8000";

    private static BackendConfig instance;

    public string BaseUrl => (target == BackendTarget.Localhost ? localhostUrl : renderUrl).TrimEnd('/');

    public static string CurrentBaseUrl
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<BackendConfig>("BackendConfig");

            return instance != null ? instance.BaseUrl : DefaultRenderUrl;
        }
    }
}
