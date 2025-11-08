// Assets/Editor/AIAssetFactory.cs
// Unity 2021+ (Editor only). Creates sprites/sheets from an image API or from a local PNG,
// slices a fixed grid, and builds a simple animation clip.
//
// What's new vs your previous version:
// - FIX: Removed reflection-based WrapperObj (was causing NullReferenceException)
// - Uses proper DTOs for request/response JSON
// - API key fallback: Editor field -> EditorPrefs -> OPENAI_API_KEY env var
// - Safe API size (256x256) + optional nearest-neighbor resample to your requested size
// - Same tabs: Generate via API, Process Existing PNG, Batch (JSON)

using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using UnityEditor.U2D.Sprites;

public class AIAssetFactory : EditorWindow
{
    // ---------- Pref keys ----------
    const string PREFS_OPENAI_KEY = "AIAssetFactory.OpenAIKey";
    const string PREFS_MODEL = "AIAssetFactory.Model";        // e.g., gpt-image-1
    const string PREFS_DEFAULT_DIR = "AIAssetFactory.DefaultDir";   // e.g., Assets/AI

    string openAIKey = "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";
    string model = "dall-e-3";
    string defaultDir = "Assets/AI";
    int tab = 0;

    // Single generation (API)
    string prompt = "Zelda-like top-down pixel art, 48x48 player sprite sheet, 4-direction walk cycle (down/left/right/up), transparent background, pixel-perfect, no gradients, soft outlines";
    int width = 192;   // final output width you want in Project
    int height = 192;   // final output height you want in Project
    int frameW = 48;
    int frameH = 48;
    string outFilename = "Player.png";
    bool forceResampleFromSafeSize = true; // if true, requests 256x256 from API then NN-resamples to width x height

    // Process existing PNG
    Texture2D inputPNG;
    int existingFrameW = 48, existingFrameH = 48;
    string processedFilename = "Processed.png";

    // Batch (JSON)
    TextAsset batchJson;

    [MenuItem("Tools/AI/Asset Factory")]
    public static void ShowWindow() => GetWindow<AIAssetFactory>("AI Asset Factory");

    void OnEnable()
    {
        openAIKey = EditorPrefs.GetString(PREFS_OPENAI_KEY, openAIKey);
        model = EditorPrefs.GetString(PREFS_MODEL, model);
        defaultDir = EditorPrefs.GetString(PREFS_DEFAULT_DIR, defaultDir);
    }

    void OnGUI()
    {
        DrawHeader();

        tab = GUILayout.Toolbar(tab, new[] { "Generate via API", "Process Existing PNG", "Batch (JSON)" });
        GUILayout.Space(6);

        switch (tab)
        {
            case 0: DrawApiTab(); break;
            case 1: DrawProcessTab(); break;
            case 2: DrawBatchTab(); break;
        }
    }

    void DrawHeader()
    {
        EditorGUILayout.LabelField("AI Asset Factory (Unity-Only)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Generate Zelda-like pixel sprites/sheets or process your own PNG. Imports as Sprite(s), slices grid, creates a simple AnimationClip.", MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("General Settings", EditorStyles.boldLabel);
        defaultDir = EditorGUILayout.TextField(new GUIContent("Save Folder (under Assets)"), defaultDir);
        model = EditorGUILayout.TextField(new GUIContent("Image Model (API)"), model);
        openAIKey = EditorGUILayout.PasswordField(new GUIContent("OpenAI API Key"), openAIKey);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Save Settings"))
            {
                EditorPrefs.SetString(PREFS_OPENAI_KEY, openAIKey);
                EditorPrefs.SetString(PREFS_MODEL, model);
                EditorPrefs.SetString(PREFS_DEFAULT_DIR, defaultDir);
                ShowNotification(new GUIContent("Settings saved"));
            }
            if (GUILayout.Button("Open Save Folder"))
            {
                var abs = Path.Combine(Directory.GetCurrentDirectory(), defaultDir);
                EditorUtility.RevealInFinder(abs);
            }
        }
        EditorGUILayout.Space();
    }

    void DrawApiTab()
    {
        EditorGUILayout.LabelField("Generate via Image API", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Requires OpenAI Images API (gpt-image-1). If you'd rather avoid API calls, use the 'Process Existing PNG' tab.", MessageType.None);

        EditorGUILayout.LabelField("Style Lock Tips", EditorStyles.miniBoldLabel);
        EditorGUILayout.HelpBox(
            "Pixel art, Zelda-like SNES top-down (3/4), 48x48 frames, limited palette, soft outlines, transparent background, no anti-aliasing.",
            MessageType.None);

        prompt = EditorGUILayout.TextArea(prompt, GUILayout.MinHeight(60));
        width = EditorGUILayout.IntField("Output Width (final)", width);
        height = EditorGUILayout.IntField("Output Height (final)", height);
        frameW = EditorGUILayout.IntField("Frame Width", frameW);
        frameH = EditorGUILayout.IntField("Frame Height", frameH);
        outFilename = EditorGUILayout.TextField("File Name", outFilename);
        forceResampleFromSafeSize = EditorGUILayout.Toggle(new GUIContent("Request 256x256 then resample NN to target"), forceResampleFromSafeSize);

        if (GUILayout.Button("Generate → Import → Slice → Anim"))
        {
            _ = GenerateAndImport();
        }
    }

    void DrawProcessTab()
    {
        EditorGUILayout.LabelField("Process Existing PNG", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Drag in a PNG (e.g., exported from ChatGPT Images). This will import as Sprite(s), slice by grid, and create a simple Idle animation.", MessageType.Info);

        inputPNG = (Texture2D)EditorGUILayout.ObjectField("Input PNG (Texture2D)", inputPNG, typeof(Texture2D), false);
        existingFrameW = EditorGUILayout.IntField("Frame Width", existingFrameW);
        existingFrameH = EditorGUILayout.IntField("Frame Height", existingFrameH);
        processedFilename = EditorGUILayout.TextField("Output File Name", processedFilename);

        if (GUILayout.Button("Import/Slice/Animate Existing PNG"))
        {
            if (inputPNG == null)
            {
                ShowNotification(new GUIContent("Please assign a PNG Texture2D."));
                return;
            }
            ProcessExistingPNG();
        }
    }

    void DrawBatchTab()
    {
        EditorGUILayout.LabelField("Batch (JSON)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("JSON format:\n[\n  {\"name\":\"Beaver\",\"prompt\":\"...\",\"w\":192,\"h\":192,\"fw\":48,\"fh\":48,\"file\":\"Beaver.png\"}\n]", MessageType.None);
        batchJson = (TextAsset)EditorGUILayout.ObjectField("Batch JSON", batchJson, typeof(TextAsset), false);

        if (GUILayout.Button("Run Batch"))
        {
            if (batchJson == null)
            {
                ShowNotification(new GUIContent("Assign a JSON TextAsset."));
                return;
            }
            _ = RunBatch(batchJson.text);
        }
    }

    // ================= Core ops =================

    async Task GenerateAndImport()
    {
        try
        {
            EnsureFolder(defaultDir);
            string savePath = Path.Combine(defaultDir, outFilename).Replace("\\", "/");

            // 1) Generate PNG bytes via API
            byte[] png = await GenerateImageWithOpenAI(prompt, width, height, forceResampleFromSafeSize);
            File.WriteAllBytes(savePath, png);
            AssetDatabase.ImportAsset(savePath, ImportAssetOptions.ForceUpdate);

            // 2) Import settings + slice
            ConfigureAndSlice(savePath, frameW, frameH);

            // 3) Create simple Idle animation from all frames
            CreateSimpleAnim(savePath, "Idle", 0.12f);

            EditorUtility.RevealInFinder(Path.Combine(Directory.GetCurrentDirectory(), savePath));
            ShowNotification(new GUIContent("Done"));
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            ShowNotification(new GUIContent("Error (see Console)"));
        }
    }

    void ProcessExistingPNG()
    {
        try
        {
            EnsureFolder(defaultDir);
            var srcPath = AssetDatabase.GetAssetPath(inputPNG);
            var dstPath = Path.Combine(defaultDir, processedFilename).Replace("\\", "/");
            File.Copy(srcPath, dstPath, true);
            AssetDatabase.ImportAsset(dstPath, ImportAssetOptions.ForceUpdate);

            ConfigureAndSlice(dstPath, existingFrameW, existingFrameH);
            CreateSimpleAnim(dstPath, "Idle", 0.12f);

            EditorUtility.RevealInFinder(Path.Combine(Directory.GetCurrentDirectory(), dstPath));
            ShowNotification(new GUIContent("Processed"));
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            ShowNotification(new GUIContent("Error (see Console)"));
        }
    }

    async Task RunBatch(string json)
    {
        try
        {
            var items = JsonUtility.FromJson<Wrapper<BatchItem>>(WrapJson(json)).Items;
            EnsureFolder(defaultDir);

            foreach (var it in items)
            {
                string savePath = Path.Combine(defaultDir, it.file).Replace("\\", "/");
                byte[] png = await GenerateImageWithOpenAI(it.prompt, it.w, it.h, forceResampleFromSafeSize);
                File.WriteAllBytes(savePath, png);
                AssetDatabase.ImportAsset(savePath, ImportAssetOptions.ForceUpdate);

                ConfigureAndSlice(savePath, it.fw, it.fh);
                CreateSimpleAnim(savePath, "Idle", 0.12f);
                Debug.Log($"Generated: {it.name} -> {savePath}");
            }
            ShowNotification(new GUIContent("Batch complete"));
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            ShowNotification(new GUIContent("Batch failed (see Console)"));
        }
    }

    // ================= Helpers =================

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            var parts = path.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }

    static void ConfigureAndSlice(string assetPath, int fw, int fh)
    {
        var ti = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        ti.textureType = TextureImporterType.Sprite;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.filterMode = FilterMode.Point;
        ti.spritePixelsPerUnit = Mathf.Max(fw, fh); // 1 unit ≈ one frame edge

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (tex == null)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }
        if (tex != null && fw > 0 && fh > 0 && tex.width % fw == 0 && tex.height % fh == 0)
        {
            ti.spriteImportMode = SpriteImportMode.Multiple;
            int cols = tex.width / fw;
            int rows = tex.height / fh;

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            var dataProvider = factory.GetSpriteEditorDataProviderFromObject(ti);
            dataProvider.InitSpriteEditorDataProvider();

            var sprites = new List<SpriteRect>();
            int idx = 0;
            for (int y = rows - 1; y >= 0; y--)
                for (int x = 0; x < cols; x++)
                {
                    sprites.Add(new SpriteRect
                    {
                        name = $"frame_{idx++}",
                        rect = new Rect(x * fw, y * fh, fw, fh),
                        alignment = SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f)
                    });
                }
            dataProvider.SetSpriteRects(sprites.ToArray());
            dataProvider.Apply();
        }
        else
        {
            ti.spriteImportMode = SpriteImportMode.Single;
        }

        AssetDatabase.WriteImportSettingsIfDirty(assetPath);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
    }

    static void CreateSimpleAnim(string texturePath, string clipName, float frameTime)
    {
        var sprites = AssetDatabase.LoadAllAssetsAtPath(texturePath)
            .OfType<Sprite>().OrderBy(s => s.name).ToArray();
        if (sprites.Length == 0) return;

        var clip = new AnimationClip
        {
            frameRate = 1.0f / frameTime
        };

        var binding = new EditorCurveBinding { type = typeof(SpriteRenderer), path = "", propertyName = "m_Sprite" };

        var keys = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe { time = i * frameTime, value = sprites[i] };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        var animPath = Path.ChangeExtension(texturePath, null) + $"_{clipName}.anim";
        AssetDatabase.CreateAsset(clip, animPath);
    }

    // ================= Image API =================

    // DTOs for request/response (no reflection)
    [Serializable]
    class ImageRequest
    {
        public string model;
        public string prompt;
        public string size; // API accepts only certain sizes (e.g., 256x256, 512x512, 1024x1024)
        public int n;
    }
    [Serializable] class ImageResp { public ImgDatum[] data; }
    [Serializable] class ImgDatum { public string b64_json; }

    static readonly HttpClient http = new HttpClient();

    async Task<byte[]> GenerateImageWithOpenAI(string userPrompt, int targetW, int targetH, bool requestSafeThenResample)
    {
        // API key fallback: Editor field -> EditorPrefs -> env var
        if (string.IsNullOrWhiteSpace(openAIKey))
            openAIKey = EditorPrefs.GetString(PREFS_OPENAI_KEY, openAIKey);
        if (string.IsNullOrWhiteSpace(openAIKey))
            openAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrWhiteSpace(openAIKey))
            throw new Exception("OpenAI API key not set. Either:\n- Enter it at the top of the window and click Save Settings, or\n- Set OPENAI_API_KEY as an environment variable.");

        string apiSize = requestSafeThenResample ? "256x256" : $"{targetW}x{targetH}";

        var req = new ImageRequest
        {
            model = string.IsNullOrWhiteSpace(model) ? "dall-e-3" : model,
            prompt = "Pixel art, Zelda-like SNES top-down, 3/4 perspective, limited palette, soft outlines, transparent background, no anti-aliasing. " + userPrompt,
            size = apiSize,
            n = 1
        };

        string json = JsonUtility.ToJson(req);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        http.DefaultRequestHeaders.Clear();
        http.DefaultRequestHeaders.Add("Authorization", "Bearer " + openAIKey);

        var resp = await http.PostAsync("https://api.openai.com/v1/images/generations", content);
        var text = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new Exception("OpenAI image error: " + text);

        var parsed = JsonUtility.FromJson<ImageResp>(text);
        if (parsed == null || parsed.data == null || parsed.data.Length == 0 || string.IsNullOrEmpty(parsed.data[0].b64_json))
            throw new Exception("OpenAI image response parse error.");

        var pngBytes = Convert.FromBase64String(parsed.data[0].b64_json);

        if (requestSafeThenResample)
        {
            pngBytes = ResampleNearest(pngBytes, targetW, targetH);
        }

        return pngBytes;
    }

    // Nearest-neighbor resampler to preserve crisp pixels
    static byte[] ResampleNearest(byte[] pngBytes, int targetW, int targetH)
    {
        var src = new Texture2D(2, 2, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point
        };
        src.LoadImage(pngBytes);

        var dst = new Texture2D(targetW, targetH, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point
        };

        int sw = src.width, sh = src.height;
        for (int y = 0; y < targetH; y++)
        {
            int sy = Mathf.FloorToInt(y / (float)targetH * sh);
            if (sy >= sh) sy = sh - 1;
            for (int x = 0; x < targetW; x++)
            {
                int sx = Mathf.FloorToInt(x / (float)targetW * sw);
                if (sx >= sw) sx = sw - 1;
                var c = src.GetPixel(sx, sy);
                dst.SetPixel(x, y, c);
            }
        }
        dst.Apply(false, false);
        return dst.EncodeToPNG();
    }

    // ================= Batch JSON helpers =================

    [Serializable]
    class BatchItem
    {
        public string name;
        public string prompt;
        public int w, h, fw, fh;
        public string file;
    }

    [Serializable] class Wrapper<T> { public T[] Items; }
    static string WrapJson(string json) => "{\"Items\":" + json + "}";
}