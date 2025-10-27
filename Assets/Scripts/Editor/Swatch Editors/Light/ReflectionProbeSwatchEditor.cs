using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine.Rendering;
using UnityEditor.Rendering;
using System.Collections.Generic;

/// <summary>
/// Custom editor for ReflectionProbe that integrates swatch-based color management with 2D-optimized property display.
/// 
/// Inherits from SwatchEditorBase to reuse all swatch functionality while adding ReflectionProbe-specific
/// property handling and behavior.
/// </summary>
[CustomEditor(typeof(ReflectionProbe))]
public class ReflectionProbeSwatchEditor : SwatchEditorBase
{
    // Serialized properties
    private SerializedProperty m_Mode;
    private SerializedProperty m_Importance;
    private SerializedProperty m_IntensityMultiplier;
    private SerializedProperty m_BoxProjection;
    private SerializedProperty m_BlendDistance;
    private SerializedProperty m_BoxSize;
    private SerializedProperty m_BoxOffset;
    private SerializedProperty m_Resolution;
    private SerializedProperty m_HDR;
    private SerializedProperty m_ShadowDistance;
    private SerializedProperty m_ClearFlags;
    private SerializedProperty m_BackgroundColor;
    private SerializedProperty m_CullingMask;
    private SerializedProperty m_UseOcclusionCulling;
    private SerializedProperty[] m_NearAndFarProperties;

    // UI state
    private bool m_ShowRuntimeSettings = true;
    private bool m_ShowCubemapCaptureSettings = true;

    private static class Styles
    {
        static Styles()
        {
            richTextMiniLabel.richText = true;
        }

        public static GUIStyle richTextMiniLabel = new GUIStyle(EditorStyles.miniLabel);

        // Type dropdown
        public static GUIContent typeText = EditorGUIUtility.TrTextContent("Type", "Specify the lighting setup for this probe: Baked, Custom, or Realtime.");
        public static GUIContent[] reflectionProbeMode = {
            EditorGUIUtility.TrTextContent("Baked"),
            EditorGUIUtility.TrTextContent("Custom"),
            EditorGUIUtility.TrTextContent("Realtime")
        };
        public static int[] reflectionProbeModeValues = {
            (int)ReflectionProbeMode.Baked,
            (int)ReflectionProbeMode.Custom,
            (int)ReflectionProbeMode.Realtime
        };

        // Runtime Settings
        public static GUIContent runtimeSettingsHeader = EditorGUIUtility.TrTextContent("Runtime Settings", "These settings determine this Probe's priority, blending, intensity, and zone of effect and works in conjunction with the cubemap of this probe when it is rendered.");
        public static GUIContent importanceText = EditorGUIUtility.TrTextContent("Importance", "When reflection probes overlap, Unity uses Importance to determine which probe should take priority.");
        public static GUIContent intensityText = EditorGUIUtility.TrTextContent("Intensity", "The intensity modifier the Editor applies to this probe's texture in its shader.");
        public static GUIContent boxProjectionText = EditorGUIUtility.TrTextContent("Box Projection", "When enabled, Unity assumes that the reflected light is originating from the inside of the probe's box, rather than from infinitely far away. This is useful for box-shaped indoor environments.");
        public static GUIContent blendDistanceText = EditorGUIUtility.TrTextContent("Blend Distance", "Area around the probe where it is blended with other probes. Only used in deferred probes.");
        public static GUIContent sizeText = EditorGUIUtility.TrTextContent("Box Size", "The size of the box in which the reflections will be applied to objects. The value is not affected by the Transform of the Game Object.");
        public static GUIContent centerText = EditorGUIUtility.TrTextContent("Box Offset", "The center of the box in which the reflections will be applied to objects. The value is relative to the position of the Game Object.");

        // Cubemap Capture Settings
        public static GUIContent captureCubemapHeader = EditorGUIUtility.TrTextContent("Cubemap Capture Settings", "Settings that determine how to render this probe's cubemap.");
        public static GUIContent resolutionText = EditorGUIUtility.TrTextContent("Resolution", "The resolution of the cubemap.");
        public static GUIContent hdrText = EditorGUIUtility.TrTextContent("HDR", "Enable High Dynamic Range rendering.");
        public static GUIContent shadowDistanceText = EditorGUIUtility.TrTextContent("Shadow Distance", "Maximum distance at which Unity renders shadows associated with this probe.");
        public static GUIContent clearFlagsText = EditorGUIUtility.TrTextContent("Clear Flags", "Specify how to fill empty areas of the cubemap.");
        public static GUIContent backgroundColorText = EditorGUIUtility.TrTextContent("Background Color", "Camera clears the screen to this color before rendering.");
        public static GUIContent cullingMaskText = EditorGUIUtility.TrTextContent("Culling Mask", "Allows objects on specified layers to be included or excluded in the reflection.");
        public static GUIContent useOcclusionCulling = EditorGUIUtility.TrTextContent("Occlusion Culling", "If this property is enabled, geometries which are blocked from the probe's line of sight are skipped during rendering.");
        
        // Bake button
        public static GUIContent bakeButtonText = EditorGUIUtility.TrTextContent("Bake");
        public static GUIContent bakeCustomButtonText = EditorGUIUtility.TrTextContent("Bake", "Bakes Reflection Probe's cubemap, overwriting the existing cubemap texture asset (if any).");
        public static string[] bakeCustomOptionText = { "Bake as new Cubemap..." };
        public static string[] bakeButtonsText = { "Bake All Reflection Probes" };
        
        public static GUIContent[] clearFlags =
        {
            EditorGUIUtility.TrTextContent("Skybox"),
            EditorGUIUtility.TrTextContent("Solid Color")
        };
        public static int[] clearFlagsValues = { 1, 2 }; // taken from Camera.h

        public static int[] reflectionResolutionValuesArray = null;
        public static GUIContent[] reflectionResolutionTextArray = null;

        // Toolbar
        private static GUIContent customPrimitiveBoundsHandleEditModeButton = new GUIContent(
            EditorGUIUtility.IconContent("EditShape").image,
            "Adjust the probe's zone of effect. Holding Alt or Shift and click the control handle to pin the center or scale the volume uniformly."
        );

        public static GUIContent[] toolContents =
        {
            customPrimitiveBoundsHandleEditModeButton,
            EditorGUIUtility.TrIconContent("CapturePosition", "Modify capture position.")
        };

        public static EditMode.SceneViewEditMode[] sceneViewEditModes = new[]
        {
            EditMode.SceneViewEditMode.ReflectionProbeBox,
            EditMode.SceneViewEditMode.ReflectionProbeOrigin
        };

        public static string baseSceneEditingToolText = "<color=grey>Probe Scene Editing Mode:</color> ";

        public static GUIContent[] toolNames =
        {
            new GUIContent(baseSceneEditingToolText + "Box Projection Bounds", ""),
            new GUIContent(baseSceneEditingToolText + "Probe Origin", "")
        };
    }

    private static ReflectionProbeSwatchEditor s_LastInteractedEditor;

    public override void OnInspectorGUI()
    {
        if (!m_ShowCubemapCaptureSettings)
        {
            // When Cubemap Capture Settings is collapsed, skip swatches
            serializedObject.Update();
            DrawComponentPropertiesAbove();
            DrawComponentPropertiesBelow();
            serializedObject.ApplyModifiedProperties();
        }
        else
        {
            // Use default swatch-based editor when foldout is expanded
            base.OnInspectorGUI();
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();

        // Initialize serialized properties
        m_Mode = serializedObject.FindProperty("m_Mode");
        m_Importance = serializedObject.FindProperty("m_Importance");
        m_IntensityMultiplier = serializedObject.FindProperty("m_IntensityMultiplier");
        m_BoxProjection = serializedObject.FindProperty("m_BoxProjection");
        m_BlendDistance = serializedObject.FindProperty("m_BlendDistance");
        m_BoxSize = serializedObject.FindProperty("m_BoxSize");
        m_BoxOffset = serializedObject.FindProperty("m_BoxOffset");
        m_Resolution = serializedObject.FindProperty("m_Resolution");
        m_HDR = serializedObject.FindProperty("m_HDR");
        m_ShadowDistance = serializedObject.FindProperty("m_ShadowDistance");
        m_ClearFlags = serializedObject.FindProperty("m_ClearFlags");
        m_BackgroundColor = serializedObject.FindProperty("m_BackGroundColor");
        m_CullingMask = serializedObject.FindProperty("m_CullingMask");
        m_UseOcclusionCulling = serializedObject.FindProperty("m_UseOcclusionCulling");
        m_NearAndFarProperties = new[] {
            serializedObject.FindProperty("m_NearClip"),
            serializedObject.FindProperty("m_FarClip")
        };
    }

    private bool sceneViewEditing
    {
        get
        {
            return IsReflectionProbeEditMode(EditMode.editMode) && EditMode.IsOwner(this);
        }
    }

    private static bool IsReflectionProbeEditMode(EditMode.SceneViewEditMode editMode)
    {
        return editMode == EditMode.SceneViewEditMode.ReflectionProbeBox ||
               editMode == EditMode.SceneViewEditMode.ReflectionProbeOrigin;
    }

    private void DoToolbar()
    {
        // Show the master tool selector
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUI.changed = false;

        var oldEditMode = EditMode.editMode;
        EditorGUI.BeginChangeCheck();
        EditMode.DoInspectorToolbar(Styles.sceneViewEditModes, Styles.toolContents, GetBounds, this);
        if (EditorGUI.EndChangeCheck())
            s_LastInteractedEditor = this;

        if (oldEditMode != EditMode.editMode)
        {
            // Repaint to update UI
            Repaint();
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        // Info box for tools
        GUILayout.BeginVertical(EditorStyles.helpBox);
        string helpText = Styles.baseSceneEditingToolText;
        if (sceneViewEditing)
        {
            int index = System.Array.IndexOf(Styles.sceneViewEditModes, EditMode.editMode);
            if (index >= 0)
                helpText = Styles.toolNames[index].text;
        }
        GUILayout.Label(helpText, Styles.richTextMiniLabel);
        GUILayout.EndVertical();

        EditorGUILayout.Space();
    }

    private Bounds GetBounds()
    {
        ReflectionProbe probe = (ReflectionProbe)target;
        return new Bounds(probe.transform.position, probe.size);
    }
    
    /// <summary>
    /// Draws the most commonly used ReflectionProbe properties above the swatch section.
    /// </summary>
    protected override void DrawComponentPropertiesAbove()
    {
        // Only show toolbar when editing a single reflection probe
        if (targets.Length == 1)
            DoToolbar();

        // Type property
        EditorGUILayout.IntPopup(m_Mode, Styles.reflectionProbeMode, Styles.reflectionProbeModeValues, Styles.typeText);

        EditorGUILayout.Space();

        // Runtime Settings foldout
        m_ShowRuntimeSettings = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowRuntimeSettings, Styles.runtimeSettingsHeader);
        if (m_ShowRuntimeSettings)
        {
            EditorGUI.indentLevel++;

            // Importance
            EditorGUILayout.PropertyField(m_Importance, Styles.importanceText);

            // Intensity
            EditorGUILayout.PropertyField(m_IntensityMultiplier, Styles.intensityText);

            // Box Projection
            EditorGUILayout.PropertyField(m_BoxProjection, Styles.boxProjectionText);

            // Blend Distance
            using (new EditorGUI.DisabledScope(!SupportedRenderingFeatures.active.reflectionProbesBlendDistance))
            {
                EditorGUILayout.PropertyField(m_BlendDistance, Styles.blendDistanceText);
            }

            // Box Size and Box Offset
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_BoxSize, Styles.sizeText);
            EditorGUILayout.PropertyField(m_BoxOffset, Styles.centerText);
            if (EditorGUI.EndChangeCheck())
            {
                Vector3 center = m_BoxOffset.vector3Value;
                Vector3 size = m_BoxSize.vector3Value;
                if (ValidateAABB(ref center, ref size))
                {
                    m_BoxOffset.vector3Value = center;
                    m_BoxSize.vector3Value = size;
                }
            }

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space();

        // Cubemap Capture Settings foldout
        m_ShowCubemapCaptureSettings = EditorGUILayout.BeginFoldoutHeaderGroup(m_ShowCubemapCaptureSettings, Styles.captureCubemapHeader);
        if (m_ShowCubemapCaptureSettings)
        {
            EditorGUI.indentLevel++;

            // Resolution
            int[] reflectionResolutionValuesArray = null;
            GUIContent[] reflectionResolutionTextArray = null;
            GetResolutionArray(ref reflectionResolutionValuesArray, ref reflectionResolutionTextArray);
            EditorGUILayout.IntPopup(m_Resolution, reflectionResolutionTextArray, reflectionResolutionValuesArray, Styles.resolutionText, GUILayout.MinWidth(40));

            // HDR
            EditorGUILayout.PropertyField(m_HDR, Styles.hdrText);

            // Shadow Distance
            EditorGUILayout.PropertyField(m_ShadowDistance, Styles.shadowDistanceText);

            // Clear Flags
            EditorGUILayout.IntPopup(m_ClearFlags, Styles.clearFlags, Styles.clearFlagsValues, Styles.clearFlagsText);

            // Background Color
            EditorGUILayout.PropertyField(m_BackgroundColor, Styles.backgroundColorText);
        }
    }

    private void GetResolutionArray(ref int[] resolutionList, ref GUIContent[] resolutionStringList)
    {
        if (Styles.reflectionResolutionValuesArray == null && Styles.reflectionResolutionTextArray == null)
        {
            int cubemapResolution = Mathf.Max(1, ReflectionProbe.minBakedCubemapResolution);
            List<int> envReflectionResolutionValues = new List<int>();
            List<GUIContent> envReflectionResolutionText = new List<GUIContent>();
            do
            {
                envReflectionResolutionValues.Add(cubemapResolution);
                envReflectionResolutionText.Add(new GUIContent(cubemapResolution.ToString()));
                cubemapResolution *= 2;
            }
            while (cubemapResolution <= ReflectionProbe.maxBakedCubemapResolution);

            Styles.reflectionResolutionValuesArray = envReflectionResolutionValues.ToArray();
            Styles.reflectionResolutionTextArray = envReflectionResolutionText.ToArray();
        }

        resolutionList = Styles.reflectionResolutionValuesArray;
        resolutionStringList = Styles.reflectionResolutionTextArray;
    }

    private void DrawClippingPlanes()
    {
        const float kNearFarLabelsWidth = 60f;
        
        GUIContent clipingPlanesLabel = new GUIContent("Clipping Planes", "Distances from the camera where rendering starts and stops.");
        GUIContent[] nearAndFarLabels = new GUIContent[]
        {
            new GUIContent("Near", "The closest point relative to the camera where drawing will occur."),
            new GUIContent("Far", "The furthest point relative to the camera where drawing will occur.")
        };

        float savedLabelWidth = EditorGUIUtility.labelWidth;
        int savedIndentLevel = EditorGUI.indentLevel;

        Rect rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
        rect = EditorGUI.PrefixLabel(rect, clipingPlanesLabel);

        EditorGUI.indentLevel = 0;
        
        float labelWidth = kNearFarLabelsWidth;
        float fieldWidth = (rect.width - labelWidth * 2) / 2;

        // Near clip
        Rect nearRect = new Rect(rect.x, rect.y, labelWidth + fieldWidth, rect.height);
        float savedLabelWidthNear = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = labelWidth;
        EditorGUI.PropertyField(nearRect, m_NearAndFarProperties[0], nearAndFarLabels[0]);
        EditorGUIUtility.labelWidth = savedLabelWidthNear;

        // Far clip
        Rect farRect = new Rect(rect.x + labelWidth + fieldWidth, rect.y, labelWidth + fieldWidth, rect.height);
        float savedLabelWidthFar = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = labelWidth;
        EditorGUI.PropertyField(farRect, m_NearAndFarProperties[1], nearAndFarLabels[1]);
        EditorGUIUtility.labelWidth = savedLabelWidthFar;

        EditorGUIUtility.labelWidth = savedLabelWidth;
        EditorGUI.indentLevel = savedIndentLevel;
    }

    // Ensures that probe's AABB encapsulates probe's position
    // Returns true if center or size was modified
    private bool ValidateAABB(ref Vector3 center, ref Vector3 size)
    {
        ReflectionProbe p = (ReflectionProbe)target;
        Vector3 localTransformPosition = p.transform.InverseTransformPoint(p.transform.position);
        Bounds b = new Bounds(center, size);
        if (b.Contains(localTransformPosition)) return false;
        b.Encapsulate(localTransformPosition);
        center = b.center;
        size = b.size;
        return true;
    }

    /// <summary>
    /// Draws secondary ReflectionProbe properties below the swatch section, filtered for 2D relevance.
    /// </summary>
    protected override void DrawComponentPropertiesBelow()
    {
        if (m_ShowCubemapCaptureSettings)
        {
            // Culling Mask
            EditorGUILayout.PropertyField(m_CullingMask, Styles.cullingMaskText);

            // Occlusion Culling
            EditorGUILayout.PropertyField(m_UseOcclusionCulling, Styles.useOcclusionCulling);

            // Clipping Planes (Near and Far)
            DrawClippingPlanes();

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space();
            
        DoBakeButton();
    }

    private ReflectionProbe reflectionProbeTarget
    {
        get { return (ReflectionProbe)target; }
    }

    private ReflectionProbeMode reflectionProbeMode
    {
        get { return reflectionProbeTarget.mode; }
    }

    private void DoBakeButton()
    {
        if (reflectionProbeTarget.mode == ReflectionProbeMode.Realtime)
        {
            EditorGUILayout.HelpBox("Baking of this reflection probe should be initiated from the scripting API because the type is 'Realtime'", MessageType.Info);
            if (!QualitySettings.realtimeReflectionProbes)
                EditorGUILayout.HelpBox("Realtime reflection probes are disabled in Quality Settings", MessageType.Warning);
            return;
        }

        GUILayout.BeginHorizontal();
        switch (reflectionProbeMode)
        {
            case ReflectionProbeMode.Custom:
                if (EditorGUI.LargeSplitButtonWithDropdownList(Styles.bakeCustomButtonText, Styles.bakeCustomOptionText, OnBakeCustomButton))
                {
                    BakeCustomReflectionProbe(reflectionProbeTarget, true);
                    GUIUtility.ExitGUI();
                }
                break;
            case ReflectionProbeMode.Baked:
                using (new EditorGUI.DisabledScope(!reflectionProbeTarget.enabled))
                {
                    // Bake button in non-continuous mode
                    if (EditorGUI.LargeSplitButtonWithDropdownList(Styles.bakeButtonText, Styles.bakeButtonsText, OnBakeButton))
                    {
                        if (!Lightmapping.BakeReflectionProbe(reflectionProbeTarget, AssetDatabase.GetAssetPath(reflectionProbeTarget.bakedTexture)))
                            Debug.LogError("Failed to bake reflection probe");
                        GUIUtility.ExitGUI();
                    }
                }
                break;
            case ReflectionProbeMode.Realtime:
                // Not showing bake button in realtime
                break;
        }
        GUILayout.EndHorizontal();

        EditorGUILayout.Space();
    }

    private void OnBakeCustomButton(object data)
    {
        int mode = (int)data;
        ReflectionProbe p = target as ReflectionProbe;
        if (mode == 0)
            BakeCustomReflectionProbe(p, false);
    }

    private void OnBakeButton(object data)
    {
        int mode = (int)data;
        if (mode == 0)
        {
            // Bake all reflection probes in the scene
            ReflectionProbe[] probes = FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None);
            foreach (var probe in probes)
            {
                if (probe.mode == ReflectionProbeMode.Baked)
                {
                    string path = AssetDatabase.GetAssetPath(probe.bakedTexture);
                    if (!string.IsNullOrEmpty(path))
                        Lightmapping.BakeReflectionProbe(probe, path);
                }
            }
        }
    }

    private void BakeCustomReflectionProbe(ReflectionProbe probe, bool usePreviousAssetPath)
    {
        string path = "";
        if (usePreviousAssetPath)
            path = AssetDatabase.GetAssetPath(probe.customBakedTexture);

        string targetExtension = probe.hdr ? "exr" : "png";
        if (string.IsNullOrEmpty(path) || System.IO.Path.GetExtension(path) != "." + targetExtension)
        {
            // We use the path of the active scene as the target path
            string targetPath = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
            targetPath = System.IO.Path.GetDirectoryName(targetPath);
            
            if (string.IsNullOrEmpty(targetPath))
                targetPath = "Assets";
            else if (!System.IO.Directory.Exists(targetPath))
                System.IO.Directory.CreateDirectory(targetPath);

            string fileName = probe.name + (probe.hdr ? "-reflectionHDR" : "-reflection") + "." + targetExtension;
            fileName = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GenerateUniqueAssetPath(System.IO.Path.Combine(targetPath, fileName)));
            
            path = EditorUtility.SaveFilePanelInProject("Save reflection probe's cubemap.", fileName, targetExtension, "", targetPath);
            if (string.IsNullOrEmpty(path))
                return;

            ReflectionProbe collidingProbe;
            if (IsCollidingWithOtherProbes(path, probe, out collidingProbe))
            {
                if (!EditorUtility.DisplayDialog("Cubemap is used by other reflection probe",
                    string.Format("'{0}' path is used by the game object '{1}', do you really want to overwrite it?",
                        path, collidingProbe.name), "Yes", "No"))
                {
                    return;
                }
            }
        }

        EditorUtility.DisplayProgressBar("Reflection Probes", "Baking " + path, 0.5f);
        if (!Lightmapping.BakeReflectionProbe(probe, path))
            Debug.LogError("Failed to bake reflection probe to " + path);
        EditorUtility.ClearProgressBar();
    }

    private bool IsCollidingWithOtherProbes(string targetPath, ReflectionProbe targetProbe, out ReflectionProbe collidingProbe)
    {
        ReflectionProbe[] probes = FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None);
        collidingProbe = null;
        foreach (var probe in probes)
        {
            if (probe == targetProbe || probe.customBakedTexture == null)
                continue;
            string path = AssetDatabase.GetAssetPath(probe.customBakedTexture);
            if (path == targetPath)
            {
                collidingProbe = probe;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gets the SwatchColorReference component from the current ReflectionProbe target.
    /// </summary>
    protected override SwatchColorReference GetCurrentSwatchReference()
    {
        if (target != null)
        {
            ReflectionProbe sr = (ReflectionProbe)target;
            return sr.GetComponent<SwatchColorReference>();
        }
        return null;
    }

    /// <summary>
    /// Auto-assigns swatch 0 to new ReflectionProbes that don't have swatch references.
    /// </summary>
    protected override void AutoAssignDefaultSwatch()
    {
        // Only auto-assign if we have a valid color palette
        if (colorPalette == null || colorPalette.colors == null || colorPalette.colors.Length == 0)
            return;

        foreach (var t in targets)
        {
            ReflectionProbe ReflectionProbe = (ReflectionProbe)t;

            if (ReflectionProbe.TryGetComponent<SwatchColorReference>(out var existingRef))
            {
                // Check if this existing reference is in our list
                if (!SwatchRefs.Contains(existingRef))
                {
                    // This is a copied/duplicated object - add it to our list
                    SwatchRefs.Add(existingRef);
                    EditorUtility.SetDirty(existingRef);
                    EditorUtility.SetDirty(ReflectionProbe);
                }
            }
            else
            {
                // No SwatchColorReference - create new one and assign swatch 0
                SwatchColorReference newSwatchRef = CreateSwatchColorReference(ReflectionProbe);

                // Set to swatch 0 and apply the color
                newSwatchRef.SetSwatchIndexAndApplyColor(0);
                EditorUtility.SetDirty(newSwatchRef);
                EditorUtility.SetDirty(ReflectionProbe);
            }
        }
    }

    /// <summary>
    /// Checks if the ReflectionProbe's color has been manually changed outside the swatch system.
    /// </summary>
    protected override bool HasColorChangedManually()
    {
        ReflectionProbe ReflectionProbe = (ReflectionProbe)target;
        SwatchColorReference swatchRef = ReflectionProbe.GetComponent<SwatchColorReference>();
        
        if (swatchRef == null || swatchRef.GetSwatchIndex() < 0)
            return false;

        Color swatchColor = swatchRef.ColorFromPalette();
        return ReflectionProbe.backgroundColor != swatchColor;
    }
}