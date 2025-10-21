using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Temporary EditorWindow to visualize common easing curves used by PrimeTween.
// Drop into Assets/Editor/ and open via Window -> PrimeTween Ease Viewer.
// This is a lightweight viewer that implements standard easing formulas for
// the most common Ease enum names (OutQuad, InQuad, OutBack, InOutSine, etc.).
// It's intended as a temporary debugging aid; delete when done.

public class PrimeTweenEaseViewer : EditorWindow {
    Vector2 scroll;
    int sampleCount = 128;
    float graphWidth = 500;
    float graphHeight = 250;
    int selectedEaseIndex = 0;
    string[] easeNames;

    [MenuItem("Window/PrimeTween Ease Viewer")]
    static void Open() {
        var w = GetWindow<PrimeTweenEaseViewer>("PrimeTween Eases");
        w.minSize = new Vector2(480, 360);
    }

    void OnEnable() {
        // Try to get the Ease enum from PrimeTween if present; otherwise fall back to a small list.
        Type easeType = Type.GetType("PrimeTween.Ease, PrimeTween") ?? Type.GetType("Ease");
        if (easeType != null && easeType.IsEnum) {
            easeNames = Enum.GetNames(easeType);
        } else {
            easeNames = new string[] {
                "Linear",
                "InQuad", "OutQuad", "InOutQuad",
                "InCubic", "OutCubic", "InOutCubic",
                "InSine", "OutSine", "InOutSine",
                "InBack", "OutBack", "InOutBack",
            };
        }
    }

    void OnGUI() {
        // Controls section (not scrollable)
        EditorGUILayout.LabelField("PrimeTween Ease Viewer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        sampleCount = EditorGUILayout.IntSlider("Samples", sampleCount, 32, 1024);
        if (GUILayout.Button("Refresh", GUILayout.Width(80))) Repaint();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        graphWidth = EditorGUILayout.FloatField("Graph Width", graphWidth);
        graphHeight = EditorGUILayout.FloatField("Graph Height", graphHeight);
        EditorGUILayout.EndHorizontal();

        selectedEaseIndex = EditorGUILayout.Popup("Ease", selectedEaseIndex, easeNames);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider); // separator line

        // Graph section (scrollable)
        scroll = EditorGUILayout.BeginScrollView(scroll);
        
        EditorGUILayout.BeginVertical();
        GUILayout.Space(20);
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        DrawGraph(easeNames[selectedEaseIndex]);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        
        GUILayout.Space(20);
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndScrollView();
    }

    void DrawGraph(string easeName) {
        Rect r = GUILayoutUtility.GetRect(graphWidth + 20, graphHeight + 40);
        GUI.Box(r, GUIContent.none);

        // inner rect
        Rect inner = new Rect(r.x + 10, r.y + 10, graphWidth, graphHeight);
        EditorGUI.DrawRect(inner, new Color(0.12f, 0.12f, 0.12f));

        // axes
        Handles.BeginGUI();
        Handles.color = Color.gray;
        Vector3 a1 = new Vector3(inner.x, inner.y + inner.height, 0);
        Vector3 a2 = new Vector3(inner.x + inner.width, inner.y + inner.height, 0);
        Handles.DrawLine(a1, a2);
        Handles.DrawLine(new Vector3(inner.x, inner.y, 0), new Vector3(inner.x, inner.y + inner.height, 0));
        Handles.EndGUI();

        // sample curve
        Vector3[] pts = new Vector3[sampleCount];
        for (int i = 0; i < sampleCount; i++) {
            float t = i / (float)(sampleCount - 1);
            float v = EvaluateEase(easeName, t);
            float x = inner.x + t * inner.width;
            float y = inner.y + (1 - v) * inner.height;
            pts[i] = new Vector3(x, y, 0);
        }

        Handles.BeginGUI();
        Handles.color = Color.cyan;
        Handles.DrawAAPolyLine(3f, pts);
        // draw points
        for (int i = 0; i < sampleCount; i += sampleCount / 16) {
            Handles.color = Color.white;
            Handles.DrawSolidDisc(pts[i], Vector3.forward, 2f);
        }
        Handles.EndGUI();

        // labels
        GUIStyle label = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.white } };
        GUI.Label(new Rect(inner.x, inner.y - 20, 200, 20), easeName, label);
        GUI.Label(new Rect(inner.x + inner.width - 80, inner.y + inner.height + 4, 80, 16), "t →", label);
        GUI.Label(new Rect(inner.x - 30, inner.y, 40, 16), "1", label);
        GUI.Label(new Rect(inner.x - 30, inner.y + inner.height - 8, 40, 16), "0", label);
    }

    float EvaluateEase(string easeName, float t) {
        // Normalize name
        string n = easeName.Trim();
        if (n.StartsWith("Ease.")) n = n.Substring(5);
        n = n.Replace(" ", "");

        // Common eases implemented here. This isn't a full PrimeTween port, but covers typical cases.
        switch (n) {
            case "Linear": return t;
            case "InQuad": return t * t;
            case "OutQuad": return 1 - (1 - t) * (1 - t);
            case "InOutQuad": return t < 0.5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
            case "InCubic": return t * t * t;
            case "OutCubic": return 1 - Mathf.Pow(1 - t, 3);
            case "InOutCubic": return t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
            case "InSine": return 1 - Mathf.Cos((t * Mathf.PI) / 2);
            case "OutSine": return Mathf.Sin((t * Mathf.PI) / 2);
            case "InOutSine": return -(Mathf.Cos(Mathf.PI * t) - 1) / 2;
            case "InBack": {
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1;
                    return c3 * t * t * t - c1 * t * t;
                }
            case "OutBack": {
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1;
                    return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
                }
            case "InOutBack": {
                    const float c1 = 1.70158f;
                    const float c2 = c1 * 1.525f;
                    if (t < 0.5f) {
                        return (Mathf.Pow(2 * t, 2) * ((c2 + 1) * 2 * t - c2)) / 2;
                    } else {
                        return (Mathf.Pow(2 * t - 2, 2) * ((c2 + 1) * (t * 2 - 2) + c2) + 2) / 2;
                    }
                }
            default:
                // try to parse keywords like OutQuad, InBack, etc.
                if (n.EndsWith("Quad")) {
                    if (n.StartsWith("In")) return t * t;
                    if (n.StartsWith("Out")) return 1 - Mathf.Pow(1 - t, 2);
                    if (n.StartsWith("InOut")) return t < 0.5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
                }
                if (n.EndsWith("Cubic")) {
                    if (n.StartsWith("In")) return t * t * t;
                    if (n.StartsWith("Out")) return 1 - Mathf.Pow(1 - t, 3);
                }
                if (n.EndsWith("Sine")) {
                    if (n.StartsWith("In")) return 1 - Mathf.Cos((t * Mathf.PI) / 2);
                    if (n.StartsWith("Out")) return Mathf.Sin((t * Mathf.PI) / 2);
                }
                // fallback
                return t;
        }
    }
}
