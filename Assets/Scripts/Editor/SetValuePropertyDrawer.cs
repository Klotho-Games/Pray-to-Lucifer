using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom property drawer for SetValueAttribute.
/// Monitors the source field and automatically sets the target field when conditions are met.
/// </summary>
[CustomPropertyDrawer(typeof(SetValueAttribute))]
public class SetValuePropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SetValueAttribute attr = (SetValueAttribute)attribute;
        
        // Draw the property normally
        EditorGUI.BeginChangeCheck();
        EditorGUI.PropertyField(position, property, label, true);
        
        // Check if the property changed
        if (EditorGUI.EndChangeCheck())
        {
            // Apply the property modification first
            property.serializedObject.ApplyModifiedProperties();
            
            // Check if condition is met and update target field
            if (CheckCondition(property, attr))
            {
                SetTargetFieldValue(property, attr);
            }
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    private bool CheckCondition(SerializedProperty sourceProperty, SetValueAttribute attr)
    {
        switch (sourceProperty.propertyType)
        {
            case SerializedPropertyType.Boolean:
                bool boolValue = sourceProperty.boolValue;
                if (attr.ConditionValue is bool conditionBool)
                {
                    return boolValue == conditionBool;
                }
                return boolValue;

            case SerializedPropertyType.Enum:
                int enumValue = sourceProperty.enumValueIndex;
                if (attr.ConditionValue != null)
                {
                    int conditionInt = System.Convert.ToInt32(attr.ConditionValue);
                    return enumValue == conditionInt;
                }
                return false;

            case SerializedPropertyType.Integer:
                int intValue = sourceProperty.intValue;
                if (attr.ConditionValue != null)
                {
                    int conditionInt = System.Convert.ToInt32(attr.ConditionValue);
                    return intValue == conditionInt;
                }
                return false;

            case SerializedPropertyType.String:
                string stringValue = sourceProperty.stringValue;
                if (attr.ConditionValue != null)
                {
                    return stringValue == attr.ConditionValue.ToString();
                }
                return !string.IsNullOrEmpty(stringValue);

            default:
                Debug.LogWarning($"SetValue: Unsupported property type '{sourceProperty.propertyType}' for field '{sourceProperty.name}'");
                return false;
        }
    }

    private void SetTargetFieldValue(SerializedProperty sourceProperty, SetValueAttribute attr)
    {
        SerializedProperty targetProperty = FindTargetProperty(sourceProperty, attr.TargetField);
        
        if (targetProperty == null)
        {
            Debug.LogWarning($"SetValue: Could not find target field '{attr.TargetField}' from source '{sourceProperty.name}'");
            return;
        }

        // Set the target property value based on its type
        switch (targetProperty.propertyType)
        {
            case SerializedPropertyType.Boolean:
                if (attr.ValueToSet is bool boolVal)
                {
                    targetProperty.boolValue = boolVal;
                }
                else
                {
                    targetProperty.boolValue = System.Convert.ToBoolean(attr.ValueToSet);
                }
                break;

            case SerializedPropertyType.Integer:
                targetProperty.intValue = System.Convert.ToInt32(attr.ValueToSet);
                break;

            case SerializedPropertyType.Float:
                targetProperty.floatValue = System.Convert.ToSingle(attr.ValueToSet);
                break;

            case SerializedPropertyType.String:
                targetProperty.stringValue = attr.ValueToSet?.ToString() ?? "";
                break;

            case SerializedPropertyType.Enum:
                targetProperty.enumValueIndex = System.Convert.ToInt32(attr.ValueToSet);
                break;

            default:
                Debug.LogWarning($"SetValue: Unsupported target property type '{targetProperty.propertyType}' for field '{attr.TargetField}'");
                return;
        }

        targetProperty.serializedObject.ApplyModifiedProperties();
    }

    private SerializedProperty FindTargetProperty(SerializedProperty sourceProperty, string targetFieldName)
    {
        string path = sourceProperty.propertyPath;
        
        // Handle nested properties
        int lastDotIndex = path.LastIndexOf('.');
        string basePath = lastDotIndex >= 0 ? path.Substring(0, lastDotIndex + 1) : "";
        
        // Try to find the target property
        SerializedProperty targetProperty = sourceProperty.serializedObject.FindProperty(basePath + targetFieldName);
        
        // If not found with base path, try without it (for root-level properties)
        if (targetProperty == null)
        {
            targetProperty = sourceProperty.serializedObject.FindProperty(targetFieldName);
        }
        
        return targetProperty;
    }
}
