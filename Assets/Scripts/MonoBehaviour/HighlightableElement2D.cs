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
    public Color highlightTint = new(1.2f, 1.2f, 1.2f, 1f);
    
    /// <summary>
    /// All SpriteRenderer components that will be affected by highlighting.
    /// </summary>
    public SpriteRenderer[] Models { get; private set; }

    /// <summary>
    /// Colors before highlighting.
    /// </summary>
    [CanBeNull] public List<Color> PreHighlightColors = null;

    [SerializeField] private bool enableDebug = false;
    
    void Awake() {
        // Auto-assign highlight anchor if not set
        if (highlightAnchor == null)
            highlightAnchor = transform;
    }

    void OnEnable() {
        // Find all SpriteRenderer components
        Models = GetComponentsInChildren<SpriteRenderer>();

        // Ensure we have a 2D collider for detection
        if (GetComponent<Collider2D>() == null)
        {
            if (enableDebug) Debug.LogWarning($"HighlightableElement2D on {gameObject.name} requires a Collider2D component for mouse detection!");
            gameObject.AddComponent<Collider2D>();
        }
    }
}