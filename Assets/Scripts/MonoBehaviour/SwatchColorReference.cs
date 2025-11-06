using System;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Component that links a SpriteRenderer to a specific color in the ColorPalette system.
/// 
/// The component handles automatic registration for performance optimization and manages serialization
/// edge cases like duplicated objects that need to re-register with the editor system.
/// </summary>
[Serializable]
public class SwatchColorReference : MonoBehaviour
{
    /// <summary>
    /// Flag for manual update mode to avoid expensive FindObjectsByType calls.
    /// </summary>
    [SerializeField] private bool manualSwatchesUpdate = false;
    
    /// <summary>
    /// Index into the ColorPalette array. -1 indicates no swatch assignment.
    /// </summary>
    [SerializeField] private int swatchIndex = -1;

    /// <summary>
    /// Reference to the required component (e.g., SpriteRenderer) for color updates. Property .color required.
    /// </summary>
    private UnityEngine.Component referencedComponent;

    /// <summary>
    /// Cached PropertyInfo for the color property to avoid reflection overhead during updates.
    /// </summary>
    private PropertyInfo colorProperty;


    private Color colorBeforeDetachment = new(1,0,1,1); // pink
    private int swatchIndexAtDetachment = -1;

    private Color LastFramePaletteColor;

    private readonly bool enableDebug = false;

    /// <summary>
    /// Cache the SpriteRenderer component reference for performance.
    /// </summary>
    private void Awake()
    {
        GetReferencedComponent();
    }

    /// <summary>
    /// Apply the initial palette color when the object starts.
    /// </summary>
    private void Start()
    {
        if (swatchIndex >= 0) UpdateColorFromPalette();
    }

    void Update()
    {
        if (swatchIndex != -1 && GetCurrentColor() != ColorFromPalette())
        {
            if (LastFramePaletteColor != ColorFromPalette())
            {
                if (enableDebug) Debug.Log("Updating color because palette changed");
                // palette color has changed
                UpdateColorFromPalette();
            }
            else
            {
                if (enableDebug) Debug.Log("Swatch detached due to manual color change");
                // has been changed externally
                swatchIndexAtDetachment = swatchIndex;
                colorBeforeDetachment = ColorFromPalette();
                swatchIndex = -1;
            }
        }

        if (swatchIndex == -1 && GetCurrentColor() == colorBeforeDetachment)
        {
            if (enableDebug) Debug.Log("Re-attaching swatch");
            swatchIndex = swatchIndexAtDetachment;
            UpdateColorFromPalette();
        }
        
        LastFramePaletteColor = ColorFromPalette();
    }

    /// <summary>
    /// Assigns a swatch index and immediately applies its color to the sprite.
    /// </summary>
    public void SetSwatchIndexAndApplyColor(int index)
    {
        SetSwatchIndex(index);
        UpdateColorFromPalette();
    }

    /// <summary>
    /// Sets the swatch index without applying color. Used internally for serialization.
    /// </summary>
    public void SetSwatchIndex(int index)
    {
        swatchIndex = index;
    }

    /// <summary>
    /// Returns the current swatch index, or -1 if no swatch is assigned.
    /// </summary>
    public int GetSwatchIndex()
    {
        return swatchIndex;
    }

    /// <summary>
    /// Finds any component on this GameObject that has a 'color' property of type Color.
    /// </summary>
    public void GetReferencedComponent()
    {
        // Reflection-based approach for built-in Unity components
        UnityEngine.Component[] components = GetComponents<UnityEngine.Component>();
        
        foreach (var component in components)
        {
            if (component == this) continue; // Skip self
            
            var property = ColorUtilitiesRuntime.GetColorProperty(component);

            if (property != null)
            {
                referencedComponent = component;
                colorProperty = property;
                if (enableDebug)
                {
                    Type componentType = component.GetType();
                    Debug.Log($"SwatchColorReference: Found color property on {componentType.Name}");
                }
                return;
            }
            // // Check if the property exists and is of type Color
            // if (property != null && property.PropertyType == typeof(Color) && 
            //     property.CanRead && property.CanWrite)
            // {
            //     referencedComponent = component;
            //     colorProperty = property;
            //     if (enableDebug)
            //     {
            //         if (enableDebug) Debug.Log($"SwatchColorReference: Found color property on {componentType.Name}");
            //     }
            //     return;
            // }
        }
        
        if (enableDebug && referencedComponent == null)
        {
            Debug.LogWarning($"SwatchColorReference: No component with 'color' property found on {gameObject.name}");
        }
    }

    /// <summary>
    /// Updates the component's color to match the current palette, but only if they differ.
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
    /// </summary>
    public Color ColorFromPalette()
    {
        if (swatchIndex < 0) return Color.pink; // Indicate no swatch assigned

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