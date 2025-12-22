using UnityEngine;

[RequireComponent(typeof(Transform))]
[ExecuteAlways]
public class TransformFullScreenScaler : MonoBehaviour
{
    [SerializeField] private Camera cam;
    private Transform tr;

    private int lastWidth;
    private int lastHeight;

    void Start()
    {
        if (tr == null)
            tr = GetComponent<Transform>();
        if (cam == null)
            cam = Camera.main;

        Resize();
    }

    private void OnValidate()
    {
        // Called in editor when serialized fields change
        Resize();
    }

    private void Update()
    {
        if (Screen.width != lastWidth || Screen.height != lastHeight)
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            Resize();
        }
    }

    private void Resize()
    {
        if (cam == null)
            cam = Camera.main;
        if (cam == null)
            return;
        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;

        if (tr == null)
            tr = GetComponent<Transform>();

        tr.localScale = new Vector2(width, height);
    }
}
