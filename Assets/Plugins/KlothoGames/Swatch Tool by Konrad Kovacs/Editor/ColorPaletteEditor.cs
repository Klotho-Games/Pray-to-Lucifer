using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for ColorPalette ScriptableObjects that ensures automatic swatch synchronization.
/// </summary>
[CustomEditor(typeof(ColorPalette))]
public class ColorPaletteEditor : Editor
{
    /// <summary>
    /// Renders the default ColorPalette inspector with automatic swatch synchronization.
    /// </summary>
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        // Check if any properties were changed
        if (GUI.changed)
        {
            SpriteRendererWithSwatchesEditor.UpdateAllSwatchReferences();
        }
    }
}