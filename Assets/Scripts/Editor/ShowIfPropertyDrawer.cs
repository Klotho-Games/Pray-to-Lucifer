using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom property drawer for ConditionalHideAttribute.
/// Hides/shows fields in the Inspector based on conditions.
/// </summary>
[CustomPropertyDrawer(typeof(ShowIfAttribute))]
public class ConditionalHidePropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ShowIfAttribute condHideAttr = (ShowIfAttribute)attribute;
        bool enabled = GetConditionResult(condHideAttr, property);

        if (!condHideAttr.HideInInspector || enabled)
        {
            // Only draw if not hidden or condition is met
            if (enabled)
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        ShowIfAttribute condHideAttr = (ShowIfAttribute)attribute;
        bool enabled = GetConditionResult(condHideAttr, property);

        if (!condHideAttr.HideInInspector || enabled)
        {
            if (enabled)
            {
                return EditorGUI.GetPropertyHeight(property, label, true);
            }
        }

        // Hide the field by returning 0 height
        return 0f;
    }

    private bool GetConditionResult(ShowIfAttribute condHideAttr, SerializedProperty property)
    {
        // Handle multiple conditions with AND/OR logic
        if (condHideAttr.HasMultipleConditions)
        {
            bool condition1 = CheckSingleCondition(condHideAttr.ConditionalSourceField, condHideAttr.CompareValue, property);
            bool condition2 = CheckSingleCondition(condHideAttr.ConditionalSourceField2, condHideAttr.CompareValue2, property);
            
            if (condHideAttr.UseAndLogic)
            {
                return condition1 && condition2; // AND logic
            }
            else
            {
                return condition1 || condition2; // OR logic
            }
        }
        
        // Single condition
        return CheckSingleCondition(condHideAttr.ConditionalSourceField, condHideAttr.CompareValue, property);
    }
    
    private bool CheckSingleCondition(string fieldName, object compareValue, SerializedProperty property)
    {
        SerializedProperty sourceProperty = GetSourceProperty(fieldName, property);
        
        if (sourceProperty == null)
        {
            Debug.LogWarning($"ShowIf: Could not find source field '{fieldName}' for property '{property.name}'");
            return true; // Show field if source not found
        }

        // Handle different property types
        switch (sourceProperty.propertyType)
        {
            case SerializedPropertyType.Boolean:
                bool boolValue = sourceProperty.boolValue;
                if (compareValue is bool)
                {
                    return boolValue == (bool)compareValue;
                }
                return boolValue;

            case SerializedPropertyType.Enum:
                int enumValue = sourceProperty.enumValueIndex;
                if (compareValue != null)
                {
                    int compareInt = System.Convert.ToInt32(compareValue);
                    return enumValue == compareInt;
                }
                return false;

            case SerializedPropertyType.Integer:
                int intValue = sourceProperty.intValue;
                if (compareValue != null)
                {
                    int compareInt = System.Convert.ToInt32(compareValue);
                    return intValue == compareInt;
                }
                return false;

            case SerializedPropertyType.String:
                string stringValue = sourceProperty.stringValue;
                if (compareValue != null)
                {
                    return stringValue == compareValue.ToString();
                }
                return !string.IsNullOrEmpty(stringValue);

            default:
                Debug.LogWarning($"ShowIf: Unsupported property type '{sourceProperty.propertyType}' for field '{fieldName}'");
                return true;
        }
    }

    private SerializedProperty GetSourceProperty(string fieldName, SerializedProperty property)
    {
        // Get the parent object to find sibling properties
        string path = property.propertyPath;
        
        // Handle nested properties (e.g., "myObject.nestedField")
        int lastDotIndex = path.LastIndexOf('.');
        string basePath = lastDotIndex >= 0 ? path.Substring(0, lastDotIndex + 1) : "";
        
        // Try to find the source property
        SerializedProperty sourceProperty = property.serializedObject.FindProperty(basePath + fieldName);
        
        // If not found with base path, try without it (for root-level properties)
        if (sourceProperty == null)
        {
            sourceProperty = property.serializedObject.FindProperty(fieldName);
        }
        
        return sourceProperty;
    }
}
