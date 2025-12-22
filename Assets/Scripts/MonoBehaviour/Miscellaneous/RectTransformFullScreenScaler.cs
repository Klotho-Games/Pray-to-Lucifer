using UnityEngine;

[RequireComponent(typeof(RectTransform))]
[ExecuteAlways]
public class RectTransformFullScreenScaler : MonoBehaviour
{
    [SerializeField] private Camera cam;
    private RectTransform rect;

    void Start()
    {
        if (rect == null)
            rect = GetComponent<RectTransform>();
        if (cam == null)
            cam = Camera.main;

        Resize();
    }

    private void OnValidate()
    {
        // Called in editor when serialized fields change
        Resize();
    }


    private void Resize()
    {
        if (cam == null)
            cam = Camera.main;
        if (cam == null)
            return;
        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;

        if (rect == null)
            rect = GetComponent<RectTransform>();

        rect.sizeDelta = new Vector2(width, height);
    }
}
