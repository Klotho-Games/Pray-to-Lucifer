using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

/// <summary>
/// Component that marks a 2D sprite as highlightable when hovered over.
/// </summary>
public class HighlightableElement2D : MonoBehaviour {
    [Header("Highlight Settings")]
    public Transform highlightAnchor;
    public float highlightScale = 1.1f;
    public bool isTint = true;
    public Color highlightColor = new(1.2f, 1.2f, 1.2f, 1f); // Lighten color
    
    /// <summary>
    /// All IColorable components that will be affected by highlighting.
    /// </summary>
    public Component[] Models { get; private set; }

    /// <summary>
    /// Colors before highlighting.
    /// </summary>
    [CanBeNull] public List<Color> PreHighlightColors = null;
    
    /// <summary>
    /// Cached anchor scale before highlighting. Used to restore scale on unhighlight.
    /// </summary>
    public Vector2 PreHighlightScale = Vector2.one;
    public bool PreHighlightScaleCached = false;

    [SerializeField] private bool enableDebug = false;
    
    void Awake() {
        // Auto-assign highlight anchor if not set
        if (highlightAnchor == null)
            highlightAnchor = transform;
    }

    void OnEnable() {
        // Find all components with a color property
        var allComponents = GetComponentsInChildren<Component>();
        var colorableList = new List<Component>();
        
        // Add components with color property
        foreach (var component in allComponents)
        {
            var colorProperty = GetColorProperty(component);
            if (colorProperty == null) continue;

            colorableList.Add(component);
            if (enableDebug)
                Debug.Log($"Found color component: {component.GetType().Name} on {component.gameObject.name}");
        }
        
        Models = colorableList.ToArray();
        
        if (Models.Length == 0)
            Debug.LogWarning($"HighlightableElement2D on {gameObject.name} found no components with color properties!");
        else if (enableDebug)
            Debug.Log($"HighlightableElement2D on {gameObject.name} found {Models.Length} colorable components: {string.Join(", ", System.Array.ConvertAll(Models, c => c.GetType().Name))}");

        // Ensure we have a 2D collider for detection
        if (GetComponent<Collider2D>() == null)
        {
            if (enableDebug) Debug.LogWarning($"HighlightableElement2D on {gameObject.name} requires a Collider2D component for mouse detection!");
            gameObject.AddComponent<BoxCollider2D>();
        }
    }

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
}