using System.Collections.Generic;
using JetBrains.Annotations;
using PrimeTween;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;

public class HighlightedElement2DController : MonoBehaviour
{
    const float sizeTweenDuration = 0.2f;
    const float colorTweenDuration = 1f;
    [SerializeField] Camera mainCamera;
    [SerializeField] LayerMask hoverLayers = -1;
    [SerializeField] bool enableDebug = false;
    [SerializeField] private bool enableDebugMousePosition = false;
    [ShowInInspector][CanBeNull] public HighlightableElement2D Current { get; private set; }

    void Awake()
    {
        // Auto-assign main camera if not set
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                if (enableDebug) Debug.LogError("HighlightedElement2DController: No main camera found! Please assign a camera.");
            }
        }
    }

    void Update()
    {
        if (mainCamera == null) return;

        // Handle mobile touch input
        if (Application.isMobilePlatform && !IsTouchActive())
        {
            SetCurrentHighlighted(null);
            return;
        }

        // Get mouse world position for 2D raycast
        Vector2 mouseWorldPos = GetMouseWorldPosition();
        if (enableDebugMousePosition) Debug.Log($"Mouse World Position: {mouseWorldPos}");
        var highlightableElement = RaycastHighlightableElement2D(mouseWorldPos);
        SetCurrentHighlighted(highlightableElement);

        // Handle click input
        if (Current != null && IsClickPressed())
        {
            var clickable = Current.GetComponent<IClickable>();
            if (clickable != null)
            {
                clickable.OnClick();
            }
        }
    }

    /// <summary>
    /// Converts mouse screen position to world position for 2D.
    /// Works with both old Input Manager and new Input System.
    /// </summary>
    private Vector2 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = GetMouseScreenPosition();
        if (enableDebugMousePosition) Debug.Log($"Mouse Screen Position: {mouseScreenPos}");
        
        return mainCamera.ScreenToWorldPoint(mouseScreenPos);
    }

    /// <summary>
    /// Gets mouse screen position using the appropriate input system.
    /// </summary>
    private Vector3 GetMouseScreenPosition()
    {
        // New Input System
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }
        if (Input.mousePosition != null)
        {
            return Input.mousePosition;
        }
        return Vector3.zero;
    }

    /// <summary>
    /// Checks if touch is currently active using the appropriate input system.
    /// </summary>
    private bool IsTouchActive()
    {
        return Touchscreen.current != null && Touchscreen.current.touches.Count > 0;
    }

    /// <summary>
    /// Checks if mouse/touch click was pressed this frame using the appropriate input system.
    /// </summary>
    private bool IsClickPressed()
    {
        // New Input System
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            return true;
        }
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// Uses 2D physics raycast to detect hoverable elements.
    /// </summary>
    [CanBeNull]
    private HighlightableElement2D RaycastHighlightableElement2D(Vector2 worldPosition)
    {
        RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero, Mathf.Infinity, hoverLayers);

        if (hit.collider != null)
        {
            var element = hit.collider.GetComponentInParent<HighlightableElement2D>();

            if (element != null)
            {
                if (enableDebugMousePosition) Debug.Log($"Mouse over 2D element: {element.gameObject.name}");
            }
            else
            {
                if (enableDebugMousePosition) Debug.Log("Mouse over 2D collider with no HighlightableElement2D component.");
            }

            return element;
        }

        return null;
    }

    void SetCurrentHighlighted([CanBeNull] HighlightableElement2D newHighlighted)
    {
        if (newHighlighted != Current)
        {
            // Remove highlight from previous element
            if (Current != null)
            {
                AnimateHighlightedElement2D(Current, false);

                if (enableDebug) Debug.Log($"Stopped hovering 2D: {Current.gameObject.name}");
            }

            Current = newHighlighted;

            // Apply highlight to new element
            if (newHighlighted != null)
            {
                AnimateHighlightedElement2D(newHighlighted, true);

                if (enableDebug) Debug.Log($"Started hovering 2D: {newHighlighted.gameObject.name}");
            }
        }
    }

    /// <summary>
    /// Animates 2D sprite highlighting effects.
    /// </summary>
    void AnimateHighlightedElement2D([NotNull] HighlightableElement2D highlightable, bool isHighlighted)
    {
        // Scale animation
        Vector3 targetScale = isHighlighted
            ? Vector3.one * highlightable.highlightScale
            : Vector3.one;
        Tween.Scale(highlightable.highlightAnchor, targetScale, sizeTweenDuration, Ease.OutBack);

        // Color tint animation
        if (isHighlighted)
        {
            if (highlightable.PreHighlightColors.IsNullOrEmpty())
            {
                highlightable.PreHighlightColors = new List<Color>();
                foreach (var spriteRenderer in highlightable.Models)
                {
                    highlightable.PreHighlightColors.Add(spriteRenderer.color);
                    Tween.Color(spriteRenderer, OverlayColor(spriteRenderer.color, highlightable.highlightTint), colorTweenDuration, Ease.OutQuad);
                }
            }
            else
            {
                for (int i = 0; i < highlightable.Models.Length; i++)
                {
                    SpriteRenderer spriteRenderer = highlightable.Models[i];
                    Tween.Color(spriteRenderer, OverlayColor(highlightable.PreHighlightColors[i], highlightable.highlightTint), colorTweenDuration, Ease.OutQuad);
                }
            }
        }
        else if (!highlightable.PreHighlightColors.IsNullOrEmpty())
        {
            for (int i = 0; i < highlightable.Models.Length; i++)
            {
                SpriteRenderer spriteRenderer = highlightable.Models[i];
                if (highlightable.PreHighlightColors[i] == spriteRenderer.color) continue;

                Tween.Color(spriteRenderer, highlightable.PreHighlightColors[i], colorTweenDuration, Ease.OutQuad);
            }
            
            // Schedule a check after all tweens complete to see if we can clear the list
            StartCoroutine(CheckAndClearPreHighlightColorsAfterDelay(highlightable, colorTweenDuration));
        }
    }

    /// <summary>
    /// Coroutine that waits for all color tweens to complete, then checks if all sprites
    /// successfully reverted and clears the cache if so.
    /// </summary>
    System.Collections.IEnumerator CheckAndClearPreHighlightColorsAfterDelay(HighlightableElement2D highlightable, float delay)
    {
        // Wait for all color tweens to complete
        yield return new WaitForSeconds(delay);
        yield return null; // Wait one frame to ensure OnComplete callbacks executed


        // Check if the list still exists and hasn't been cleared already
        if (highlightable == null || highlightable.PreHighlightColors.IsNullOrEmpty())
        {
            yield break;
        }

        if (highlightable == Current) yield break;

        if (enableDebug) Debug.Log(highlightable + " is not current highlight, clearing colors...");

        List<Color> tempColors = new(highlightable.PreHighlightColors);
        
        highlightable.PreHighlightColors.Clear();

        for (int i = 0; i < 100; ++i)
        {
            yield return null;

            if (highlightable == Current)
            {
                highlightable.PreHighlightColors = tempColors;
            }
        }
        
        tempColors.Clear();
    }

    static Color OverlayColor(Color baseColor, Color overlayColor)
    {
        return new Color(
            Mathf.Clamp01(baseColor.r * overlayColor.r),
            Mathf.Clamp01(baseColor.g * overlayColor.g),
            Mathf.Clamp01(baseColor.b * overlayColor.b),
            baseColor.a);
    }

    /// <summary>
    /// Gets the current mouse world position (useful for other systems).
    /// </summary>
    public Vector2 GetCurrentMouseWorldPosition()
    {
        return GetMouseWorldPosition();
    }

    /// <summary>
    /// Manually clear the current highlight.
    /// </summary>
    public void ClearHighlight()
    {
        SetCurrentHighlighted(null);
    }
}
