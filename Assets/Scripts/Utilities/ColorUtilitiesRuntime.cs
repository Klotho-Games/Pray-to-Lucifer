using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Runtime-safe utility class for color property reflection.
/// Does not depend on UnityEditor and can be used in builds.
/// </summary>
public static class ColorUtilitiesRuntime
{    
    /// <summary>
    /// Gets the color property from any component that has one.
    /// Supports Color and Color32 types.
    /// </summary>
    public static System.Reflection.PropertyInfo GetColorProperty(Component component)
    {
        if (component == null) return null;

        // Check common color property names
        var colorProperty = component.GetType().GetProperty("color")
                            ?? component.GetType().GetProperty("vertexColor")
                            ?? component.GetType().GetProperty("tintColor")
                            ?? component.GetType().GetProperty("fillColor")
                            ?? component.GetType().GetProperty("backgroundColor")
                            ?? component.GetType().GetProperty("emissionColor");

        if (colorProperty != null)
        {
            var propType = colorProperty.PropertyType;

            // Support both Color and Color32
            if (propType == typeof(Color) || propType == typeof(Color32))
            {
                return colorProperty;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the color value from a component if it has a color property.
    /// </summary>
    public static Color? GetColor(Component component)
    {
        var colorProperty = GetColorProperty(component);
        if (colorProperty == null) return null;

        try
        {
            return (Color)colorProperty.GetValue(component);
        }
        catch
        {
            return null;
        }
    }
    
    /// <summary>
    /// Sets the color on a component if it has a color property.
    /// </summary>
    public static void SetColor(Component component, Color newColor)
    {
        var colorProperty = GetColorProperty(component);
        if (colorProperty == null) return;

        try
        {
            colorProperty.SetValue(component, newColor);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to set color on {component.GetType().Name}: {e.Message}");
        }
    }

    /// <summary>
    /// Finds all components on a GameObject that have a Color property.
    /// </summary>
    public static List<Component> FindColorableComponents(GameObject gameObject)
    {
        List<Component> colorableComponents = new();
        Component[] allComponents = gameObject.GetComponents<Component>();

        foreach (var component in allComponents)
        {
            if (component == null) continue;

            // Skip SwatchColorReference itself
            if (component is SwatchColorReference) continue;

            // Check for color property
            if (GetColorProperty(component) != null)
            {
                colorableComponents.Add(component);
            }
        }

        return colorableComponents;
    }

    /// <summary>
    /// Loads the ColorPalette from Resources folder.
    /// </summary>
    public static ColorPalette LoadColorPalette()
    {
        return Resources.Load<ColorPalette>("ColorPalette");
    }

    /// <summary>
    /// Gets a safe color from the palette, returning Color.clear for invalid indices.
    /// </summary>
    public static Color GetSafeColorFromPalette(int swatchIndex, ColorPalette palette)
    {
        if (palette != null && 
            palette.colors != null && 
            swatchIndex >= 0 && 
            swatchIndex < palette.colors.Length)
        {
            return palette.colors[swatchIndex];
        }
        return Color.clear;
    }

    /// <summary>
    /// Validates that a swatch index is within valid bounds for a palette.
    /// </summary>
    public static bool IsValidSwatchIndex(int swatchIndex, ColorPalette palette)
    {
        return palette != null && 
               palette.colors != null && 
               swatchIndex >= 0 && 
               swatchIndex < palette.colors.Length;
    }
}
