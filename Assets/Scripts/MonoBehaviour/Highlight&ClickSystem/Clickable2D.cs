using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Example clickable 2D object that works with the hover system.
/// </summary>
[RequireComponent(typeof(HighlightableElement2D))]
[RequireComponent(typeof(Collider2D))]
public class Clickable2D : MonoBehaviour, IClickable {
    [Header("Click Settings")]
    [SerializeField] private ActionSO[] clickActions;
    [SerializeField] private bool enableDebug = false;
    
    public virtual void OnClick() {
        if (enableDebug) {
            Debug.Log($"Clicked 2D object: {gameObject.name}");
        }
        
        // Override this method in derived classes for custom click behavior
        if (clickActions == null || clickActions.Length == 0)
        {
            if (enableDebug)
            {
                Debug.LogWarning($"No click actions assigned for {gameObject.name}");
            }
            return;
        }
        foreach (var action in clickActions)
        {
            TryExecute(action);
        }
    }
    
    /// <summary>
    /// Override this method to add custom click behavior.
    /// </summary>
    protected virtual void TryExecute(ActionSO clickAction) 
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