using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for SpriteRenderer that integrates swatch-based color management with 2D-optimized property display.
/// 
/// Inherits from SwatchEditorBase to reuse all swatch functionality while adding SpriteRenderer-specific
/// property handling and behavior.
/// </summary>
[CustomEditor(typeof(SpriteRenderer))]
public class SpriteRendererSwatchEditor : SwatchEditorBase
{
    /// <summary>
    /// Draws the most commonly used SpriteRenderer properties above the swatch section.
    /// </summary>
    protected override void DrawComponentPropertiesAbove()
    {
        // Draw only the essential SpriteRenderer properties
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Sprite"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Color"));
    }

    /// <summary>
    /// Draws secondary SpriteRenderer properties below the swatch section, filtered for 2D relevance.
    /// </summary>
    protected override void DrawComponentPropertiesBelow()
    {
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FlipX"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FlipY"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_DrawMode"));

        // Show size property only if draw mode is not Simple
        var drawMode = serializedObject.FindProperty("m_DrawMode");
        if (drawMode.enumValueIndex != 0) // Not Simple mode
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Size"));
        }

        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Materials"));

        // Sorting properties - essential for 2D layering
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SortingLayerID"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("m_SortingOrder"));
    }

    /// <summary>
    /// Gets the SwatchColorReference component from the current SpriteRenderer target.
    /// </summary>
    protected override SwatchColorReference GetCurrentSwatchReference()
    {
        if (target != null)
        {
            SpriteRenderer sr = (SpriteRenderer)target;
            return sr.GetComponent<SwatchColorReference>();
        }
        return null;
    }

    /// <summary>
    /// Auto-assigns swatch 0 to new SpriteRenderers that don't have swatch references.
    /// </summary>
    protected override void AutoAssignDefaultSwatch()
    {
        // Only auto-assign if we have a valid color palette
        if (colorPalette == null || colorPalette.colors == null || colorPalette.colors.Length == 0)
            return;

        foreach (var t in targets)
        {
            SpriteRenderer spriteRenderer = (SpriteRenderer)t;

            if (spriteRenderer.TryGetComponent<SwatchColorReference>(out var existingRef))
            {
                // Check if this existing reference is in our list
                if (!SwatchRefs.Contains(existingRef))
                {
                    // This is a copied/duplicated object - add it to our list
                    SwatchRefs.Add(existingRef);
                    EditorUtility.SetDirty(existingRef);
                    EditorUtility.SetDirty(spriteRenderer);
                }
            }
            else
            {
                // No SwatchColorReference - create new one and assign swatch 0
                SwatchColorReference newSwatchRef = CreateSwatchColorReference(spriteRenderer);

                // Set to swatch 0 and apply the color
                newSwatchRef.SetSwatchIndexAndApplyColor(0);
                EditorUtility.SetDirty(newSwatchRef);
                EditorUtility.SetDirty(spriteRenderer);
            }
        }
    }

    /// <summary>
    /// Checks if the SpriteRenderer's color has been manually changed outside the swatch system.
    /// </summary>
    protected override bool HasColorChangedManually()
    {
        SpriteRenderer spriteRenderer = (SpriteRenderer)target;
        SwatchColorReference swatchRef = spriteRenderer.GetComponent<SwatchColorReference>();
        
        if (swatchRef == null || swatchRef.GetSwatchIndex() < 0)
            return false;

        Color swatchColor = swatchRef.ColorFromPalette();
        return spriteRenderer.color != swatchColor;
    }
}