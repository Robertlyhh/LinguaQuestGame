using System.IO;
using UnityEditor;
using UnityEngine;

public class SpriteRecolorer : EditorWindow
{
    Texture2D source;
    string outDir = "Assets/Art/Variants";

    // Source color selection
    enum ColorChoice { Green, Yellow, Orange, Red }
    ColorChoice sourceColor = ColorChoice.Green;

    // Target color toggles
    bool makeGreen = false, makeYellow = true, makeOrange = true, makeRed = true;

    float satMul = 1.00f;  // saturation multiplier
    float valMul = 1.00f;  // value/brightness multiplier
    float minAlphaToAffect = 1f / 255f; // don't touch fully transparent pixels

    [MenuItem("Tools/Art/Sprite Recolorer")]
    static void Open() => GetWindow<SpriteRecolorer>("Sprite Recolorer");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Sprite Recolorer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select a source color, then choose which target colors to generate.", MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Source Sprite", EditorStyles.boldLabel);
        source = (Texture2D)EditorGUILayout.ObjectField("Texture2D (PNG)", source, typeof(Texture2D), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Source Color", EditorStyles.boldLabel);
        sourceColor = (ColorChoice)EditorGUILayout.EnumPopup("Current Sprite Color", sourceColor);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Target Colors (Generate)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select which color variants to create", MessageType.None);

        makeGreen = EditorGUILayout.ToggleLeft("Green", makeGreen);
        makeYellow = EditorGUILayout.ToggleLeft("Yellow", makeYellow);
        makeOrange = EditorGUILayout.ToggleLeft("Orange", makeOrange);
        makeRed = EditorGUILayout.ToggleLeft("Red", makeRed);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
        outDir = EditorGUILayout.TextField("Output Folder", outDir);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Global Adjustments (optional)", EditorStyles.boldLabel);
        satMul = EditorGUILayout.Slider(new GUIContent("Saturation ×"), satMul, 0.5f, 1.5f);
        valMul = EditorGUILayout.Slider(new GUIContent("Value/Brightness ×"), valMul, 0.5f, 1.5f);
        minAlphaToAffect = EditorGUILayout.Slider(new GUIContent("Min Alpha To Affect"), minAlphaToAffect, 0f, 0.2f);

        EditorGUILayout.Space(10);
        using (new EditorGUI.DisabledScope(source == null))
        {
            if (GUILayout.Button("Generate Variants", GUILayout.Height(30)))
                Generate();
        }
    }

    void Generate()
    {
        if (source == null) return;

        string path = AssetDatabase.GetAssetPath(source);
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        if (ti == null)
        {
            Debug.LogError("Could not get TextureImporter for source.");
            return;
        }

        // Ensure readable, no compression so we get crisp pixels
        bool origReadable = ti.isReadable;
        var origCompression = ti.textureCompression;
        var origNPOT = ti.npotScale;

        ti.isReadable = true;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.npotScale = TextureImporterNPOTScale.None;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        // Make sure output folder exists
        if (!AssetDatabase.IsValidFolder(outDir))
        {
            var parts = outDir.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        // Read pixels
        var src = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        Graphics.CopyTexture(source, src);
        var pixels = src.GetPixels();

        // Generate selected variants
        if (makeGreen) SaveVariant(pixels, src.width, src.height, ColorChoice.Green, "green");
        if (makeYellow) SaveVariant(pixels, src.width, src.height, ColorChoice.Yellow, "yellow");
        if (makeOrange) SaveVariant(pixels, src.width, src.height, ColorChoice.Orange, "orange");
        if (makeRed) SaveVariant(pixels, src.width, src.height, ColorChoice.Red, "red");

        // Restore importer
        ti.isReadable = origReadable;
        ti.textureCompression = origCompression;
        ti.npotScale = origNPOT;
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        AssetDatabase.Refresh();
        Debug.Log($"Sprite recolor variants generated from {sourceColor} to selected targets.");
    }

    void SaveVariant(Color[] srcPixels, int w, int h, ColorChoice targetColor, string suffix)
    {
        var outTex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        outTex.filterMode = FilterMode.Point;

        // Calculate hue shift from source to target
        float hueShift = CalculateHueShift(sourceColor, targetColor);

        for (int i = 0; i < srcPixels.Length; i++)
        {
            Color c = srcPixels[i];
            if (c.a <= minAlphaToAffect)
            {
                outTex.SetPixel(i % w, i / w, c); // keep fully transparent untouched
                continue;
            }

            Color.RGBToHSV(c, out float H, out float S, out float V);

            // Only shift hues that match the source color range
            if (IsColorHue(H, sourceColor))
            {
                H = Mathf.Repeat(H + hueShift, 1f);
                S = Mathf.Clamp01(S * satMul);
                V = Mathf.Clamp01(V * valMul);
                var outC = Color.HSVToRGB(H, S, V);
                outC.a = c.a;
                outTex.SetPixel(i % w, i / w, outC);
            }
            else
            {
                // Non-matching pixels: keep original (prevents other colors from changing)
                outTex.SetPixel(i % w, i / w, c);
            }
        }

        outTex.Apply(false, false);
        var bytes = outTex.EncodeToPNG();

        string srcName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(source));
        string savePath = $"{outDir}/{srcName}_{suffix}.png";
        File.WriteAllBytes(savePath, bytes);
        AssetDatabase.ImportAsset(savePath, ImportAssetOptions.ForceUpdate);

        // Set sprite import settings (point filter, no mipmaps)
        var ti = (TextureImporter)AssetImporter.GetAtPath(savePath);
        ti.textureType = TextureImporterType.Sprite;
        ti.mipmapEnabled = false;
        ti.filterMode = FilterMode.Point;
        ti.spritePixelsPerUnit = Mathf.Max(16, Mathf.Max(source.width, source.height) / 3);
        AssetDatabase.WriteImportSettingsIfDirty(savePath);
        AssetDatabase.ImportAsset(savePath, ImportAssetOptions.ForceUpdate);
    }

    // Calculate hue shift in [0,1] range from source to target color
    float CalculateHueShift(ColorChoice from, ColorChoice to)
    {
        float fromHue = GetColorHue(from);
        float toHue = GetColorHue(to);

        float shift = toHue - fromHue;

        // Normalize to [-0.5, 0.5] range for shortest path
        if (shift > 0.5f) shift -= 1f;
        if (shift < -0.5f) shift += 1f;

        return shift;
    }

    // Get the center hue value for each color (in 0-1 range)
    float GetColorHue(ColorChoice color)
    {
        switch (color)
        {
            case ColorChoice.Red: return 0f / 360f;      // 0°
            case ColorChoice.Orange: return 30f / 360f;     // 30°
            case ColorChoice.Yellow: return 60f / 360f;     // 60°
            case ColorChoice.Green: return 120f / 360f;    // 120°
            default: return 0f;
        }
    }

    // Check if a hue belongs to a specific color range
    bool IsColorHue(float H01, ColorChoice color)
    {
        float deg = H01 * 360f;

        switch (color)
        {
            case ColorChoice.Green:
                // Green range: 90° to 150°
                return deg >= 90f && deg <= 150f;

            case ColorChoice.Yellow:
                // Yellow range: 45° to 75°
                return deg >= 45f && deg <= 75f;

            case ColorChoice.Orange:
                // Orange range: 15° to 45°
                return deg >= 15f && deg <= 45f;

            case ColorChoice.Red:
                // Red range: 345° to 15° (wraps around)
                return deg >= 345f || deg <= 15f;

            default:
                return false;
        }
    }
}