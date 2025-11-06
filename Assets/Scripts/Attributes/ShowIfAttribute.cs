using UnityEngine;

/// <summary>
/// Attribute to conditionally show fields in the Inspector based on another field's value.
/// </summary>
public class ShowIfAttribute : PropertyAttribute
{
    public string ConditionalSourceField;
    public object CompareValue;
    
    public string ConditionalSourceField2;
    public object CompareValue2;
    
    public bool UseAndLogic;
    public bool HasMultipleConditions;
    public bool HideInInspector;
    
    /// <summary>
    /// Shows field only when the specified bool field is true.
    /// </summary>
    public ShowIfAttribute(string conditionalSourceField)
    {
        ConditionalSourceField = conditionalSourceField;
        CompareValue = true;
        HasMultipleConditions = false;
        HideInInspector = false;
    }
    
    /// <summary>
    /// Shows field only when the specified field equals the compare value.
    /// </summary>
    public ShowIfAttribute(string conditionalSourceField, object compareValue)
    {
        ConditionalSourceField = conditionalSourceField;
        CompareValue = compareValue;
        HasMultipleConditions = false;
        HideInInspector = false;
    }
    
    /// <summary>
    /// Shows field when two bool fields meet the condition (AND or OR).
    /// </summary>
    public ShowIfAttribute(bool useAndLogic, string conditionalSourceField1, string conditionalSourceField2)
    {
        UseAndLogic = useAndLogic;
        ConditionalSourceField = conditionalSourceField1;
        CompareValue = true;
        ConditionalSourceField2 = conditionalSourceField2;
        CompareValue2 = true;
        HasMultipleConditions = true;
        HideInInspector = false;
    }

    /// <summary>
    /// Shows field when a bool field (true) and another field with compare value meet the condition (AND or OR).
    /// </summary>
    public ShowIfAttribute(bool useAndLogic, string boolConditionalField, string conditionalSourceField2, object compareValue2)
    {
        UseAndLogic = useAndLogic;
        ConditionalSourceField = boolConditionalField;
        CompareValue = true;
        ConditionalSourceField2 = conditionalSourceField2;
        CompareValue2 = compareValue2;
        HasMultipleConditions = true;
        HideInInspector = false;
    }
    
    /// <summary>
    /// Shows field when a bool field (true) and another field with compare value meet the condition (AND or OR).
    /// </summary>
    public ShowIfAttribute(bool useAndLogic, string conditionalSourceField2, object compareValue2, string boolConditionalField)
    {
        UseAndLogic = useAndLogic;
        ConditionalSourceField = boolConditionalField;
        CompareValue = true;
        ConditionalSourceField2 = conditionalSourceField2;
        CompareValue2 = compareValue2;
        HasMultipleConditions = true;
        HideInInspector = false;
    }
    
    /// <summary>
    /// Shows field when two fields with compare values meet the condition (AND or OR).
    /// </summary>
    public ShowIfAttribute(bool useAndLogic, string conditionalSourceField1, object compareValue1, string conditionalSourceField2, object compareValue2)
    {
        UseAndLogic = useAndLogic;
        ConditionalSourceField = conditionalSourceField1;
        CompareValue = compareValue1;
        ConditionalSourceField2 = conditionalSourceField2;
        CompareValue2 = compareValue2;
        HasMultipleConditions = true;
        HideInInspector = false;
    }
}
