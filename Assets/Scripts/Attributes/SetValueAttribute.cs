using UnityEngine;

/// <summary>
/// Attribute that automatically sets a target field to a specific value when a condition is met.
/// Place this attribute on the SOURCE field that you want to monitor.
/// Usage:
/// - [SetValue("targetField", valueToSet)] - Sets target when this bool becomes true
/// - [SetValue("targetField", valueToSet, conditionValue)] - Sets target when this field equals conditionValue
/// </summary>
public class SetValueAttribute : PropertyAttribute
{
    /// <summary>
    /// The name of the field to modify
    /// </summary>
    public string TargetField;
    
    /// <summary>
    /// The value to set the target field to
    /// </summary>
    public object ValueToSet;
    
    /// <summary>
    /// The condition value this field must equal (default: true for bool fields)
    /// </summary>
    public object ConditionValue;
    
    /// <summary>
    /// Sets target field to a value when this bool field is true.
    /// </summary>
    public SetValueAttribute(string targetField, object valueToSet)
    {
        TargetField = targetField;
        ValueToSet = valueToSet;
        ConditionValue = true; // Default for bool fields
    }
    
    /// <summary>
    /// Sets target field to a value when this field equals a specific condition value.
    /// </summary>
    public SetValueAttribute(string targetField, object valueToSet, object conditionValue)
    {
        TargetField = targetField;
        ValueToSet = valueToSet;
        ConditionValue = conditionValue;
    }
}
