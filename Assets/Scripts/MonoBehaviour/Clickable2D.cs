using UnityEngine;

/// <summary>
/// Example clickable 2D object that works with the hover system.
/// </summary>
[RequireComponent(typeof(HighlightableElement2D))]
[RequireComponent(typeof(Collider2D))]
public class Clickable2D : MonoBehaviour, IClickable {
    [Header("Click Settings")]
    [SerializeField] private ClickActionSO clickAction;
    [SerializeField] private bool enableDebug = false;
    
    public virtual void OnClick() {
        if (enableDebug) {
            Debug.Log($"Clicked 2D object: {gameObject.name}");
        }
        
        // Override this method in derived classes for custom click behavior
        HandleCustomClick();
    }
    
    /// <summary>
    /// Override this method to add custom click behavior.
    /// </summary>
    protected virtual void HandleCustomClick() 
    {
        if (clickAction != null && clickAction.CanExecute(this))
        {
            clickAction.Execute(this);
        }
        else if (enableDebug && clickAction == null)
        {
            Debug.LogWarning($"No click action assigned for {gameObject.name}");
        }
    } 
}