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
        // Strategy: Prefer IColorable wrappers, skip raw components if wrapper exists
        var allComponents = GetComponentsInChildren<Component>();
        var colorableList = new List<Component>();
        var gameObjectsWithWrappers = new HashSet<GameObject>();
        
        // First pass: Find all IColorable components and track their GameObjects
        foreach (var component in allComponents)
        {
            if (component == null) continue;
            
            if (component is IColorable)
            {
                colorableList.Add(component);
                gameObjectsWithWrappers.Add(component.gameObject);
                if (enableDebug)
                    Debug.Log($"Found IColorable wrapper: {component.GetType().Name} on {component.gameObject.name}");
            }
        }
        
        // Second pass: Add raw components with color property ONLY if their GameObject doesn't have a wrapper
        foreach (var component in allComponents)
        {
            if (component == null) continue;
            if (component is IColorable) continue; // Skip, already added in first pass
            if (gameObjectsWithWrappers.Contains(component.gameObject)) continue; // Skip, GameObject has wrapper
            
            // Check if it has a color property (SpriteRenderer, UI.Image, etc.)
            var colorProperty = component.GetType().GetProperty("color");
            if (colorProperty != null && colorProperty.PropertyType == typeof(Color))
            {
                colorableList.Add(component);
                if (enableDebug)
                    Debug.Log($"Found raw color component: {component.GetType().Name} on {component.gameObject.name}");
            }
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
}