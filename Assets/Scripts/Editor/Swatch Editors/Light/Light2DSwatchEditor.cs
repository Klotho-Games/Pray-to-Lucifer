using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Rendering;
using UnityEditor.Rendering;
using UnityEngine.UI;
using UnityEditor.EditorTools;

/// <summary>
/// Custom editor for Light2D that integrates swatch-based color management with 2D-optimized property display.
/// 
/// Inherits from SwatchEditorBase to reuse all swatch functionality while adding Light2D-specific
/// property handling and behavior.
/// </summary>
[CustomEditor(typeof(Light2D))]
public class Light2DSwatchEditor : SwatchEditorBase
{
    private SerializedProperty m_ApplyToSortingLayers;
    private SortingLayer[] m_AllSortingLayers;
    private GUIContent[] m_AllSortingLayerNames;
    private List<int> m_ApplyToSortingLayersList;
    private bool m_BlendingSettingsFoldout = true;
    private bool m_ShadowsSettingsFoldout = false;
    private bool m_VolumetricSettingsFoldout = false;
    private bool m_NormalMapsSettingsFoldout = false;
    
    /// <summary>
    /// Draws the most commonly used Light2D properties above the swatch section.
    /// </summary>
    protected override void DrawComponentPropertiesAbove()
    {
        LightType();
        Color();

        void LightType()
        {
            // Custom UI that matches Unity's Light2D editor
            SerializedProperty lightTypeProp = serializedObject.FindProperty("m_LightType");
            
            // Define the light type options with icons (excluding deprecated Parametric)
            GUIContent[] lightTypeOptions = new GUIContent[]
            {
                new GUIContent("Freeform", Resources.Load("InspectorIcons/FreeformLight") as Texture),
                new GUIContent("Sprite", Resources.Load("InspectorIcons/SpriteLight") as Texture),
                new GUIContent("Spot", Resources.Load("InspectorIcons/PointLight") as Texture),
                new GUIContent("Global", Resources.Load("InspectorIcons/GlobalLight") as Texture)
            };
            
            Rect lightTypeRect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginProperty(lightTypeRect, GUIContent.none, lightTypeProp);
            EditorGUI.BeginChangeCheck();
            
            // Subtract 1 from intValue to account for deprecated Parametric (index 0)
            int newLightType = EditorGUI.Popup(lightTypeRect, new GUIContent("Light Type"), lightTypeProp.intValue - 1, lightTypeOptions);
            
            if (EditorGUI.EndChangeCheck())
            {
                lightTypeProp.intValue = newLightType + 1; // Add 1 back to skip Parametric
                serializedObject.ApplyModifiedProperties();
            }
            
            EditorGUI.EndProperty();
        }

        void Color()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Color"));
        }
    }

    /// <summary>
    /// Draws secondary Light2D properties below the swatch section, filtered for 2D relevance.
    /// </summary>
    protected override void DrawComponentPropertiesBelow()
    {
        Intensity();
        Falloff(); // Freeform only
        Sprite(); // Sprite only
        RadiusSpotAngleAndFalloff(); // Spot only
        TargetSortingLayers();
        EditShape(); // Freeform only
        Blending();
        // Show for all except Global
        SerializedProperty lightTypeProp = serializedObject.FindProperty("m_LightType");
        if (lightTypeProp.intValue == (int)Light2D.LightType.Global)
            return;
        Shadows();
        Volumetric();
        NormalMaps();

        void Intensity()
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Intensity"));
        }

        void Falloff()
        {
            // Only show for Freeform lights
            SerializedProperty lightTypeProp = serializedObject.FindProperty("m_LightType");
            if (lightTypeProp.intValue != (int)Light2D.LightType.Freeform)
                return;

            // Falloff Size property
            SerializedProperty falloffSizeProp = serializedObject.FindProperty("m_ShapeLightFalloffSize");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(falloffSizeProp, new GUIContent("Falloff", "Adjusts the falloff area of this light. The higher the falloff value, the larger area the falloff spans."));
            if (EditorGUI.EndChangeCheck())
            {
                if (falloffSizeProp.floatValue < 0)
                    falloffSizeProp.floatValue = 0;
                serializedObject.ApplyModifiedProperties();
            }

            // Falloff Strength (Intensity) property
            SerializedProperty falloffIntensityProp = serializedObject.FindProperty("m_FalloffIntensity");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Slider(falloffIntensityProp, 0, 1, new GUIContent("Falloff Strength", "Adjusts the falloff curve to control the softness of this light's edges. The higher the falloff strength, the softer the edges of this light."));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
        
        void Sprite()
        {
            // Only show for Sprite lights
            SerializedProperty lightTypeProp = serializedObject.FindProperty("m_LightType");
            if (lightTypeProp.intValue != (int)Light2D.LightType.Sprite)
                return;

            // Sprite property (light cookie)
            SerializedProperty spriteProp = serializedObject.FindProperty("m_LightCookieSprite");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(spriteProp, new GUIContent("Sprite", "Assign a Sprite which acts as a mask to create a light cookie."));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        void RadiusSpotAngleAndFalloff()
        {
            // Only show for Spot (Point) lights
            SerializedProperty lightTypeProp = serializedObject.FindProperty("m_LightType");
            if (lightTypeProp.intValue != (int)Light2D.LightType.Point)
                return;

            // Radius properties (Inner / Outer)
            DrawRadiusProperties();

            // Inner / Outer Spot Angle
            DrawInnerAndOuterSpotAngle();

            // Falloff Strength (same as Freeform)
            SerializedProperty falloffIntensityProp = serializedObject.FindProperty("m_FalloffIntensity");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Slider(falloffIntensityProp, 0, 1, new GUIContent("Falloff Strength", "Adjusts the falloff curve to control the softness of this light's edges. The higher the falloff strength, the softer the edges of this light."));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            void DrawRadiusProperties()
            {
                SerializedProperty innerRadius = serializedObject.FindProperty("m_PointLightInnerRadius");
                SerializedProperty outerRadius = serializedObject.FindProperty("m_PointLightOuterRadius");
                
                GUIStyle style = GUI.skin.box;
                float savedLabelWidth = EditorGUIUtility.labelWidth;
                int savedIndentLevel = EditorGUI.indentLevel;
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(new GUIContent("Radius", "Adjusts the inner / outer radius of this light to change the size of this light."));
                EditorGUILayout.BeginHorizontal();
                EditorGUI.indentLevel = 0;
                
                // Inner Radius
                EditorGUIUtility.labelWidth = style.CalcSize(new GUIContent("Inner")).x;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(innerRadius, new GUIContent("Inner", "Specify the inner radius of the light"));
                if (EditorGUI.EndChangeCheck())
                {
                    if (innerRadius.floatValue > outerRadius.floatValue)
                        innerRadius.floatValue = outerRadius.floatValue;
                    else if (innerRadius.floatValue < 0)
                        innerRadius.floatValue = 0;
                    serializedObject.ApplyModifiedProperties();
                }

                // Outer Radius
                EditorGUIUtility.labelWidth = style.CalcSize(new GUIContent("Outer")).x;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(outerRadius, new GUIContent("Outer", "Specify the outer radius of the light"));
                if (EditorGUI.EndChangeCheck())
                {
                    if (outerRadius.floatValue < innerRadius.floatValue)
                        outerRadius.floatValue = innerRadius.floatValue;
                    serializedObject.ApplyModifiedProperties();
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndHorizontal();
                EditorGUIUtility.labelWidth = savedLabelWidth;
                EditorGUI.indentLevel = savedIndentLevel;
            }

            void DrawInnerAndOuterSpotAngle()
            {
                SerializedProperty minProperty = serializedObject.FindProperty("m_PointLightInnerAngle");
                SerializedProperty maxProperty = serializedObject.FindProperty("m_PointLightOuterAngle");
                
                float textFieldWidth = 45f;
                float min = minProperty.floatValue;
                float max = maxProperty.floatValue;

                var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
                rect = EditorGUI.PrefixLabel(rect, new GUIContent("Inner / Outer Spot Angle", "Adjusts the inner / outer angles of this light to change the angle ranges of this Spot Light's beam."));
                
                EditorGUI.BeginProperty(new Rect(rect) { width = rect.width * 0.5f }, GUIContent.none, minProperty);
                EditorGUI.BeginProperty(new Rect(rect) { xMin = rect.x + rect.width * 0.5f }, GUIContent.none, maxProperty);

                var minRect = new Rect(rect) { width = textFieldWidth };
                var maxRect = new Rect(rect) { xMin = rect.xMax - textFieldWidth };
                var sliderRect = new Rect(rect) { xMin = minRect.xMax + 4, xMax = maxRect.xMin - 4 };

                // Min angle field
                EditorGUI.BeginChangeCheck();
                EditorGUI.DelayedFloatField(minRect, minProperty, GUIContent.none);
                if (EditorGUI.EndChangeCheck())
                {
                    if (minProperty.floatValue > maxProperty.floatValue)
                        minProperty.floatValue = maxProperty.floatValue;
                    else if (minProperty.floatValue < 0)
                        minProperty.floatValue = 0;
                    serializedObject.ApplyModifiedProperties();
                }

                // Min/Max slider
                EditorGUI.BeginChangeCheck();
                EditorGUI.MinMaxSlider(sliderRect, ref min, ref max, 0f, 360f);
                if (EditorGUI.EndChangeCheck())
                {
                    minProperty.floatValue = min;
                    maxProperty.floatValue = max;
                    serializedObject.ApplyModifiedProperties();
                }

                // Max angle field
                EditorGUI.BeginChangeCheck();
                EditorGUI.DelayedFloatField(maxRect, maxProperty, GUIContent.none);
                if (EditorGUI.EndChangeCheck())
                {
                    if (minProperty.floatValue > maxProperty.floatValue)
                        maxProperty.floatValue = minProperty.floatValue;
                    else if (maxProperty.floatValue > 360)
                        maxProperty.floatValue = 360;
                    serializedObject.ApplyModifiedProperties();
                }

                EditorGUI.EndProperty();
                EditorGUI.EndProperty();
            }
        }

        void TargetSortingLayers()
        {
            // Safety check: ensure properties are initialized
            if (m_ApplyToSortingLayers == null || m_AllSortingLayers == null || m_ApplyToSortingLayersList == null)
            {
                // Re-initialize if needed
                m_ApplyToSortingLayers = serializedObject.FindProperty("m_ApplyToSortingLayers");
                m_ApplyToSortingLayersList = new List<int>();
                m_AllSortingLayers = SortingLayer.layers;
                m_AllSortingLayerNames = m_AllSortingLayers.Select(x => new GUIContent(x.name)).ToArray();
            }

            Rect totalPosition = EditorGUILayout.GetControlRect();
            GUIContent labelContent = new GUIContent("Target Sorting Layers", "Determines which layers this light affects. To optimize performance, minimize the number of layers this light affects.");
            GUIContent actualLabel = EditorGUI.BeginProperty(totalPosition, labelContent, m_ApplyToSortingLayers);
            Rect position = EditorGUI.PrefixLabel(totalPosition, actualLabel);

            // Build list of currently selected sorting layer IDs
            m_ApplyToSortingLayersList.Clear();
            int applyToSortingLayersSize = m_ApplyToSortingLayers.arraySize;
            for (int i = 0; i < applyToSortingLayersSize; ++i)
            {
                int layerID = m_ApplyToSortingLayers.GetArrayElementAtIndex(i).intValue;
                if (SortingLayer.IsValid(layerID))
                    m_ApplyToSortingLayersList.Add(layerID);
            }

            // Determine what to display in the button
            GUIContent selectedLayers;
            if (m_ApplyToSortingLayersList.Count == 1)
                selectedLayers = new GUIContent(SortingLayer.IDToName(m_ApplyToSortingLayersList[0]));
            else if (m_ApplyToSortingLayersList.Count == m_AllSortingLayers.Length)
                selectedLayers = new GUIContent("Everything");
            else if (m_ApplyToSortingLayersList.Count == 0)
                selectedLayers = new GUIContent("Nothing");
            else
                selectedLayers = new GUIContent("Mixed...");

            if (EditorGUI.DropdownButton(position, selectedLayers, FocusType.Keyboard, EditorStyles.popup))
            {
                GenericMenu menu = new GenericMenu();
                menu.allowDuplicateNames = true;

                // Add "Nothing" option
                menu.AddItem(new GUIContent("Nothing"), m_ApplyToSortingLayersList.Count == 0, () =>
                {
                    m_ApplyToSortingLayersList.Clear();
                    UpdateSortingLayersArray();
                });

                // Add "Everything" option
                menu.AddItem(new GUIContent("Everything"), m_ApplyToSortingLayersList.Count == m_AllSortingLayers.Length, () =>
                {
                    m_ApplyToSortingLayersList.Clear();
                    m_ApplyToSortingLayersList.AddRange(m_AllSortingLayers.Select(x => x.id));
                    UpdateSortingLayersArray();
                });

                menu.AddSeparator("");

                // Add individual sorting layers
                for (int i = 0; i < m_AllSortingLayers.Length; ++i)
                {
                    var sortingLayer = m_AllSortingLayers[i];
                    int layerID = sortingLayer.id;
                    menu.AddItem(m_AllSortingLayerNames[i], m_ApplyToSortingLayersList.Contains(layerID), () =>
                    {
                        if (m_ApplyToSortingLayersList.Contains(layerID))
                            m_ApplyToSortingLayersList.RemoveAll(id => id == layerID);
                        else
                            m_ApplyToSortingLayersList.Add(layerID);
                        UpdateSortingLayersArray();
                    });
                }

                menu.DropDown(position);
            }

            EditorGUI.EndProperty();

            void UpdateSortingLayersArray()
            {
                m_ApplyToSortingLayers.ClearArray();
                for (int i = 0; i < m_ApplyToSortingLayersList.Count; ++i)
                {
                    m_ApplyToSortingLayers.InsertArrayElementAtIndex(i);
                    m_ApplyToSortingLayers.GetArrayElementAtIndex(i).intValue = m_ApplyToSortingLayersList[i];
                }

                serializedObject.ApplyModifiedProperties();
            }
        }

        void EditShape()
        {
            // Only show for Freeform lights
            SerializedProperty lightTypeProp = serializedObject.FindProperty("m_LightType");
            if (lightTypeProp.intValue != (int)Light2D.LightType.Freeform)
                return;

            // Draw the Edit Shape button
            const float kButtonWidth = 33;
            const float kButtonHeight = 23;
            const float k_SpaceBetweenLabelAndButton = 5;
            var buttonStyle = new GUIStyle("EditModeSingleButton");

            var rect = EditorGUILayout.GetControlRect(true, kButtonHeight, buttonStyle);
            var buttonRect = new Rect(rect.xMin + EditorGUIUtility.labelWidth, rect.yMin, kButtonWidth, kButtonHeight);

            var labelContent = new GUIContent("Edit Shape");
            var labelSize = GUI.skin.label.CalcSize(labelContent);

            var labelRect = new Rect(
                buttonRect.xMax + k_SpaceBetweenLabelAndButton,
                rect.yMin + (rect.height - labelSize.y) * .5f,
                labelSize.x,
                rect.height);

            // Load the icon (try both pro and non-pro skin versions)
            GUIContent icon;
            if (EditorGUIUtility.isProSkin)
                icon = new GUIContent(Resources.Load<Texture>("ShapeToolPro"), "Unlocks the shape to allow editing in the Scene View.");
            else
                icon = new GUIContent(Resources.Load<Texture>("ShapeTool"), "Unlocks the shape to allow editing in the Scene View.");

            // Try to find the Freeform Shape Tool
            var freeformToolType = System.Type.GetType("UnityEditor.Rendering.Universal.Light2DEditor+FreeformShapeTool, Unity.RenderPipelines.Universal.Editor");
            
            bool isToolAvailable = freeformToolType != null;
            bool isToolActive = false;

            if (isToolAvailable)
            {
                // Check if the tool is currently active using reflection
                var toolManagerType = typeof(ToolManager).Assembly.GetType("UnityEditor.EditorTools.EditorToolManager");
                if (toolManagerType != null)
                {
                    var isActiveToolMethod = toolManagerType.GetMethod("IsActiveTool", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (isActiveToolMethod != null)
                    {
                        var genericMethod = isActiveToolMethod.MakeGenericMethod(freeformToolType);
                        isToolActive = (bool)genericMethod.Invoke(null, null);
                    }
                }
            }

            using (new EditorGUI.DisabledGroupScope(!isToolAvailable))
            {
                EditorGUI.BeginChangeCheck();
                bool newIsActive = GUI.Toggle(buttonRect, isToolActive, icon, buttonStyle);
                GUI.Label(labelRect, "Edit Shape");

                if (EditorGUI.EndChangeCheck() && isToolAvailable)
                {
                    if (newIsActive)
                    {
                        // Activate the tool
                        var setActiveToolMethod = typeof(ToolManager).GetMethod("SetActiveTool", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static, null, System.Reflection.CallingConventions.Any, new System.Type[] {  }, null);
                        if (setActiveToolMethod != null)
                        {
                            var genericMethod = setActiveToolMethod.MakeGenericMethod(freeformToolType);
                            genericMethod.Invoke(null, null);
                        }
                    }
                    else
                    {
                        // Restore previous tool
                        var restorePreviousToolMethod = typeof(ToolManager).GetMethod("RestorePreviousTool", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (restorePreviousToolMethod != null)
                        {
                            restorePreviousToolMethod.Invoke(null, null);
                        }
                    }
                }
            }
        }

        void Blending()
        {
            CoreEditorUtils.DrawSplitter(false);
            m_BlendingSettingsFoldout = CoreEditorUtils.DrawHeaderFoldout(new GUIContent("Blending", "Options used for blending"), m_BlendingSettingsFoldout);

            if (!m_BlendingSettingsFoldout)
                return;

            // Blend Style - using simplified approach since internal utilities aren't accessible
            SerializedProperty blendStyleProp = serializedObject.FindProperty("m_BlendStyleIndex");

            // Try to get renderer data through reflection or use fallback
            UniversalRenderPipelineAsset pipelineAsset = UniversalRenderPipeline.asset;
            GUIContent[] blendStyleNames = null;
            int[] blendStyleIndices = null;
            bool hasValidBlendStyles = false;

            if (pipelineAsset != null)
            {
                try
                {
                    // Try to access the renderer data
                    var rendererDataProperty = pipelineAsset.GetType().GetProperty("scriptableRendererData");
                    if (rendererDataProperty != null)
                    {
                        var rendererData = rendererDataProperty.GetValue(pipelineAsset);
                        if (rendererData != null)
                        {
                            var lightBlendStylesField = rendererData.GetType().GetField("m_LightBlendStyles",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                            if (lightBlendStylesField != null)
                            {
                                var blendStyles = lightBlendStylesField.GetValue(rendererData) as System.Array;
                                if (blendStyles != null && blendStyles.Length > 0)
                                {
                                    var namesList = new List<string>();
                                    var indicesList = new List<int>();

                                    for (int i = 0; i < blendStyles.Length; i++)
                                    {
                                        var blendStyle = blendStyles.GetValue(i);
                                        var nameField = blendStyle.GetType().GetField("name");
                                        if (nameField != null)
                                        {
                                            string name = nameField.GetValue(blendStyle) as string;
                                            namesList.Add(name ?? $"Operation {i}");
                                            indicesList.Add(i);
                                        }
                                    }

                                    if (namesList.Count > 0)
                                    {
                                        blendStyleNames = namesList.Select(x => new GUIContent(x)).ToArray();
                                        blendStyleIndices = indicesList.ToArray();
                                        hasValidBlendStyles = true;
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Fallback if reflection fails
                }
            }

            // Use fallback with default URP 2D blend style names if we couldn't get blend styles from renderer data
            if (!hasValidBlendStyles)
            {
                blendStyleNames = new GUIContent[]
                {
                    new GUIContent("Multiply"),
                    new GUIContent("Additive"),
                    new GUIContent("Multiply with Mask (R)"),
                    new GUIContent("Additive with Mask (R)")
                };
                blendStyleIndices = new int[] { 0, 1, 2, 3 };
            }

            // Display Blend Style
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.IntPopup(blendStyleProp, blendStyleNames, blendStyleIndices,
                new GUIContent("Blend Style", "Adjusts how this light blends with the Sprites on the Target Sorting Layers. Different Blend Styles can be customized in the 2D Renderer Data Asset."));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            // Light Order
            SerializedProperty lightOrderProp = serializedObject.FindProperty("m_LightOrder");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(lightOrderProp,
                new GUIContent("Light Order", "Determines the relative order in which lights of the same Blend Style get rendered. Lights with lower values are rendered first."));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            // Overlap Operation
            SerializedProperty overlapOperationProp = serializedObject.FindProperty("m_OverlapOperation");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(overlapOperationProp,
                new GUIContent("Overlap Operation", "Determines how this light blends with the other lights either through additive or alpha blending."));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }
        
        void Shadows()
        {
            CoreEditorUtils.DrawSplitter(false);
            
            SerializedProperty shadowsEnabledProp = serializedObject.FindProperty("m_ShadowsEnabled");
            
            // Draw header foldout with toggle checkbox
            DrawHeaderFoldoutWithToggle(new GUIContent("Shadows", "Options used for shadows"), ref m_ShadowsSettingsFoldout, shadowsEnabledProp);

            if (!m_ShadowsSettingsFoldout)
                return;

            EditorGUI.indentLevel++;
            EditorGUI.BeginDisabledGroup(!shadowsEnabledProp.boolValue);

            // Shadow Strength
            SerializedProperty shadowIntensityProp = serializedObject.FindProperty("m_ShadowIntensity");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(shadowIntensityProp, 
                new GUIContent("Strength", "Adjusts the amount of light occlusion from the Shadow Caster 2D component(s) when blocking this light. The higher the value, the more opaque the shadow becomes."));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            // Shadow Softness
            SerializedProperty shadowSoftnessProp = serializedObject.FindProperty("m_ShadowSoftness");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(shadowSoftnessProp, 
                new GUIContent("Softness", "Adjusts the amount of softness at the edge of the shadow."));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            // Shadow Falloff Strength
            SerializedProperty shadowFalloffProp = serializedObject.FindProperty("m_ShadowSoftnessFalloffIntensity");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(shadowFalloffProp, 
                new GUIContent("Falloff Strength", "Adjusts the falloff curve to control the softness of the shadow edges. The higher the falloff strength, the softer the edges of this shadow."));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;
        }

        void Volumetric()
        {
            CoreEditorUtils.DrawSplitter(false);
            
            SerializedProperty volumetricEnabledProp = serializedObject.FindProperty("m_LightVolumeEnabled");
            
            // Draw header foldout with toggle checkbox
            DrawHeaderFoldoutWithToggle(new GUIContent("Volumetric", "Options used for volumetric lighting"), ref m_VolumetricSettingsFoldout, volumetricEnabledProp);

            if (!m_VolumetricSettingsFoldout)
                return;

            EditorGUI.indentLevel++;
            EditorGUI.BeginDisabledGroup(!volumetricEnabledProp.boolValue);

            // Volumetric Intensity
            SerializedProperty volumetricIntensityProp = serializedObject.FindProperty("m_LightVolumeIntensity");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(volumetricIntensityProp, 
                new GUIContent("Intensity", "Adjusts the intensity of this additional light volume that's additively blended on top of this light. To enable the Volumetric Shadow Strength, increase this Intensity to be greater than 0."));
            if (EditorGUI.EndChangeCheck())
            {
                if (volumetricIntensityProp.floatValue < 0)
                    volumetricIntensityProp.floatValue = 0;
                serializedObject.ApplyModifiedProperties();
            }

            // Shadow Strength with toggle
            DrawToggleProperty(
                new GUIContent("Shadow Strength", "Adjusts the amount of volume light occlusion from the Shadow Caster 2D component(s) when blocking this light."), 
                serializedObject.FindProperty("m_ShadowVolumeIntensityEnabled"), 
                serializedObject.FindProperty("m_ShadowVolumeIntensity"));

            EditorGUI.EndDisabledGroup();
            EditorGUI.indentLevel--;
        }

        void DrawToggleProperty(GUIContent label, SerializedProperty boolProperty, SerializedProperty property)
        {
            int savedIndentLevel = EditorGUI.indentLevel;
            float savedLabelWidth = EditorGUIUtility.labelWidth;
            const int kCheckboxWidth = 20;

            EditorGUILayout.BeginHorizontal();
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(boolProperty, GUIContent.none, GUILayout.MaxWidth(kCheckboxWidth));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUIUtility.labelWidth = EditorGUIUtility.labelWidth - kCheckboxWidth;
            EditorGUI.BeginDisabledGroup(!boolProperty.boolValue);
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(property, label);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
            
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel = savedIndentLevel;
            EditorGUIUtility.labelWidth = savedLabelWidth;
        }
        
        void NormalMaps()
        {
            CoreEditorUtils.DrawSplitter(false);
            m_NormalMapsSettingsFoldout = CoreEditorUtils.DrawHeaderFoldout(new GUIContent("Normal Maps", "Options used for normal maps"), m_NormalMapsSettingsFoldout);

            if (!m_NormalMapsSettingsFoldout)
                return;

            // Quality property
            SerializedProperty normalMapQualityProp = serializedObject.FindProperty("m_NormalMapQuality");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(normalMapQualityProp,
                new GUIContent("Quality", "Determines the accuracy of the lighting calculations when normal map is used. To optimize for performance, select Fast."));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }

            // Distance property (disabled when Quality is set to Disabled)
            EditorGUI.BeginDisabledGroup(normalMapQualityProp.intValue == (int)Light2D.NormalMapQuality.Disabled);

            SerializedProperty normalMapZDistanceProp = serializedObject.FindProperty("m_NormalMapDistance");
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(normalMapZDistanceProp,
                new GUIContent("Distance", "Adjusts the z-axis distance of this light and the lit Sprite(s). Do note that this distance does not Transform the position of this light in the Scene."));
            if (EditorGUI.EndChangeCheck())
            {
                normalMapZDistanceProp.floatValue = Mathf.Max(0.0f, normalMapZDistanceProp.floatValue);
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.EndDisabledGroup();
        }
    
        void DrawHeaderFoldoutWithToggle(GUIContent title, ref bool foldoutState, SerializedProperty toggleState)
        {
            const float height = 17f;
            var backgroundRect = GUILayoutUtility.GetRect(0, 0);

            var labelRect = backgroundRect;
            labelRect.yMax += height;
            labelRect.xMin += 16f;
            labelRect.xMax -= 20f;

            EditorGUI.BeginChangeCheck();
            bool newToggleState = GUI.Toggle(labelRect, toggleState.boolValue, " ");  // Needs a space for proper checkbox outline
            if (EditorGUI.EndChangeCheck())
            {
                toggleState.boolValue = newToggleState;
                serializedObject.ApplyModifiedProperties();
            }

            bool newFoldoutState = CoreEditorUtils.DrawHeaderFoldout("", foldoutState);
            if (newFoldoutState != foldoutState)
                foldoutState = newFoldoutState;

            labelRect.xMin += 20;
            EditorGUI.LabelField(labelRect, title, EditorStyles.boldLabel);
        }

    }

    /// <summary>
    /// Gets the SwatchColorReference component from the current Light2D target.
    /// </summary>
    protected override SwatchColorReference GetCurrentSwatchReference()
    {
        if (target != null)
        {
            Light2D sr = (Light2D)target;
            return sr.GetComponent<SwatchColorReference>();
        }
        return null;
    }

    /// <summary>
    /// Auto-assigns swatch 0 to new Light2Ds that don't have swatch references.
    /// </summary>
    protected override void AutoAssignDefaultSwatch()
    {
        // Only auto-assign if we have a valid color palette
        if (colorPalette == null || colorPalette.colors == null || colorPalette.colors.Length == 0)
            return;

        foreach (var t in targets)
        {
            Light2D Light2D = (Light2D)t;

            if (Light2D.TryGetComponent<SwatchColorReference>(out var existingRef))
            {
                // Check if this existing reference is in our list
                if (!SwatchRefs.Contains(existingRef))
                {
                    // This is a copied/duplicated object - add it to our list
                    SwatchRefs.Add(existingRef);
                    EditorUtility.SetDirty(existingRef);
                    EditorUtility.SetDirty(Light2D);
                }
            }
            else
            {
                // No SwatchColorReference - create new one and assign swatch 0
                SwatchColorReference newSwatchRef = CreateSwatchColorReference(Light2D);

                // Set to swatch 0 and apply the color
                newSwatchRef.SetSwatchIndexAndApplyColor(0);
                EditorUtility.SetDirty(newSwatchRef);
                EditorUtility.SetDirty(Light2D);
            }
        }
    }

    /// <summary>
    /// Checks if the Light2D's color has been manually changed outside the swatch system.
    /// </summary>
    protected override bool HasColorChangedManually()
    {
        Light2D Light2D = (Light2D)target;
        SwatchColorReference swatchRef = Light2D.GetComponent<SwatchColorReference>();
        
        if (swatchRef == null || swatchRef.GetSwatchIndex() < 0)
            return false;

        Color swatchColor = swatchRef.ColorFromPalette();
        return Light2D.color != swatchColor;
    }
}