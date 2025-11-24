using System.Collections.Generic;
using JetBrains.Annotations;
using PrimeTween;
using UnityEngine;
using UnityEngine.InputSystem;

public class HighlightedElement2DController : MonoBehaviour
{
    [SerializeField] float sizeTweenDuration = 0.2f;
    [SerializeField] Ease sizeTweenEaseIn = Ease.OutBack;
    [SerializeField] Ease sizeTweenEaseOut = Ease.OutBack;
    [SerializeField] float colorTweenDuration = 1f;
    [SerializeField] Ease colorTweenEaseIn = Ease.OutQuad;
    [SerializeField] Ease colorTweenEaseOut = Ease.OutQuad;
    [SerializeField] Camera mainCamera;
    [SerializeField] LayerMask hoverLayers = -1;
    [SerializeField] bool enableDebug = true;
    [SerializeField] private bool enableDebugMousePosition = false;
    [CanBeNull] public HighlightableElement2D Current { get; private set; }

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
    void AnimateHighlightedElement2D([NotNull] HighlightableElement2D h, bool isHighlighted)
    {
        if (enableDebug) Debug.Log($"[AnimateHighlightedElement2D] Starting animation for '{h.gameObject.name}' | isHighlighted={isHighlighted}");
        
        // Color tint animation
        if (isHighlighted)
        {
            if (enableDebug) Debug.Log($"[AnimateHighlightedElement2D] HIGHLIGHTING '{h.gameObject.name}'");
            
            // Cache anchor scale if not already cached
            if (!h.PreHighlightScaleCached)
            {
                h.PreHighlightScale = h.highlightAnchor.localScale;
                if (enableDebug) Debug.Log($"[AnimateHighlightedElement2D] Caching original scale: {h.PreHighlightScale} | Target scale multiplier: {h.highlightScale}");
                
                Vector2 targetScale = new(h.highlightScale * h.PreHighlightScale.x, h.highlightScale * h.PreHighlightScale.y);
                if (enableDebug) Debug.Log($"[AnimateHighlightedElement2D] Tweening scale TO: {targetScale} over {sizeTweenDuration}s with ease {sizeTweenEaseIn}");
                
                Tween.ScaleX(h.highlightAnchor, h.highlightScale * h.PreHighlightScale.x, sizeTweenDuration, sizeTweenEaseIn, useUnscaledTime: true);
                Tween.ScaleY(h.highlightAnchor, h.highlightScale * h.PreHighlightScale.y, sizeTweenDuration, sizeTweenEaseIn, useUnscaledTime: true);
                h.PreHighlightScaleCached = true;
            }
            else
            {
                if (enableDebug) Debug.Log($"[AnimateHighlightedElement2D] Scale already cached: {h.PreHighlightScale} | Re-applying highlight scale: {h.highlightScale}");
                
                Tween.ScaleX(h.highlightAnchor, h.highlightScale * h.PreHighlightScale.x, sizeTweenDuration, sizeTweenEaseIn, useUnscaledTime: true);
                Tween.ScaleY(h.highlightAnchor, h.highlightScale * h.PreHighlightScale.y, sizeTweenDuration, sizeTweenEaseIn, useUnscaledTime: true);
            }

            if (h.PreHighlightColors == null || h.PreHighlightColors.Count == 0)
            {
                if (enableDebug) Debug.Log($"[AnimateHighlightedElement2D] No cached colors found. Caching colors for {h.Models.Length} components...");
                
                h.PreHighlightColors = new List<Color>();
                int cachedCount = 0;
                foreach (var component in h.Models)
                {
                    Color? currentColor = GetColor(component);
                    if (currentColor == null)
                    {
                        if (enableDebug) Debug.LogWarning($"[AnimateHighlightedElement2D] Could not get color from component: {component.GetType().Name} on '{component.gameObject.name}'");
                        continue;
                    }

                    h.PreHighlightColors.Add(currentColor.Value);
                    Color targetColor = h.isTint ? OverlayColor(currentColor.Value, h.highlightColor) : h.highlightColor;
                    
                    if (enableDebug) Debug.Log($"[AnimateHighlightedElement2D] Component[{cachedCount}] {component.GetType().Name}: {currentColor.Value} → {targetColor} | isTint={h.isTint} | Duration={colorTweenDuration}s | Ease={colorTweenEaseIn}");
                    
                    TweenExt.Color(component, targetColor, colorTweenDuration, colorTweenEaseIn, useUnscaledTime: true);
                    cachedCount++;
                }
                
                if (enableDebug) Debug.Log($"[AnimateHighlightedElement2D] Cached {cachedCount} colors total");
            }
            else
            {
                if (enableDebug) Debug.Log($"[AnimateHighlightedElement2D] Using {h.PreHighlightColors.Count} cached colors for re-highlighting");
                
                for (int i = 0; i < h.Models.Length; i++)
                {
                    Component component = h.Models[i];
                    Color targetColor = h.isTint ? OverlayColor(h.PreHighlightColors[i], h.highlightColor) : h.highlightColor;
                    
                    if (enableDebug) Debug.Log($"[AnimateHighlightedElement2D] Re-highlighting Component[{i}] {component.GetType().Name}: Cached={h.PreHighlightColors[i]} → Target={targetColor}");
                    
                    TweenExt.Color(component, targetColor, colorTweenDuration, colorTweenEaseIn, useUnscaledTime: true);
                }
            }
        }
        else
        {
            if (enableDebug) Debug.Log($"[AnimateHighlightedElement2D] UN-HIGHLIGHTING '{h.gameObject.name}'");
            
            if (h.PreHighlightScaleCached)
            {
                if (enableDebug) Debug.Log($"[AnimateHighlightedElement2D] Restoring scale FROM current to CACHED: {h.PreHighlightScale} over {sizeTweenDuration}s with ease {sizeTweenEaseOut}");
                
                // Restore to pre-highlight scale
                Tween.ScaleX(h.highlightAnchor, h.PreHighlightScale.x, sizeTweenDuration, sizeTweenEaseOut, useUnscaledTime: true);
                Tween.ScaleY(h.highlightAnchor, h.PreHighlightScale.y, sizeTweenDuration, sizeTweenEaseOut, useUnscaledTime: true);
            }
            else
            {
                if (enableDebug) Debug.LogWarning($"[AnimateHighlightedElement2D] No cached scale found to restore for '{h.gameObject.name}'");
            }

            if (h.PreHighlightColors != null && h.PreHighlightColors.Count > 0)
            {
                if (enableDebug) Debug.Log($"[AnimateHighlightedElement2D] Restoring {h.PreHighlightColors.Count} cached colors...");
                
                int restoredCount = 0;
                int skippedCount = 0;
                int failedCount = 0;
                
                for (int i = 0; i < h.Models.Length; i++)
                {
                    Component component = h.Models[i];
                    
                    Color? currentColor = GetColor(component);
                    if (currentColor == null)
                    {
                        if (enableDebug) Debug.LogWarning($"[AnimateHighlightedElement2D] Component[{i}] {component.GetType().Name}: Could not get current color");
                        failedCount++;
                        continue;
                    }
                    
                    if (h.PreHighlightColors[i] == currentColor.Value)
                    {
                        if (enableDebug) Debug.Log($"[AnimateHighlightedElement2D] Component[{i}] {component.GetType().Name}: Already at cached color {currentColor.Value}, skipping tween");
                        skippedCount++;
                        continue;
                    }
                    
                    if (enableDebug) Debug.Log($"[AnimateHighlightedElement2D] Component[{i}] {component.GetType().Name}: Restoring {currentColor.Value} → {h.PreHighlightColors[i]} over {colorTweenDuration}s with ease {colorTweenEaseOut}");
                    
                    TweenExt.Color(component, h.PreHighlightColors[i], colorTweenDuration, colorTweenEaseOut, useUnscaledTime: true);
                    restoredCount++;
                }
                
                if (enableDebug) Debug.Log($"[AnimateHighlightedElement2D] Color restoration summary: Restored={restoredCount}, Skipped={skippedCount}, Failed={failedCount}");

                // Schedule a check after all tweens complete to see if we can clear the list and restore scale
                if (enableDebug) Debug.Log($"[AnimateHighlightedElement2D] Starting coroutine to monitor and clear cache after {colorTweenDuration}s");
                StartCoroutine(CheckAndClearPreHighlightColorsAfterDelay(h, colorTweenDuration));
            }
            else
            {
                if (enableDebug) Debug.LogWarning($"[AnimateHighlightedElement2D] No cached colors to restore for '{h.gameObject.name}'");
            }
        }
        
        if (enableDebug) Debug.Log($"[AnimateHighlightedElement2D] Finished animation setup for '{h.gameObject.name}'");
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
        if (highlightable == null || highlightable.PreHighlightColors == null || highlightable.PreHighlightColors.Count == 0)
        {
            yield break;
        }

        if (highlightable == Current) yield break;

        if (enableDebug) Debug.Log(highlightable + " is not current highlight, clearing colors...");

    List<Color> tempColors = new(highlightable.PreHighlightColors);
    Vector2 cachedScale = highlightable.PreHighlightScale;
    bool hadScaleCached = highlightable.PreHighlightScaleCached;

    highlightable.PreHighlightColors.Clear();
    highlightable.PreHighlightScaleCached = false;

        // Monitor for re-highlighting during the same duration as the original tweenar 
        float elapsedTime = 0f;
        while (elapsedTime < delay)
        {
            yield return null;
            elapsedTime += Time.deltaTime;

            if (highlightable == Current)
            {
                highlightable.PreHighlightColors = tempColors;
                if (hadScaleCached)
                {
                    highlightable.PreHighlightScale = cachedScale;
                    highlightable.PreHighlightScaleCached = true;
                }
                if (enableDebug) Debug.Log(highlightable + " became current highlight again, restoring colors.");
                yield break;
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
