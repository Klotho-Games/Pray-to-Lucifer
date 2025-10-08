using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Component that links a SpriteRenderer to a specific color in the ColorPalette system.
/// 
/// WHY: Provides the runtime bridge between sprites and the palette system. Without this component,
/// sprites would lose their color relationships when the palette changes. This maintains the connection
/// and enables automatic color updates, ensuring visual consistency across the entire project.
/// 
/// The component handles automatic registration for performance optimization and manages serialization
/// edge cases like duplicated objects that need to re-register with the editor system.
/// </summary>
[System.Serializable]
public class SwatchColorReference : MonoBehaviour
{
    /// <summary>
    /// Flag for manual update mode to avoid expensive FindObjectsByType calls.
    /// 
    /// WHY: Set by duplicated objects that need to trigger a one-time editor update to register
    /// themselves in the performance-optimized static list maintained by the editor.
    /// </summary>
    [SerializeField] private bool manualSwatchesUpdate = false;
    
    /// <summary>
    /// Index into the ColorPalette array. -1 indicates no swatch assignment.
    /// 
    /// WHY: HideInInspector prevents clutter - users interact through the swatch buttons, not raw indices.
    /// SerializeField ensures the connection persists through play mode and scene saves.
    /// </summary>
    [HideInInspector][SerializeField] private int swatchIndex = -1;

    /// <summary>
    /// Reference to the required component (e.g., SpriteRenderer) for color updates. Property .color required.
    /// 
    /// WHY: Avoids GetComponent calls during frequent Update() color checks, improving runtime performance.
    /// </summary>
    private UnityEngine.Component referencedComponent;

    /// <summary>
    /// Cached PropertyInfo for the color property to avoid reflection overhead during updates.
    /// </summary>
    private PropertyInfo colorProperty;

    private readonly bool enableDebug = true;

    /// <summary>
    /// Cache the SpriteRenderer component reference for performance.
    /// 
    /// WHY: Awake runs before Start, ensuring the component is ready when UpdateColorFromPalette
    /// is called. Caching avoids repeated GetComponent calls during runtime color updates.
    /// </summary>
    private void Awake()
    {
        GetReferencedComponent();
    }

    /// <summary>
    /// Apply the initial palette color when the object starts.
    /// 
    /// WHY: Ensures sprites show the correct palette color immediately when the scene loads,
    /// even if their serialized color doesn't match the current palette (e.g., after palette changes).
    /// </summary>
    private void Start()
    {
        UpdateColorFromPalette();
    }

    /// <summary>
    /// Assigns a swatch index and immediately applies its color to the sprite.
    /// 
    /// WHY: Provides atomic operation for swatch assignment. Ensures the visual change happens 
    /// immediately when users click swatch buttons, giving instant feedback and maintaining
    /// the visual connection between palette and sprite.
    /// </summary>
    public void SetSwatchIndexAndApplyColor(int index)
    {
        SetSwatchIndex(index);
        UpdateColorFromPalette();
    }

    /// <summary>
    /// Sets the swatch index without applying color. Used internally for serialization.
    /// 
    /// WHY: Separated from color application to handle cases where only the reference needs
    /// to be updated (like undo operations) without triggering visual changes prematurely.
    /// </summary>
    public void SetSwatchIndex(int index)
    {
        swatchIndex = index;
    }

    /// <summary>
    /// Returns the current swatch index, or -1 if no swatch is assigned.
    /// 
    /// WHY: Provides read access for the editor to highlight the current swatch and 
    /// for systems that need to know which palette color this sprite references.
    /// </summary>
    public int GetSwatchIndex()
    {
        return swatchIndex;
    }

    /// <summary>
    /// Finds any component on this GameObject that has a 'color' property of type Color.
    /// 
    /// WHY: Uses reflection to automatically detect any component with a color property,
    /// making the system extensible to work with custom components without code changes.
    /// Caches the PropertyInfo for performance during frequent updates.
    /// </summary>
    public void GetReferencedComponent()
    {
        // First try interface-based approach (most performant)
        var colorableComponent = GetComponent<IColorable>();
        if (colorableComponent != null)
        {
            referencedComponent = colorableComponent as UnityEngine.Component;
            colorProperty = typeof(IColorable).GetProperty("color");
            return;
        }
        
        // Fallback to reflection-based approach for built-in Unity components
        UnityEngine.Component[] components = GetComponents<UnityEngine.Component>();
        
        foreach (var component in components)
        {
            if (component == this) continue; // Skip self
            
            Type componentType = component.GetType();
            PropertyInfo property = componentType.GetProperty("color", BindingFlags.Public | BindingFlags.Instance);
            
            // Check if the property exists and is of type Color
            if (property != null && property.PropertyType == typeof(Color) && 
                property.CanRead && property.CanWrite)
            {
                referencedComponent = component;
                colorProperty = property;
                if (enableDebug)
                {
                    Debug.Log($"SwatchColorReference: Found color property on {componentType.Name}");
                }
                return;
            }
        }
        
        if (enableDebug && referencedComponent == null)
        {
            Debug.LogWarning($"SwatchColorReference: No component with 'color' property found on {gameObject.name}");
        }
    }

    /// <summary>
    /// Updates the component's color to match the current palette, but only if they differ.
    /// 
    /// WHY: Called frequently from Update(), so optimization is critical. Uses cached PropertyInfo
    /// to avoid reflection overhead on each call. Only applies changes when needed to avoid 
    /// unnecessary SetDirty calls that trigger serialization.
    /// 
    /// SetDirty ensures changes persist through play mode and scene saves.
    /// </summary>
    public void UpdateColorFromPalette()
    {
        if (referencedComponent == null || colorProperty == null) GetReferencedComponent();
        if (referencedComponent == null || colorProperty == null) return;

        Color tempColor = GetCurrentColor();
        Color colorFromPalette = ColorFromPalette();

        if (colorFromPalette != tempColor)
        {
            if (enableDebug) Debug.Log($"SwatchColorReference: Updating color on {referencedComponent.GetType().Name} from {tempColor} to {colorFromPalette}");
            SetCurrentColor(colorFromPalette);
            
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(referencedComponent);
#endif
        }
    }
    
    /// <summary>
    /// Gets the current color from the referenced component using cached PropertyInfo.
    /// </summary>
    private Color GetCurrentColor()
    {
        try
        {
            return (Color)colorProperty.GetValue(referencedComponent);
        }
        catch (Exception e)
        {
            if (enableDebug)
            {
                Debug.LogError($"SwatchColorReference: Failed to get color from {referencedComponent.GetType().Name}: {e.Message}");
            }
            return Color.clear;
        }
    }
    
    /// <summary>
    /// Sets the color on the referenced component using cached PropertyInfo.
    /// </summary>
    private void SetCurrentColor(Color color)
    {
        try
        {
            colorProperty.SetValue(referencedComponent, color);
        }
        catch (Exception e)
        {
            if (enableDebug)
            {
                Debug.LogError($"SwatchColorReference: Failed to set color on {referencedComponent.GetType().Name}: {e.Message}");
            }
        }
    }

    /// <summary>
    /// Retrieves the color from the component that this component references.
    /// 
    /// WHY: Centralizes palette access and bounds checking. Returns Color.clear for invalid
    /// indices to provide safe fallback behavior instead of exceptions. This graceful degradation
    /// prevents broken sprites when palette sizes change or indices become invalid.
    /// 
    /// Uses Resources.Load for runtime compatibility - the palette needs to be accessible
    /// from both editor and runtime contexts.
    /// </summary>
    public Color ColorFromPalette()
    {
        if (swatchIndex < 0) return Color.clear;

        ColorPalette palette = Resources.Load<ColorPalette>("ColorPalette");
        if (palette == null || palette.colors == null || swatchIndex >= palette.colors.Length) return Color.clear;

        Color paletteColor = palette.colors[swatchIndex];
        // Always update the color to ensure it matches the palette
        return paletteColor;
    }

    public void ClearSwatchReference()
    {
        swatchIndex = -1;
    }

    public SwatchColorReference[] ManualUpdateSwatchReferencesList()
    {
        if (!manualSwatchesUpdate) return new SwatchColorReference[0];
        
        // Update all swatch references
        SwatchColorReference[] swatchRefs = FindObjectsByType<SwatchColorReference>(FindObjectsSortMode.None);

        manualSwatchesUpdate = false;
        return swatchRefs;
    }
}