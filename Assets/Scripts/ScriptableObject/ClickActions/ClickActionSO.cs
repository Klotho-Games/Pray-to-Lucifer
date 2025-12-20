using UnityEngine;

/// <summary>
/// Base class for click actions that can be assigned to Clickable2D objects.
/// Create concrete implementations for specific button behaviors.
/// </summary>
public abstract class ClickActionSO : ScriptableObject
{
    [TextArea(2, 4)]
    [Tooltip("Description of what this action does (for documentation)")]
    public string description;
    
    /// <summary>
    /// Execute the click action.
    /// </summary>
    /// <param name="source">The Clickable2D component that triggered this action</param>
    public abstract void Execute(Clickable2D source);
    
    /// <summary>
    /// Optional: Check if this action can be executed.
    /// Override this to add conditional logic.
    /// </summary>
    public virtual bool CanExecute(Clickable2D source) => true;
}
