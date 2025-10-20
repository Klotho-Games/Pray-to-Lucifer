using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Base class for custom editors that integrate swatch-based color management.
/// 
/// The base class handles:
/// - Swatch UI rendering and interaction
/// - Color palette management
/// - SwatchColorReference component integration
/// - Undo/redo system integration
/// </summary>
public abstract class SwatchEditorBase : Editor
{
    // UI Configuration - These values balance visual clarity with inspector space efficiency
    protected const int boxSize = 18;  // Swatch button size - large enough to see colors clearly
    protected const int highlightPadding = 2;  // Border space around selected swatch for visual feedback
    
    /// <summary>
    /// Performance optimization flag. When enabled, avoids costly FindObjectsByType calls on every inspector refresh.
    /// </summary>
    protected readonly bool enableManualSwatchUpdate = true;

    /// <summary>
    /// Static list maintaining all SwatchColorReference components in the scene.
    /// </summary>
    public static List<SwatchColorReference> SwatchRefs = new();
    
    /// <summary>
    /// Cached reference to the active ColorPalette to avoid repeated Resources.Load calls.
    /// </summary>
    protected ColorPalette colorPalette;
    
    /// <summary>
    /// Controls foldout state of the swatch section in the inspector.
    /// </summary>
    protected bool showSwatches = true;

    /// <summary>
    /// Subscribe to Unity's undo system when the editor becomes active.
    /// </summary>
    protected virtual void OnEnable()
    {
        Undo.undoRedoPerformed += UpdateAllSwatchReferences;
    }

    /// <summary>
    /// Unsubscribe from undo system when editor is disabled to prevent memory leaks.
    /// </summary>
    protected virtual void OnDisable()
    {
        Undo.undoRedoPerformed -= UpdateAllSwatchReferences;
    }

    /// <summary>
    /// Abstract method for drawing component-specific properties above the swatch section.
    /// Implement this in derived classes to show the most important properties first.
    /// </summary>
    protected abstract void DrawComponentPropertiesAbove();

    /// <summary>
    /// Abstract method for drawing component-specific properties below the swatch section.
    /// Implement this in derived classes to show secondary properties after swatches.
    /// </summary>
    protected abstract void DrawComponentPropertiesBelow();

    /// <summary>
    /// Abstract method to get the SwatchColorReference component from the current target.
    /// Implement this in derived classes to specify which component should have the swatch reference.
    /// </summary>
    protected abstract SwatchColorReference GetCurrentSwatchReference();

    /// <summary>
    /// Abstract method to handle auto-assignment of default swatch to new components.
    /// Implement this in derived classes to specify how new components should get swatch references.
    /// </summary>
    protected abstract void AutoAssignDefaultSwatch();

    /// <summary>
    /// Abstract method to check if the component's color has been manually changed outside the swatch system.
    /// Implement this in derived classes to maintain data integrity between manual and swatch-based color changes.
    /// </summary>
    protected abstract bool HasColorChangedManually();

    /// <summary>
    /// Main inspector rendering method that orchestrates the swatch-integrated interface.
    /// </summary>
    public override void OnInspectorGUI()
    {
        // Update the serialized object
        serializedObject.Update();

        // Load ColorPalette if not already loaded
        if (colorPalette == null) LoadColorPalette();

        // Only do editor-specific updates when NOT in Play mode
        // if (!EditorApplication.isPlaying)
        // {
            // Update all swatch references if manual swatch update is enabled
            ManualSwatchUpdate();

            // Auto-assign swatch 0 to new components
            AutoAssignDefaultSwatch();
        // }

        // Draw component properties above swatches
        DrawComponentPropertiesAbove();

        // Draw swatches section
        DrawSwatchesSection();

        // Draw component properties below swatches
        DrawComponentPropertiesBelow();

        // Apply property modifications
        serializedObject.ApplyModifiedProperties();

        // Only handle manual color changes when NOT in Play mode
        // In Play mode, SwatchColorReference.Update() handles this
        if (GUI.changed && HasColorChangedManually() /* && !EditorApplication.isPlaying */)
        {
            ClearSwatchReferences();
        }
    }

    /// <summary>
    /// Draws the swatch selection interface with all user interaction handling.
    /// </summary>
    protected void DrawSwatchesSection()
    {
        if (colorPalette == null || colorPalette.colors == null || colorPalette.colors.Length == 0) return;

        SwatchColorReference swatchRef = GetCurrentSwatchReference();

        showSwatches = EditorGUILayout.Foldout(showSwatches, "Swatches", true);

        if (showSwatches)
        {
            DrawCurrentSwatchInfoAndClearButton(swatchRef);
            DrawSwatchButtons(swatchRef);
        }
    }

    /// <summary>
    /// Draws current swatch information and clear button.
    /// </summary>
    protected void DrawCurrentSwatchInfoAndClearButton(SwatchColorReference swatchRef)
    {
        if (swatchRef == null || swatchRef.GetSwatchIndex() < 0) return;

        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField($"Current Swatch: {swatchRef.GetSwatchIndex()}", EditorStyles.miniLabel);

        if (GUILayout.Button("Clear Swatch Reference", GUILayout.Width(150)))
        {
            ClearSwatchReferences();
        }

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Draws the grid of swatch buttons with proper layout.
    /// </summary>
    protected void DrawSwatchButtons(SwatchColorReference swatchRef)
    {
        // Calculate how many swatches fit per row
        float inspectorWidth = EditorGUIUtility.currentViewWidth - 40; // Account for margins/padding
        int swatchWidth = boxSize + highlightPadding;
        int swatchesPerRow = Mathf.Max(1, (int)(inspectorWidth / swatchWidth));

        for (int i = 0; i < colorPalette.colors.Length; i += swatchesPerRow)
        {
            EditorGUILayout.BeginHorizontal();

            // Draw swatches for this row
            for (int j = i; j < Mathf.Min(i + swatchesPerRow, colorPalette.colors.Length); j++)
            {
                DrawSingleSwatchButton(j, swatchRef);
            }

            EditorGUILayout.EndHorizontal();
        }
    }

    /// <summary>
    /// Draws a single swatch button with all interaction handling.
    /// </summary>
    protected void DrawSingleSwatchButton(int index, SwatchColorReference swatchRef)
    {
        Color c = colorPalette.colors[index];
        // Allocate space for the swatch button with padding
        Rect rect = GUILayoutUtility.GetRect(boxSize + highlightPadding, boxSize + highlightPadding, GUILayout.Width(boxSize + highlightPadding), GUILayout.Height(boxSize + highlightPadding));

        // Center the actual swatch within the allocated space
        Rect swatchRect = new(rect.x + highlightPadding / 2, rect.y + highlightPadding / 2, boxSize, boxSize);

        // Highlight current swatch with a border
        bool isCurrentSwatch = swatchRef != null && swatchRef.GetSwatchIndex() == index;
        if (isCurrentSwatch)
        {
            EditorGUI.DrawRect(rect, Color.white);
        }

        EditorGUI.DrawRect(swatchRect, c);

        ProcessSwatchMouseEvents(index, swatchRect);
    }

    /// <summary>
    /// Handles mouse events for swatch interaction (click, double-click, right-click).
    /// </summary>
    protected void ProcessSwatchMouseEvents(int index, Rect swatchRect)
    {
        Event currentEvent = Event.current;
        if (swatchRect.Contains(currentEvent.mousePosition))
        {
            if (currentEvent.type == EventType.MouseDown)
            {
                if (currentEvent.button == 0) // Left mouse button
                {
                    if (currentEvent.clickCount == 1)
                    {
                        // Single left-click: Apply swatch
                        ApplySwatchToTargets(index);
                        currentEvent.Use();
                    }
                    else if (currentEvent.clickCount == 2)
                    {
                        // Double left-click: Open color picker
                        OpenColorPicker(index);
                        currentEvent.Use();
                    }
                }
                else if (currentEvent.button == 1) // Right mouse button
                {
                    // Right-click: Show context menu
                    ShowSwatchContextMenu(index);
                    currentEvent.Use();
                }
            }
        }
    }

    /// <summary>
    /// Applies the selected swatch to all currently selected targets.
    /// </summary>
    protected virtual void ApplySwatchToTargets(int swatchIndex)
    {
        foreach (var t in targets)
        {
            Component component = (Component)t;
            SwatchColorReference targetSwatchRef = component.GetComponent<SwatchColorReference>();

            // Record the component state before changes
            Undo.RecordObject(component, "Change Color");

            // Add SwatchColorReference component if it doesn't exist
            if (targetSwatchRef == null)
            {
                targetSwatchRef = CreateSwatchColorReference(component);
                Undo.RecordObject(targetSwatchRef, "Change Swatch Reference");
            }
            else
            {
                Undo.RecordObject(targetSwatchRef, "Change Swatch Reference");
            }

            // Set the swatch index and apply color
            targetSwatchRef.SetSwatchIndexAndApplyColor(swatchIndex);
            UnityEditor.EditorUtility.SetDirty(targetSwatchRef);
        }
    }

    /// <summary>
    /// Creates a SwatchColorReference component on the given component's GameObject.
    /// </summary>
    protected SwatchColorReference CreateSwatchColorReference(Component component)
    {
        SwatchColorReference targetSwatchRef = Undo.AddComponent<SwatchColorReference>(component.gameObject);
        SwatchRefs.Add(targetSwatchRef);
        return targetSwatchRef;
    }

    /// <summary>
    /// Clears swatch references from all selected targets.
    /// </summary>
    protected void ClearSwatchReferences()
    {
        foreach (var t in targets)
        {
            Component component = (Component)t;
            SwatchColorReference sRef = component.GetComponent<SwatchColorReference>();
            if (sRef != null)
            {
                Undo.RecordObject(sRef, "Clear Swatch Reference");
                sRef.ClearSwatchReference();
                EditorUtility.SetDirty(sRef);
            }
        }
    }

    /// <summary>
    /// Opens Unity's color picker for editing a swatch color.
    /// </summary>
    protected void OpenColorPicker(int swatchIndex)
    {
        Color currentColor = colorPalette.colors[swatchIndex];

        System.Action<Color> onColorChanged = newColor =>
        {
            Undo.RecordObject(colorPalette, "Change Swatch Color");
            colorPalette.colors[swatchIndex] = newColor;
            EditorUtility.SetDirty(colorPalette);
            AssetDatabase.SaveAssets();

            UpdateAllSwatchReferences();
            Repaint();
        };

        ColorPickerWindow.Show(currentColor, onColorChanged, $"Swatch {swatchIndex}");
    }

    /// <summary>
    /// Shows the right-click context menu for swatch operations.
    /// </summary>
    protected void ShowSwatchContextMenu(int swatchIndex)
    {
        GenericMenu menu = new();

        menu.AddItem(new GUIContent("Edit Color"), false, () => OpenColorPicker(swatchIndex));
        menu.AddItem(new GUIContent("Copy Color"), false, () => CopyColorToClipboard(swatchIndex));
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Duplicate Swatch"), false, () => DuplicateSwatch(swatchIndex));
        menu.AddItem(new GUIContent("Delete Swatch"), false, () => DeleteSwatch(swatchIndex));

        menu.ShowAsContext();
    }

    /// <summary>
    /// Copies the swatch color to clipboard as a hex string.
    /// </summary>
    protected void CopyColorToClipboard(int swatchIndex)
    {
        Color color = colorPalette.colors[swatchIndex];
        string colorHex = ColorUtility.ToHtmlStringRGBA(color);
        EditorGUIUtility.systemCopyBuffer = $"#{colorHex}";
    }

    /// <summary>
    /// Duplicates a swatch by adding it to the end of the palette.
    /// </summary>
    protected void DuplicateSwatch(int swatchIndex)
    {
        Undo.RecordObject(colorPalette, "Duplicate Swatch");

        Color colorToDuplicate = colorPalette.colors[swatchIndex];
        Color[] newColors = new Color[colorPalette.colors.Length + 1];

        for (int i = 0; i < colorPalette.colors.Length; i++)
        {
            newColors[i] = colorPalette.colors[i];
        }

        newColors[newColors.Length - 1] = colorToDuplicate;

        colorPalette.colors = newColors;
        EditorUtility.SetDirty(colorPalette);
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Deletes a swatch from the palette and updates affected references.
    /// </summary>
    protected void DeleteSwatch(int swatchIndex)
    {
        if (colorPalette.colors.Length <= 1)
        {
            EditorUtility.DisplayDialog("Cannot Delete", "Cannot delete the last remaining swatch.", "OK");
            return;
        }

        Undo.RecordObject(colorPalette, "Delete Swatch");

        Color[] newColors = new Color[colorPalette.colors.Length - 1];
        int newIndex = 0;

        for (int i = 0; i < colorPalette.colors.Length; i++)
        {
            if (i != swatchIndex)
            {
                newColors[newIndex] = colorPalette.colors[i];
                newIndex++;
            }
        }

        colorPalette.colors = newColors;
        EditorUtility.SetDirty(colorPalette);
        AssetDatabase.SaveAssets();

        UpdateSwatchReferencesAfterDeletion(swatchIndex);
    }

    /// <summary>
    /// Updates all SwatchColorReference indices after a swatch deletion.
    /// </summary>
    protected void UpdateSwatchReferencesAfterDeletion(int deletedIndex)
    {
        SwatchColorReference[] allSwatchRefs = FindObjectsByType<SwatchColorReference>(FindObjectsSortMode.None);

        foreach (var swatchRef in allSwatchRefs)
        {
            int currentIndex = swatchRef.GetSwatchIndex();
            if (currentIndex > deletedIndex)
            {
                Undo.RecordObject(swatchRef, "Update Swatch Index");
                swatchRef.SetSwatchIndex(currentIndex - 1);
                EditorUtility.SetDirty(swatchRef);
            }
            else if (currentIndex == deletedIndex)
            {
                Undo.RecordObject(swatchRef, "Clear Deleted Swatch Reference");
                swatchRef.ClearSwatchReference();
                EditorUtility.SetDirty(swatchRef);
            }
        }
    }

    /// <summary>
    /// Updates all SwatchColorReference components to reflect current palette state.
    /// </summary>
    public static void UpdateAllSwatchReferences()
    {
        for (int i = SwatchRefs.Count - 1; i >= 0; i--)
        {
            var swatchRef = SwatchRefs[i];
            if (swatchRef == null)
            {
                SwatchRefs.RemoveAt(i);
                continue;
            }

            if (swatchRef.GetSwatchIndex() < 0) continue;

            swatchRef.UpdateColorFromPalette();
            EditorUtility.SetDirty(swatchRef);
        }
    }

    /// <summary>
    /// Handles manual swatch update for performance optimization.
    /// </summary>
    protected void ManualSwatchUpdate()
    {
        if (!enableManualSwatchUpdate) return;

        foreach (var target in targets)
        {
            Component component = target as Component;
            if (component == null) continue;

            if (component.TryGetComponent<SwatchColorReference>(out var swatchRef))
            {
                SwatchColorReference[] foundRefs = swatchRef.ManualUpdateSwatchReferencesList();
                foreach (var reference in foundRefs)
                {
                    if (!SwatchRefs.Contains(reference))
                    {
                        SwatchRefs.Add(reference);
                    }
                }
                UpdateAllSwatchReferences();
                return;
            }
        }
    }

    /// <summary>
    /// Loads the ColorPalette from Resources, creating a default one if none exists.
    /// </summary>
    protected void LoadColorPalette()
    {
        colorPalette = Resources.Load<ColorPalette>("ColorPalette");

        if (colorPalette == null)
        {
            colorPalette = CreateInstance<ColorPalette>();
            colorPalette.colors = new Color[] { Color.white };

            SavePaletteAssetToResources();
        }
    }

    /// <summary>
    /// Saves the ColorPalette asset to the Resources folder.
    /// </summary>
    protected string SavePaletteAssetToResources()
    {
        string resourcesPath = "Assets/Resources";
        if (!AssetDatabase.IsValidFolder(resourcesPath))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        string assetPath = "Assets/Resources/ColorPalette.asset";
        AssetDatabase.CreateAsset(colorPalette, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return assetPath;
    }
}