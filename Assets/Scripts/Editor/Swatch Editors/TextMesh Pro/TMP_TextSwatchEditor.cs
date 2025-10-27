using UnityEngine;
using UnityEditor;
using TMPro;
using TMPro.EditorUtilities;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Custom editor for TextMeshProUGUI that integrates swatch-based color management.
/// </summary>
[CustomEditor(typeof(TextMeshProUGUI))]
public class TMP_TextSwatchEditor : SwatchEditorBase
{
    private static bool s_ExtraSettingsFoldout = false;
    private static readonly string[] k_UiStateLabel = new string[] { "<i>(Click to collapse)</i> ", "<i>(Click to expand)</i> " };
    
    private string m_RtlText;
    private GUIContent[] m_StyleNames;
    private List<TMP_Style> m_Styles = new List<TMP_Style>();
    private Dictionary<int, int> m_TextStyleIndexLookup = new Dictionary<int, int>();
    private int m_StyleSelectionIndex;
    
    private Material[] m_MaterialPresets;
    private GUIContent[] m_MaterialPresetNames;
    private Dictionary<int, int> m_MaterialPresetIndexLookup = new Dictionary<int, int>();
    private int m_MaterialPresetSelectionIndex;

    protected override void OnEnable()
    {
        base.OnEnable();
        
        // Get Styles from Style Sheet
        if (TMP_Settings.instance != null)
            m_StyleNames = GetStyleNames();
        
        // Get Material Presets
        m_MaterialPresetNames = GetMaterialPresets();
    }

    private GUIContent[] GetStyleNames()
    {
        m_TextStyleIndexLookup.Clear();
        m_Styles.Clear();

        // First style on the list is always the Normal default style
        TMP_Style styleNormal = TMP_Style.NormalStyle;
        m_Styles.Add(styleNormal);
        m_TextStyleIndexLookup.Add(styleNormal.hashCode, 0);

        // Get styles from Style Sheet potentially assigned to the text object
        SerializedProperty styleSheetProp = serializedObject.FindProperty("m_StyleSheet");
        TMP_StyleSheet localStyleSheet = (TMP_StyleSheet)styleSheetProp.objectReferenceValue;

        if (localStyleSheet != null)
        {
            SerializedObject styleSheetObj = new SerializedObject(localStyleSheet);
            SerializedProperty stylesListProp = styleSheetObj.FindProperty("m_StyleList");
            
            for (int i = 0; i < stylesListProp.arraySize; i++)
            {
                SerializedProperty styleProp = stylesListProp.GetArrayElementAtIndex(i);
                TMP_Style style = localStyleSheet.GetStyle(styleProp.FindPropertyRelative("m_HashCode").intValue);
                
                if (style != null && !m_TextStyleIndexLookup.ContainsKey(style.hashCode))
                {
                    m_Styles.Add(style);
                    m_TextStyleIndexLookup.Add(style.hashCode, m_TextStyleIndexLookup.Count);
                }
            }
        }

        // Get styles from TMP Settings' default style sheet
        TMP_StyleSheet globalStyleSheet = TMP_Settings.defaultStyleSheet;
        if (globalStyleSheet != null)
        {
            SerializedObject globalSheetObj = new SerializedObject(globalStyleSheet);
            SerializedProperty globalStylesListProp = globalSheetObj.FindProperty("m_StyleList");
            
            for (int i = 0; i < globalStylesListProp.arraySize; i++)
            {
                SerializedProperty styleProp = globalStylesListProp.GetArrayElementAtIndex(i);
                TMP_Style style = globalStyleSheet.GetStyle(styleProp.FindPropertyRelative("m_HashCode").intValue);
                
                if (style != null && !m_TextStyleIndexLookup.ContainsKey(style.hashCode))
                {
                    m_Styles.Add(style);
                    m_TextStyleIndexLookup.Add(style.hashCode, m_TextStyleIndexLookup.Count);
                }
            }
        }

        // Create array that will contain the list of available styles
        return m_Styles.Select(item => new GUIContent(item.name)).ToArray();
    }

    private GUIContent[] GetMaterialPresets()
    {
        SerializedProperty fontAssetProp = serializedObject.FindProperty("m_fontAsset");
        TMP_FontAsset fontAsset = fontAssetProp.objectReferenceValue as TMP_FontAsset;
        if (fontAsset == null) return null;

        // Get all materials that reference this font asset's texture
        List<Material> materialList = new List<Material>();
        Material baseMaterial = fontAsset.material;
        if (baseMaterial != null)
            materialList.Add(baseMaterial);

        // Find materials matching the font asset name pattern
        string searchPattern = "t:Material " + fontAsset.name.Split(' ')[0];
        string[] materialGUIDs = AssetDatabase.FindAssets(searchPattern);

        for (int i = 0; i < materialGUIDs.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(materialGUIDs[i]);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (mat != null && baseMaterial != null && 
                mat.HasProperty("_MainTex") && mat.GetTexture("_MainTex") != null &&
                baseMaterial.GetTexture("_MainTex") != null &&
                mat.GetTexture("_MainTex").GetInstanceID() == baseMaterial.GetTexture("_MainTex").GetInstanceID())
            {
                if (!materialList.Contains(mat))
                    materialList.Add(mat);
            }
        }

        m_MaterialPresets = materialList.ToArray();
        m_MaterialPresetNames = new GUIContent[m_MaterialPresets.Length];
        m_MaterialPresetIndexLookup.Clear();

        for (int i = 0; i < m_MaterialPresetNames.Length; i++)
        {
            m_MaterialPresetNames[i] = new GUIContent(m_MaterialPresets[i].name);
            m_MaterialPresetIndexLookup.Add(m_MaterialPresets[i].GetInstanceID(), i);
        }

        return m_MaterialPresetNames;
    }

    private static bool EditorToggle(Rect position, bool value, GUIContent content, GUIStyle style)
    {
        int id = GUIUtility.GetControlID(content, FocusType.Keyboard, position);
        Event evt = Event.current;

        // Toggle selected toggle on space or return key
        if (GUIUtility.keyboardControl == id && evt.type == EventType.KeyDown && 
            (evt.keyCode == KeyCode.Space || evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter))
        {
            value = !value;
            evt.Use();
            GUI.changed = true;
        }

        if (evt.type == EventType.MouseDown && position.Contains(Event.current.mousePosition))
        {
            GUIUtility.keyboardControl = id;
            EditorGUIUtility.editingTextField = false;
            HandleUtility.Repaint();
        }

        return GUI.Toggle(position, id, value, content, style);
    }

    private static void DrawColorProperty(Rect rect, SerializedProperty property)
    {
        int oldIndent = EditorGUI.indentLevel;
        EditorGUI.indentLevel = 0;
        
        if (EditorGUIUtility.wideMode)
        {
            EditorGUI.PropertyField(new Rect(rect.x, rect.y, 50f, rect.height), property, GUIContent.none);
            rect.x += 50f;
            rect.width = Mathf.Min(100f, rect.width - 55f);
        }
        else
        {
            rect.height /= 2f;
            rect.width = Mathf.Min(100f, rect.width - 5f);
            EditorGUI.PropertyField(rect, property, GUIContent.none);
            rect.y += rect.height;
        }

        EditorGUI.BeginChangeCheck();
        string colorString = EditorGUI.TextField(rect, string.Format("#{0}", ColorUtility.ToHtmlStringRGBA(property.colorValue)));
        if (EditorGUI.EndChangeCheck())
        {
            Color color;
            if (ColorUtility.TryParseHtmlString(colorString, out color))
            {
                property.colorValue = color;
            }
        }
        
        EditorGUI.indentLevel = oldIndent;
    }

    /// <summary>
    /// Draws the most commonly used TextMeshProUGUI properties above the swatch section.
    /// </summary>
    protected override void DrawComponentPropertiesAbove()
    {
        TextInput();
        TextStyle();
        MainSettings();
        FontAsset();
        MaterialPreset();
        FontStyle();
        FontSizeAndAutoSize();
        VertexColor();

        void TextInput()
        {
            // Text Input section header with RTL toggle
            EditorGUILayout.Space();

            Rect rect = EditorGUILayout.GetControlRect(false, 22);
            GUI.Label(rect, new GUIContent("<b>Text Input</b>"), TMPro.EditorUtilities.TMP_UIStyleManager.sectionHeader);

            EditorGUI.indentLevel = 0;

            // Display RTL Toggle
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 110f;

            SerializedProperty isRightToLeftProp = serializedObject.FindProperty("m_isRightToLeft");
            isRightToLeftProp.boolValue = EditorGUI.Toggle(new Rect(rect.width - 120, rect.y + 3, 130, 20), new GUIContent("Enable RTL Editor"), isRightToLeftProp.boolValue);

            EditorGUIUtility.labelWidth = labelWidth;

            // Text property without label
            SerializedProperty textProp = serializedObject.FindProperty("m_text");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(textProp, GUIContent.none);

            if (EditorGUI.EndChangeCheck())
            {
                // Text has been modified
                serializedObject.ApplyModifiedProperties();
            }

            // RTL Text Input (shown when RTL Editor is enabled)
            if (isRightToLeftProp.boolValue)
            {
                // Copy source text to RTL string
                m_RtlText = string.Empty;
                string sourceText = textProp.stringValue;

                // Reverse Text displayed in Text Input Box
                for (int i = 0; i < sourceText.Length; i++)
                    m_RtlText += sourceText[sourceText.Length - i - 1];

                GUILayout.Label("RTL Text Input");

                EditorGUI.BeginChangeCheck();
                m_RtlText = EditorGUILayout.TextArea(m_RtlText, TMPro.EditorUtilities.TMP_UIStyleManager.wrappingTextArea, GUILayout.Height(EditorGUI.GetPropertyHeight(textProp) - EditorGUIUtility.singleLineHeight), GUILayout.ExpandWidth(true));

                if (EditorGUI.EndChangeCheck())
                {
                    // Convert RTL input back to normal text
                    sourceText = string.Empty;

                    // Reverse Text displayed in Text Input Box
                    for (int i = 0; i < m_RtlText.Length; i++)
                        sourceText += m_RtlText[m_RtlText.Length - i - 1];

                    textProp.stringValue = sourceText;
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }

        void TextStyle()
        {
            // TEXT STYLE
            if (m_StyleNames != null)
            {
                Rect rect = EditorGUILayout.GetControlRect(false, 17);

                SerializedProperty textStyleHashCodeProp = serializedObject.FindProperty("m_TextStyleHashCode");
                EditorGUI.BeginProperty(rect, new GUIContent("Text Style"), textStyleHashCodeProp);

                m_TextStyleIndexLookup.TryGetValue(textStyleHashCodeProp.intValue, out m_StyleSelectionIndex);

                EditorGUI.BeginChangeCheck();
                m_StyleSelectionIndex = EditorGUI.Popup(rect, new GUIContent("Text Style"), m_StyleSelectionIndex, m_StyleNames);
                if (EditorGUI.EndChangeCheck())
                {
                    textStyleHashCodeProp.intValue = m_Styles[m_StyleSelectionIndex].hashCode;
                    serializedObject.ApplyModifiedProperties();
                }

                EditorGUI.EndProperty();
            }
        }

        void MainSettings()
        {
            // Main Settings section header
            EditorGUILayout.Space();

            Rect rect = EditorGUILayout.GetControlRect(false, 22);
            GUI.Label(rect, new GUIContent("<b>Main Settings</b>"), TMPro.EditorUtilities.TMP_UIStyleManager.sectionHeader);

            EditorGUI.indentLevel = 0;
        }

        void FontAsset()
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_fontAsset"));
            if (EditorGUI.EndChangeCheck())
            {
                // Font changed - refresh material presets
                m_MaterialPresetNames = GetMaterialPresets();
                m_MaterialPresetSelectionIndex = 0;
            }
        }

        void MaterialPreset()
        {
            if (m_MaterialPresetNames != null)
            {
                SerializedProperty sharedMaterialProp = serializedObject.FindProperty("m_sharedMaterial");
                
                Rect rect = EditorGUILayout.GetControlRect(false, 17);
                EditorGUI.BeginProperty(rect, new GUIContent("Material Preset"), sharedMaterialProp);

                float oldHeight = EditorStyles.popup.fixedHeight;
                EditorStyles.popup.fixedHeight = rect.height;

                int oldSize = EditorStyles.popup.fontSize;
                EditorStyles.popup.fontSize = 11;

                if (sharedMaterialProp.objectReferenceValue != null)
                    m_MaterialPresetIndexLookup.TryGetValue(sharedMaterialProp.objectReferenceValue.GetInstanceID(), out m_MaterialPresetSelectionIndex);

                EditorGUI.BeginChangeCheck();
                m_MaterialPresetSelectionIndex = EditorGUI.Popup(rect, new GUIContent("Material Preset"), m_MaterialPresetSelectionIndex, m_MaterialPresetNames);
                if (EditorGUI.EndChangeCheck())
                {
                    sharedMaterialProp.objectReferenceValue = m_MaterialPresets[m_MaterialPresetSelectionIndex];
                    serializedObject.ApplyModifiedProperties();
                }

                EditorStyles.popup.fixedHeight = oldHeight;
                EditorStyles.popup.fontSize = oldSize;

                EditorGUI.EndProperty();
            }
        }

        void FontStyle()
        {
            SerializedProperty fontStyleProp = serializedObject.FindProperty("m_fontStyle");
            
            EditorGUI.BeginChangeCheck();

            int v1, v2, v3, v4, v5, v6, v7;

            if (EditorGUIUtility.wideMode)
            {
                Rect rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight + 2f);

                EditorGUI.BeginProperty(rect, new GUIContent("Font Style"), fontStyleProp);

                EditorGUI.PrefixLabel(rect, new GUIContent("Font Style"));

                int styleValue = fontStyleProp.intValue;

                rect.x += EditorGUIUtility.labelWidth;
                rect.width -= EditorGUIUtility.labelWidth;

                rect.width = Mathf.Max(25f, rect.width / 7f);

                v1 = EditorToggle(rect, (styleValue & 1) == 1, new GUIContent("B", "Bold"), TMP_UIStyleManager.alignmentButtonLeft) ? 1 : 0; // Bold
                rect.x += rect.width;
                v2 = EditorToggle(rect, (styleValue & 2) == 2, new GUIContent("I", "Italic"), TMP_UIStyleManager.alignmentButtonMid) ? 2 : 0; // Italics
                rect.x += rect.width;
                v3 = EditorToggle(rect, (styleValue & 4) == 4, new GUIContent("U", "Underline"), TMP_UIStyleManager.alignmentButtonMid) ? 4 : 0; // Underline
                rect.x += rect.width;
                v7 = EditorToggle(rect, (styleValue & 64) == 64, new GUIContent("S", "Strikethrough"), TMP_UIStyleManager.alignmentButtonRight) ? 64 : 0; // Strikethrough
                rect.x += rect.width;

                int selected = 0;

                EditorGUI.BeginChangeCheck();
                v4 = EditorToggle(rect, (styleValue & 8) == 8, new GUIContent("ab", "Lowercase"), TMP_UIStyleManager.alignmentButtonLeft) ? 8 : 0; // Lowercase
                if (EditorGUI.EndChangeCheck() && v4 > 0)
                {
                    selected = v4;
                }
                rect.x += rect.width;
                EditorGUI.BeginChangeCheck();
                v5 = EditorToggle(rect, (styleValue & 16) == 16, new GUIContent("AB", "Uppercase"), TMP_UIStyleManager.alignmentButtonMid) ? 16 : 0; // Uppercase
                if (EditorGUI.EndChangeCheck() && v5 > 0)
                {
                    selected = v5;
                }
                rect.x += rect.width;
                EditorGUI.BeginChangeCheck();
                v6 = EditorToggle(rect, (styleValue & 32) == 32, new GUIContent("SC", "Smallcaps"), TMP_UIStyleManager.alignmentButtonRight) ? 32 : 0; // Smallcaps
                if (EditorGUI.EndChangeCheck() && v6 > 0)
                {
                    selected = v6;
                }

                if (selected > 0)
                {
                    v4 = selected == 8 ? 8 : 0;
                    v5 = selected == 16 ? 16 : 0;
                    v6 = selected == 32 ? 32 : 0;
                }

                EditorGUI.EndProperty();
            }
            else
            {
                Rect rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight + 2f);

                EditorGUI.BeginProperty(rect, new GUIContent("Font Style"), fontStyleProp);

                EditorGUI.PrefixLabel(rect, new GUIContent("Font Style"));

                int styleValue = fontStyleProp.intValue;

                rect.x += EditorGUIUtility.labelWidth;
                rect.width -= EditorGUIUtility.labelWidth;
                rect.width = Mathf.Max(25f, rect.width / 4f);

                v1 = EditorToggle(rect, (styleValue & 1) == 1, new GUIContent("B", "Bold"), TMP_UIStyleManager.alignmentButtonLeft) ? 1 : 0; // Bold
                rect.x += rect.width;
                v2 = EditorToggle(rect, (styleValue & 2) == 2, new GUIContent("I", "Italic"), TMP_UIStyleManager.alignmentButtonMid) ? 2 : 0; // Italics
                rect.x += rect.width;
                v3 = EditorToggle(rect, (styleValue & 4) == 4, new GUIContent("U", "Underline"), TMP_UIStyleManager.alignmentButtonMid) ? 4 : 0; // Underline
                rect.x += rect.width;
                v7 = EditorToggle(rect, (styleValue & 64) == 64, new GUIContent("S", "Strikethrough"), TMP_UIStyleManager.alignmentButtonRight) ? 64 : 0; // Strikethrough

                rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight + 2f);

                rect.x += EditorGUIUtility.labelWidth;
                rect.width -= EditorGUIUtility.labelWidth;

                rect.width = Mathf.Max(25f, rect.width / 4f);

                int selected = 0;

                EditorGUI.BeginChangeCheck();
                v4 = EditorToggle(rect, (styleValue & 8) == 8, new GUIContent("ab", "Lowercase"), TMP_UIStyleManager.alignmentButtonLeft) ? 8 : 0; // Lowercase
                if (EditorGUI.EndChangeCheck() && v4 > 0)
                {
                    selected = v4;
                }
                rect.x += rect.width;
                EditorGUI.BeginChangeCheck();
                v5 = EditorToggle(rect, (styleValue & 16) == 16, new GUIContent("AB", "Uppercase"), TMP_UIStyleManager.alignmentButtonMid) ? 16 : 0; // Uppercase
                if (EditorGUI.EndChangeCheck() && v5 > 0)
                {
                    selected = v5;
                }
                rect.x += rect.width;
                EditorGUI.BeginChangeCheck();
                v6 = EditorToggle(rect, (styleValue & 32) == 32, new GUIContent("SC", "Smallcaps"), TMP_UIStyleManager.alignmentButtonRight) ? 32 : 0; // Smallcaps
                if (EditorGUI.EndChangeCheck() && v6 > 0)
                {
                    selected = v6;
                }

                if (selected > 0)
                {
                    v4 = selected == 8 ? 8 : 0;
                    v5 = selected == 16 ? 16 : 0;
                    v6 = selected == 32 ? 32 : 0;
                }

                EditorGUI.EndProperty();
            }

            if (EditorGUI.EndChangeCheck())
            {
                fontStyleProp.intValue = v1 + v2 + v3 + v4 + v5 + v6 + v7;
                serializedObject.ApplyModifiedProperties();
            }
        }

        void FontSizeAndAutoSize()
        {
            SerializedProperty fontSizeProp = serializedObject.FindProperty("m_fontSize");
            SerializedProperty fontSizeBaseProp = serializedObject.FindProperty("m_fontSizeBase");
            SerializedProperty autoSizingProp = serializedObject.FindProperty("m_enableAutoSizing");

            // FONT SIZE
            EditorGUI.BeginChangeCheck();

            EditorGUI.BeginDisabledGroup(autoSizingProp.boolValue);
            EditorGUILayout.PropertyField(fontSizeProp, new GUIContent("Font Size"), GUILayout.MaxWidth(EditorGUIUtility.labelWidth + 50f));
            EditorGUI.EndDisabledGroup();

            if (EditorGUI.EndChangeCheck())
            {
                float fontSize = Mathf.Clamp(fontSizeProp.floatValue, 0, 32767);
                fontSizeProp.floatValue = fontSize;
                fontSizeBaseProp.floatValue = fontSize;
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.indentLevel += 1;

            // AUTO SIZE
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(autoSizingProp, new GUIContent("Auto Size"));
            if (EditorGUI.EndChangeCheck())
            {
                if (autoSizingProp.boolValue == false)
                    fontSizeProp.floatValue = fontSizeBaseProp.floatValue;

                serializedObject.ApplyModifiedProperties();
            }

            // AUTO SIZE OPTIONS (shown when Auto Size is enabled)
            if (autoSizingProp.boolValue)
            {
                SerializedProperty fontSizeMinProp = serializedObject.FindProperty("m_fontSizeMin");
                SerializedProperty fontSizeMaxProp = serializedObject.FindProperty("m_fontSizeMax");
                SerializedProperty charWidthMaxAdjProp = serializedObject.FindProperty("m_charWidthMaxAdj");
                SerializedProperty lineSpacingMaxProp = serializedObject.FindProperty("m_lineSpacingMax");

                Rect rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);

                EditorGUI.PrefixLabel(rect, new GUIContent("Auto Size Options"));

                int previousIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;

                rect.width = (rect.width - EditorGUIUtility.labelWidth) / 4f;
                rect.x += EditorGUIUtility.labelWidth;

                // Min
                EditorGUIUtility.labelWidth = 24;
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(rect, fontSizeMinProp, new GUIContent("Min"));
                if (EditorGUI.EndChangeCheck())
                {
                    float minSize = Mathf.Max(0, fontSizeMinProp.floatValue);
                    fontSizeMinProp.floatValue = Mathf.Min(minSize, fontSizeMaxProp.floatValue);
                    serializedObject.ApplyModifiedProperties();
                }
                rect.x += rect.width;

                // Max
                EditorGUIUtility.labelWidth = 27;
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(rect, fontSizeMaxProp, new GUIContent("Max"));
                if (EditorGUI.EndChangeCheck())
                {
                    float maxSize = Mathf.Clamp(fontSizeMaxProp.floatValue, 0, 32767);
                    fontSizeMaxProp.floatValue = Mathf.Max(fontSizeMinProp.floatValue, maxSize);
                    serializedObject.ApplyModifiedProperties();
                }
                rect.x += rect.width;

                // WD%
                EditorGUI.BeginChangeCheck();
                EditorGUIUtility.labelWidth = 36;
                EditorGUI.PropertyField(rect, charWidthMaxAdjProp, new GUIContent("WD%"));
                rect.x += rect.width;

                // Line
                EditorGUIUtility.labelWidth = 28;
                EditorGUI.PropertyField(rect, lineSpacingMaxProp, new GUIContent("Line"));

                EditorGUIUtility.labelWidth = 0;

                if (EditorGUI.EndChangeCheck())
                {
                    charWidthMaxAdjProp.floatValue = Mathf.Clamp(charWidthMaxAdjProp.floatValue, 0, 50);
                    lineSpacingMaxProp.floatValue = Mathf.Min(0, lineSpacingMaxProp.floatValue);
                    serializedObject.ApplyModifiedProperties();
                }

                EditorGUI.indentLevel = previousIndent;
            }

            EditorGUI.indentLevel -= 1;
        }
    
        void VertexColor()
        {
            // VERTEX COLOR
            SerializedProperty fontColorProp = serializedObject.FindProperty("m_fontColor");
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(fontColorProp, new GUIContent("Vertex Color", "The base color of the text vertices."));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
    }

    /// <summary>
    /// Draws secondary TextMeshProUGUI properties below the swatch section.
    /// </summary>
    protected override void DrawComponentPropertiesBelow()
    {
        ColorGradient();
        OverrideTags();
        SpacingOptions();
        Alignment();
        WrappingOverflow();
        UVMapping();
        ExtraSettings();

        void ColorGradient()
        {
            SerializedProperty enableVertexGradientProp = serializedObject.FindProperty("m_enableVertexGradient");
            SerializedProperty fontColorGradientProp = serializedObject.FindProperty("m_fontColorGradient");
            SerializedProperty fontColorGradientPresetProp = serializedObject.FindProperty("m_fontColorGradientPreset");
            SerializedProperty colorModeProp = serializedObject.FindProperty("m_colorMode");

            // COLOR GRADIENT CHECKBOX
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(enableVertexGradientProp, new GUIContent("Color Gradient", "The gradient color applied over the Vertex Color. Can be locally set or driven by a Gradient Asset."));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUIUtility.fieldWidth = 0;

            if (enableVertexGradientProp.boolValue)
            {
                EditorGUI.indentLevel += 1;

                EditorGUI.BeginChangeCheck();

                // COLOR PRESET
                EditorGUILayout.PropertyField(fontColorGradientPresetProp, new GUIContent("Color Preset", "A Color Preset which override the local color settings."));

                SerializedObject obj = null;
                SerializedProperty colorMode;
                SerializedProperty topLeft;
                SerializedProperty topRight;
                SerializedProperty bottomLeft;
                SerializedProperty bottomRight;

                if (fontColorGradientPresetProp.objectReferenceValue == null)
                {
                    colorMode = colorModeProp;
                    topLeft = fontColorGradientProp.FindPropertyRelative("topLeft");
                    topRight = fontColorGradientProp.FindPropertyRelative("topRight");
                    bottomLeft = fontColorGradientProp.FindPropertyRelative("bottomLeft");
                    bottomRight = fontColorGradientProp.FindPropertyRelative("bottomRight");
                }
                else
                {
                    obj = new SerializedObject(fontColorGradientPresetProp.objectReferenceValue);
                    colorMode = obj.FindProperty("colorMode");
                    topLeft = obj.FindProperty("topLeft");
                    topRight = obj.FindProperty("topRight");
                    bottomLeft = obj.FindProperty("bottomLeft");
                    bottomRight = obj.FindProperty("bottomRight");
                }

                // COLOR MODE
                EditorGUILayout.PropertyField(colorMode, new GUIContent("Color Mode", "The type of gradient to use."));

                // COLORS (based on Color Mode)
                Rect rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight * (EditorGUIUtility.wideMode ? 1 : 2));

                EditorGUI.PrefixLabel(rect, new GUIContent("Colors", "The color composition of the gradient."));

                rect.x += EditorGUIUtility.labelWidth;
                rect.width = rect.width - EditorGUIUtility.labelWidth;

                switch ((ColorMode)colorMode.enumValueIndex)
                {
                    case ColorMode.Single:
                        DrawColorProperty(rect, topLeft);

                        topRight.colorValue = topLeft.colorValue;
                        bottomLeft.colorValue = topLeft.colorValue;
                        bottomRight.colorValue = topLeft.colorValue;
                        break;

                    case ColorMode.HorizontalGradient:
                        rect.width /= 2f;

                        DrawColorProperty(rect, topLeft);

                        rect.x += rect.width;

                        DrawColorProperty(rect, topRight);

                        bottomLeft.colorValue = topLeft.colorValue;
                        bottomRight.colorValue = topRight.colorValue;
                        break;

                    case ColorMode.VerticalGradient:
                        DrawColorProperty(rect, topLeft);

                        rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * (EditorGUIUtility.wideMode ? 1 : 2));
                        rect.x += EditorGUIUtility.labelWidth;

                        DrawColorProperty(rect, bottomLeft);

                        topRight.colorValue = topLeft.colorValue;
                        bottomRight.colorValue = bottomLeft.colorValue;
                        break;

                    case ColorMode.FourCornersGradient:
                        rect.width /= 2f;

                        DrawColorProperty(rect, topLeft);

                        rect.x += rect.width;

                        DrawColorProperty(rect, topRight);

                        rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight * (EditorGUIUtility.wideMode ? 1 : 2));
                        rect.x += EditorGUIUtility.labelWidth;
                        rect.width = (rect.width - EditorGUIUtility.labelWidth) / 2f;

                        DrawColorProperty(rect, bottomLeft);

                        rect.x += rect.width;

                        DrawColorProperty(rect, bottomRight);
                        break;
                }

                if (EditorGUI.EndChangeCheck())
                {
                    if (obj != null)
                    {
                        obj.ApplyModifiedProperties();
                        TMPro_EventManager.ON_COLOR_GRADIENT_PROPERTY_CHANGED(fontColorGradientPresetProp.objectReferenceValue as TMP_ColorGradient);
                    }
                    serializedObject.ApplyModifiedProperties();
                }

                EditorGUI.indentLevel -= 1;
            }
        }

        void OverrideTags()
        {
            // OVERRIDE TAGS
            SerializedProperty overrideHtmlColorsProp = serializedObject.FindProperty("m_overrideHtmlColors");
            
            EditorGUILayout.PropertyField(overrideHtmlColorsProp, new GUIContent("Override Tags", "Whether the color settings override the <color> tag."));

            EditorGUILayout.Space();
        }

        void SpacingOptions()
        {
            // CHARACTER, LINE & PARAGRAPH SPACING
            EditorGUI.BeginChangeCheck();

            Rect rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);

            EditorGUI.PrefixLabel(rect, new GUIContent("Spacing Options (em)", "Spacing adjustments between different elements of the text. Values are in font units where a value of 1 equals 1/100em."));

            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            float currentLabelWidth = EditorGUIUtility.labelWidth;
            rect.x += currentLabelWidth;
            rect.width = (rect.width - currentLabelWidth - 3f) / 2f;

            EditorGUIUtility.labelWidth = Mathf.Min(rect.width * 0.55f, 80f);

            SerializedProperty characterSpacingProp = serializedObject.FindProperty("m_characterSpacing");
            SerializedProperty wordSpacingProp = serializedObject.FindProperty("m_wordSpacing");
            SerializedProperty lineSpacingProp = serializedObject.FindProperty("m_lineSpacing");
            SerializedProperty paragraphSpacingProp = serializedObject.FindProperty("m_paragraphSpacing");

            EditorGUI.PropertyField(rect, characterSpacingProp, new GUIContent("Character"));
            rect.x += rect.width + 3f;
            EditorGUI.PropertyField(rect, wordSpacingProp, new GUIContent("Word"));

            rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

            rect.x += currentLabelWidth;
            rect.width = (rect.width - currentLabelWidth - 3f) / 2f;
            EditorGUIUtility.labelWidth = Mathf.Min(rect.width * 0.55f, 80f);

            EditorGUI.PropertyField(rect, lineSpacingProp, new GUIContent("Line"));
            rect.x += rect.width + 3f;
            EditorGUI.PropertyField(rect, paragraphSpacingProp, new GUIContent("Paragraph"));

            EditorGUIUtility.labelWidth = currentLabelWidth;
            EditorGUI.indentLevel = oldIndent;

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.Space();
        }

        void Alignment()
        {
            // TEXT ALIGNMENT
            EditorGUI.BeginChangeCheck();

            Rect rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.currentViewWidth > 504 ? 20 : 40 + 3);
            
            SerializedProperty horizontalAlignmentProp = serializedObject.FindProperty("m_HorizontalAlignment");
            SerializedProperty verticalAlignmentProp = serializedObject.FindProperty("m_VerticalAlignment");
            
            EditorGUI.BeginProperty(rect, new GUIContent("Alignment", "Horizontal and vertical alignment of the text within its container."), horizontalAlignmentProp);
            EditorGUI.BeginProperty(rect, new GUIContent("Alignment", "Horizontal and vertical alignment of the text within its container."), verticalAlignmentProp);

            EditorGUI.PrefixLabel(rect, new GUIContent("Alignment", "Horizontal and vertical alignment of the text within its container."));
            rect.x += EditorGUIUtility.labelWidth;

            EditorGUI.PropertyField(rect, horizontalAlignmentProp, GUIContent.none);
            EditorGUI.PropertyField(rect, verticalAlignmentProp, GUIContent.none);

            // WRAPPING RATIOS shown if Justified or Flush mode is selected.
            if (((HorizontalAlignmentOptions)horizontalAlignmentProp.intValue & HorizontalAlignmentOptions.Justified) == HorizontalAlignmentOptions.Justified || 
                ((HorizontalAlignmentOptions)horizontalAlignmentProp.intValue & HorizontalAlignmentOptions.Flush) == HorizontalAlignmentOptions.Flush)
            {
                SerializedProperty wordWrappingRatiosProp = serializedObject.FindProperty("m_wordWrappingRatios");
                
                Rect sliderRect = EditorGUILayout.GetControlRect(false, 17);
                EditorGUI.Slider(sliderRect, wordWrappingRatiosProp, 0.0f, 1.0f, new GUIContent("Wrap Mix (W <-> C)", "How much to favor words versus characters when distributing the text."));
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();
            EditorGUI.EndProperty();

            EditorGUILayout.Space();
        }

        void WrappingOverflow()
        {
            // TEXT WRAPPING
            SerializedProperty textWrappingModeProp = serializedObject.FindProperty("m_TextWrappingMode");
            
            Rect rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginProperty(rect, new GUIContent("Wrapping", "Wraps text to the next line when reaching the edge of the container."), textWrappingModeProp);

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(rect, textWrappingModeProp);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndProperty();

            // TEXT OVERFLOW
            SerializedProperty textOverflowModeProp = serializedObject.FindProperty("m_overflowMode");
            
            EditorGUI.BeginChangeCheck();

            if ((TextOverflowModes)textOverflowModeProp.enumValueIndex == TextOverflowModes.Linked)
            {
                SerializedProperty linkedTextComponentProp = serializedObject.FindProperty("m_linkedTextComponent");
                
                EditorGUILayout.BeginHorizontal();

                float fieldWidth = EditorGUIUtility.fieldWidth;
                EditorGUIUtility.fieldWidth = 65;
                EditorGUILayout.PropertyField(textOverflowModeProp, new GUIContent("Overflow", "How to display text which goes past the edge of the container."));
                EditorGUIUtility.fieldWidth = fieldWidth;

                EditorGUILayout.PropertyField(linkedTextComponentProp, GUIContent.none);

                EditorGUILayout.EndHorizontal();
            }
            else if ((TextOverflowModes)textOverflowModeProp.enumValueIndex == TextOverflowModes.Page)
            {
                SerializedProperty pageToDisplayProp = serializedObject.FindProperty("m_pageToDisplay");
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(textOverflowModeProp, new GUIContent("Overflow", "How to display text which goes past the edge of the container."));
                EditorGUILayout.PropertyField(pageToDisplayProp, GUIContent.none);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.PropertyField(textOverflowModeProp, new GUIContent("Overflow", "How to display text which goes past the edge of the container."));
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.Space();
        }

        void UVMapping()
        {
            // TEXTURE MAPPING OPTIONS
            SerializedProperty horizontalMappingProp = serializedObject.FindProperty("m_horizontalMapping");
            SerializedProperty verticalMappingProp = serializedObject.FindProperty("m_verticalMapping");
            
            EditorGUI.BeginChangeCheck();
            
            EditorGUILayout.PropertyField(horizontalMappingProp, new GUIContent("Horizontal Mapping", "Horizontal UV mapping when using a shader with a texture face option."));
            EditorGUILayout.PropertyField(verticalMappingProp, new GUIContent("Vertical Mapping", "Vertical UV mapping when using a shader with a texture face option."));
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            // UV OPTIONS - Line Offset shown if Horizontal Mapping is not Character (enumValueIndex > 0)
            if (horizontalMappingProp.enumValueIndex > 0)
            {
                SerializedProperty uvLineOffsetProp = serializedObject.FindProperty("m_uvLineOffset");
                
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(uvLineOffsetProp, new GUIContent("Line Offset", "Adds an horizontal offset to each successive line. Used for slanted texturing."), GUILayout.MinWidth(70f));
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }

            EditorGUILayout.Space();
        }

        void ExtraSettings()
        {
            // EXTRA SETTINGS SECTION HEADER WITH FOLDOUT
            Rect rect = EditorGUILayout.GetControlRect(false, 24);

            if (GUI.Button(rect, new GUIContent("<b>Extra Settings</b>"), TMP_UIStyleManager.sectionHeader))
                s_ExtraSettingsFoldout = !s_ExtraSettingsFoldout;

            GUI.Label(rect, s_ExtraSettingsFoldout ? k_UiStateLabel[0] : k_UiStateLabel[1], TMP_UIStyleManager.rightLabel);

            if (s_ExtraSettingsFoldout)
            {
                Margins();
                GeometrySorting();
                IsScaleStatic();
                RichText();
                RaycastTarget();
                Maskable();
                Parsing();
                EmojiFallbackSupport();
                SpriteAsset();
                StyleSheetAsset();
                FontFeatures();
                ExtraPadding();
            }

            void Margins()
            {
                SerializedProperty marginProp = serializedObject.FindProperty("m_margin");
                
                EditorGUI.BeginChangeCheck();
                
                // Two rows: labels on top, values below
                Rect rect = EditorGUILayout.GetControlRect(false, 2 * 18);
                EditorGUI.BeginProperty(rect, new GUIContent("Margins", "The space between the text and the edge of its container."), marginProp);
                
                Rect pos0 = new Rect(rect.x, rect.y + 2, rect.width - 15, 18);
                
                float width = rect.width + 3;
                pos0.width = EditorGUIUtility.labelWidth;
                EditorGUI.PrefixLabel(pos0, new GUIContent("Margins"));
                
                Vector4 margins = marginProp.vector4Value;
                
                float widthB = width - EditorGUIUtility.labelWidth;
                float fieldWidth = widthB / 4;
                pos0.width = Mathf.Max(fieldWidth - 5, 45f);
                
                // Draw each margin field with label above value
                pos0.x = EditorGUIUtility.labelWidth + 15;
                margins.x = DrawMarginField(pos0, "Left", margins.x);
                
                pos0.x += fieldWidth;
                margins.y = DrawMarginField(pos0, "Top", margins.y);
                
                pos0.x += fieldWidth;
                margins.z = DrawMarginField(pos0, "Right", margins.z);
                
                pos0.x += fieldWidth;
                margins.w = DrawMarginField(pos0, "Bottom", margins.w);
                
                if (EditorGUI.EndChangeCheck())
                {
                    // Clamp margins to reasonable values
                    TextMeshProUGUI tmpText = (TextMeshProUGUI)target;
                    RectTransform rectTransform = tmpText.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        Rect textContainerSize = rectTransform.rect;
                        margins.x = Mathf.Clamp(margins.x, -textContainerSize.width, textContainerSize.width);
                        margins.z = Mathf.Clamp(margins.z, -textContainerSize.width, textContainerSize.width);
                        margins.y = Mathf.Clamp(margins.y, -textContainerSize.height, textContainerSize.height);
                        margins.w = Mathf.Clamp(margins.w, -textContainerSize.height, textContainerSize.height);
                    }
                    
                    marginProp.vector4Value = margins;
                    serializedObject.ApplyModifiedProperties();
                }
                
                EditorGUI.EndProperty();
                EditorGUILayout.Space();
            }
            
            float DrawMarginField(Rect position, string label, float value)
            {
                int controlId = GUIUtility.GetControlID(FocusType.Keyboard, position);
                
                // Draw the label in the top area which will act as drag zone
                Rect labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                EditorGUI.PrefixLabel(labelRect, controlId, new GUIContent(label));
                
                // Add cursor rect for the label area to show horizontal arrows
                EditorGUIUtility.AddCursorRect(labelRect, MouseCursor.SlideArrow);
                
                // Field area below the label
                Rect fieldRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, EditorGUIUtility.singleLineHeight);
                
                // Handle dragging on the label
                Event evt = Event.current;
                if (evt.type == EventType.MouseDown && labelRect.Contains(evt.mousePosition))
                {
                    GUIUtility.hotControl = controlId;
                    GUIUtility.keyboardControl = controlId;
                    evt.Use();
                }
                else if (evt.type == EventType.MouseDrag && GUIUtility.hotControl == controlId)
                {
                    value += evt.delta.x * 0.1f; // Adjust sensitivity
                    GUI.changed = true;
                    evt.Use();
                }
                else if (evt.type == EventType.MouseUp && GUIUtility.hotControl == controlId)
                {
                    GUIUtility.hotControl = 0;
                    evt.Use();
                }
                
                // Draw the float field
                EditorGUI.BeginChangeCheck();
                float newValue = EditorGUI.FloatField(fieldRect, GUIContent.none, value);
                if (EditorGUI.EndChangeCheck())
                {
                    value = newValue;
                }
                
                return value;
            }

            void GeometrySorting()
            {
                SerializedProperty geometrySortingOrderProp = serializedObject.FindProperty("m_geometrySortingOrder");
                
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(geometrySortingOrderProp, new GUIContent("Geometry Sorting", "The order in which text geometry is sorted. Used to adjust the way overlapping characters are displayed."));
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                }
                
                EditorGUILayout.Space();
            }

            void IsScaleStatic()
            {
                SerializedProperty isTextObjectScaleStaticProp = serializedObject.FindProperty("m_IsTextObjectScaleStatic");
                
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(isTextObjectScaleStaticProp, new GUIContent("Is Scale Static", "Controls whether a text object will be excluded from the InternalUpdate callback to handle scale changes of the text object or its parent(s)."));
                if (EditorGUI.EndChangeCheck())
                {
                    TextMeshProUGUI tmpText = (TextMeshProUGUI)target;
                    tmpText.isTextObjectScaleStatic = isTextObjectScaleStaticProp.boolValue;
                    serializedObject.ApplyModifiedProperties();
                }
                
                EditorGUILayout.Space();
            }

            void RichText()
            {
                SerializedProperty isRichTextProp = serializedObject.FindProperty("m_isRichText");
                
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(isRichTextProp, new GUIContent("Rich Text", "Enables the use of rich text tags such as <color> and <font>."));
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }

            void RaycastTarget()
            {
                SerializedProperty raycastTargetProp = serializedObject.FindProperty("m_RaycastTarget");
                
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(raycastTargetProp, new GUIContent("Raycast Target", "Whether the text blocks raycasts from the Graphic Raycaster."));
                if (EditorGUI.EndChangeCheck())
                {
                    // Change needs to propagate to the child sub objects.
                    TextMeshProUGUI tmpText = (TextMeshProUGUI)target;
                    UnityEngine.UI.Graphic[] graphicComponents = tmpText.GetComponentsInChildren<UnityEngine.UI.Graphic>();
                    for (int i = 1; i < graphicComponents.Length; i++)
                        graphicComponents[i].raycastTarget = raycastTargetProp.boolValue;
                    
                    serializedObject.ApplyModifiedProperties();
                }
            }

            void Maskable()
            {
                SerializedProperty maskableProp = serializedObject.FindProperty("m_Maskable");
                if (maskableProp == null)
                    return;
                
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(maskableProp, new GUIContent("Maskable", "Determines if the text object will be affected by UI Mask."));
                if (EditorGUI.EndChangeCheck())
                {
                    TextMeshProUGUI tmpText = (TextMeshProUGUI)target;
                    tmpText.maskable = maskableProp.boolValue;
                    
                    // Change needs to propagate to the child sub objects.
                    UnityEngine.UI.MaskableGraphic[] maskableGraphics = tmpText.GetComponentsInChildren<UnityEngine.UI.MaskableGraphic>();
                    for (int i = 1; i < maskableGraphics.Length; i++)
                        maskableGraphics[i].maskable = maskableProp.boolValue;
                    
                    serializedObject.ApplyModifiedProperties();
                }
            }

            void Parsing()
            {
                SerializedProperty parseCtrlCharactersProp = serializedObject.FindProperty("m_parseCtrlCharacters");
                SerializedProperty useMaxVisibleDescenderProp = serializedObject.FindProperty("m_useMaxVisibleDescender");
                
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(parseCtrlCharactersProp, new GUIContent("Parse Escape Characters", "Whether to display strings such as \"\\n\" as is or replace them by the character they represent."));
                EditorGUILayout.PropertyField(useMaxVisibleDescenderProp, new GUIContent("Visible Descender", "Compute descender values from visible characters only. Used to adjust layout behavior when hiding and revealing characters dynamically."));
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                }
                
                EditorGUILayout.Space();
            }

            void EmojiFallbackSupport()
            {
                SerializedProperty emojiFallbackSupportProp = serializedObject.FindProperty("m_EmojiFallbackSupport");
                
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(emojiFallbackSupportProp, new GUIContent("Emoji Fallback", "Enables fallback to emoji fonts when the current font doesn't support emoji characters."));
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }

            void SpriteAsset()
            {
                SerializedProperty spriteAssetProp = serializedObject.FindProperty("m_spriteAsset");
                
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(spriteAssetProp, new GUIContent("Sprite Asset", "The Sprite Asset to use for <sprite> tags."), true);
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                }
                
                EditorGUILayout.Space();
            }

            void StyleSheetAsset()
            {
                SerializedProperty styleSheetAssetProp = serializedObject.FindProperty("m_StyleSheet");
                
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(styleSheetAssetProp, new GUIContent("Style Sheet Asset", "The Style Sheet Asset to use for <style> tags."), true);
                if (EditorGUI.EndChangeCheck())
                {
                    m_StyleNames = GetStyleNames();
                    serializedObject.ApplyModifiedProperties();
                }
                
                EditorGUILayout.Space();
            }

            void FontFeatures()
            {
                SerializedProperty fontFeaturesActiveProp = serializedObject.FindProperty("m_ActiveFontFeatures");
                
                if (fontFeaturesActiveProp == null)
                    return;
                
                // Define available font features
                string[] k_FontFeatures = new string[] {
                    "kern", "liga", "dlig", "smcp", "c2sc", "case", "cpsp", "tnum", "lnum", "onum", "pnum",
                    "frac", "subs", "sups", "ordn", "zero", "ss01", "ss02", "ss03", "ss04", "ss05", "ss06", "ss07"
                };
                
                int srcMask = 0;
                int featureCount = fontFeaturesActiveProp.arraySize;
                
                for (int i = 0; i < featureCount; i++)
                {
                    SerializedProperty activeFeatureProperty = fontFeaturesActiveProp.GetArrayElementAtIndex(i);
                    int featureValue = activeFeatureProperty.intValue;
                    
                    for (int j = 0; j < k_FontFeatures.Length; j++)
                    {
                        // Convert string to int tag
                        int tagValue = (k_FontFeatures[j][0] << 24) | (k_FontFeatures[j][1] << 16) | 
                                      (k_FontFeatures[j].Length > 2 ? k_FontFeatures[j][2] << 8 : 0) | 
                                      (k_FontFeatures[j].Length > 3 ? k_FontFeatures[j][3] : 0);
                        
                        if (featureValue == tagValue)
                        {
                            srcMask |= 0x1 << j;
                            break;
                        }
                    }
                }
                
                EditorGUI.BeginChangeCheck();
                int mask = EditorGUILayout.MaskField(new GUIContent("Font Features", "OpenType font features to enable."), srcMask, k_FontFeatures);
                if (EditorGUI.EndChangeCheck())
                {
                    fontFeaturesActiveProp.ClearArray();
                    
                    int writeIndex = 0;
                    for (int i = 0; i < k_FontFeatures.Length; i++)
                    {
                        int bit = 0x1 << i;
                        if ((mask & bit) == bit)
                        {
                            fontFeaturesActiveProp.InsertArrayElementAtIndex(writeIndex);
                            SerializedProperty newFeature = fontFeaturesActiveProp.GetArrayElementAtIndex(writeIndex);
                            
                            // Convert string to int tag
                            int tagValue = (k_FontFeatures[i][0] << 24) | (k_FontFeatures[i][1] << 16) | 
                                          (k_FontFeatures[i].Length > 2 ? k_FontFeatures[i][2] << 8 : 0) | 
                                          (k_FontFeatures[i].Length > 3 ? k_FontFeatures[i][3] : 0);
                            
                            newFeature.intValue = tagValue;
                            writeIndex += 1;
                        }
                    }
                    
                    serializedObject.ApplyModifiedProperties();
                }
            }

            void ExtraPadding()
            {
                SerializedProperty enableExtraPaddingProp = serializedObject.FindProperty("m_enableExtraPadding");
                SerializedProperty checkPaddingRequiredProp = serializedObject.FindProperty("m_checkPaddingRequired");
                
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(enableExtraPaddingProp, new GUIContent("Extra Padding", "Adds some padding between the characters and the edge of the text mesh. Can reduce graphical errors when displaying small text."));
                if (EditorGUI.EndChangeCheck())
                {
                    if (checkPaddingRequiredProp != null)
                        checkPaddingRequiredProp.boolValue = true;
                    
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }

    /// <summary>
    /// Gets the SwatchColorReference component from the current TextMeshProUGUI target.
    /// </summary>
    protected override SwatchColorReference GetCurrentSwatchReference()
    {
        if (target != null)
        {
            TextMeshProUGUI tmpText = (TextMeshProUGUI)target;
            return tmpText.GetComponent<SwatchColorReference>();
        }
        return null;
    }

    /// <summary>
    /// Auto-assigns swatch 0 to new TextMeshProUGUI components that don't have swatch references.
    /// </summary>
    protected override void AutoAssignDefaultSwatch()
    {
        // Only auto-assign if we have a valid color palette
        if (colorPalette == null || colorPalette.colors == null || colorPalette.colors.Length == 0)
            return;

        foreach (var t in targets)
        {
            TextMeshProUGUI tmpText = (TextMeshProUGUI)t;

            if (tmpText.TryGetComponent<SwatchColorReference>(out var existingRef))
            {
                // Check if this existing reference is in our list
                if (!SwatchRefs.Contains(existingRef))
                {
                    // This is a copied/duplicated object - add it to our list
                    SwatchRefs.Add(existingRef);
                    EditorUtility.SetDirty(existingRef);
                    EditorUtility.SetDirty(tmpText);
                }
            }
            else
            {
                // No SwatchColorReference - create new one and assign swatch 0
                SwatchColorReference newSwatchRef = CreateSwatchColorReference(tmpText);

                // Set to swatch 0 and apply the color
                newSwatchRef.SetSwatchIndexAndApplyColor(0);
                EditorUtility.SetDirty(newSwatchRef);
                EditorUtility.SetDirty(tmpText);
            }
        }
    }

    /// <summary>
    /// Checks if the TextMeshProUGUI's color has been manually changed outside the swatch system.
    /// </summary>
    protected override bool HasColorChangedManually()
    {
        TextMeshProUGUI tmpText = (TextMeshProUGUI)target;
        SwatchColorReference swatchRef = tmpText.GetComponent<SwatchColorReference>();
        
        if (swatchRef == null || swatchRef.GetSwatchIndex() < 0)
            return false;

        Color swatchColor = swatchRef.ColorFromPalette();
        return tmpText.color != swatchColor;
    }
}