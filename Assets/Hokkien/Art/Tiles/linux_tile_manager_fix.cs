using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using System.Reflection;

/*
Workaround for unity bug where in tiling window managers new window can't be 
docked

https://discussions.unity.com/t/unable-to-dock-undock-unity-windows-with-tiling-window-managers/797485
*/
public static class DockedPaletteOpener
{
    [MenuItem("Tools/Open Docked Tile Palette %#t")]
    public static void OpenDockedPalette()
    {
        // Target the Inspector dock area
        var inspectorType = typeof(Editor).Assembly
            .GetType("UnityEditor.InspectorWindow");
        var inspector = EditorWindow.GetWindow(inspectorType);

        var dockAreaField = typeof(EditorWindow).GetField("m_Parent",
            BindingFlags.NonPublic | BindingFlags.Instance);

        var dockArea = dockAreaField?.GetValue(inspector);
        if (dockArea == null)
        {
            Debug.LogError("Could not find Inspector dock area"); return;
        }

        // Add Tile pallet editor to inspector tab
        var palette = ScriptableObject.CreateInstance<GridPaintPaletteWindow>();

        var addTabMethod = dockArea.GetType().GetMethod("AddTab",
            BindingFlags.Public | BindingFlags.Instance,
            null,
            new System.Type[] { typeof(EditorWindow), typeof(bool) },
            null);

        if (addTabMethod != null)
        {
            addTabMethod.Invoke(dockArea, new object[] { palette, true });
            palette.Show();
        }
        else
        {
            Debug.LogError("AddTab not found");
            palette.Show();
        }
    }
}