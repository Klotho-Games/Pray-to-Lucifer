using UnityEngine;
using UnityEditor;

/// <summary>
/// Automatically adds IColorable wrapper components when certain components are added.
/// </summary>
[InitializeOnLoad]
public class ColorableComponentAutoAdder
{
    static ColorableComponentAutoAdder()
    {
        ObjectFactory.componentWasAdded += OnComponentAdded;
    }

    private static void OnComponentAdded(Component component)
    {
        if (component == null) return;

        // Auto-add ColorableSpriteRenderer when SpriteRenderer is added
        if (component is SpriteRenderer)
        {
            var gameObject = component.gameObject;
            if (gameObject.GetComponent<ColorableSpriteRenderer>() == null)
            {
                gameObject.AddComponent<ColorableSpriteRenderer>();
                Debug.Log($"Auto-added ColorableSpriteRenderer to {gameObject.name}");
            }
        }

        // Auto-add ColorableImage when UI.Image is added
        if (component is UnityEngine.UI.Image)
        {
            var gameObject = component.gameObject;
            if (gameObject.GetComponent<ColorableImage>() == null)
            {
                gameObject.AddComponent<ColorableImage>();
                Debug.Log($"Auto-added ColorableImage to {gameObject.name}");
            }
        }
    }

    /// <summary>
    /// Menu item to add ColorableSpriteRenderer to all existing SpriteRenderers in the scene.
    /// </summary>
    [MenuItem("Tools/Colorable/Add Wrappers to All SpriteRenderers in Scene")]
    public static void AddColorableToAllSpriteRenderers()
    {
        var spriteRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        int addedCount = 0;

        foreach (var sr in spriteRenderers)
        {
            if (sr.GetComponent<ColorableSpriteRenderer>() == null)
            {
                sr.gameObject.AddComponent<ColorableSpriteRenderer>();
                addedCount++;
            }
        }

        Debug.Log($"Added ColorableSpriteRenderer to {addedCount} GameObjects");
        EditorUtility.DisplayDialog("Complete", $"Added ColorableSpriteRenderer to {addedCount} GameObjects with SpriteRenderer", "OK");
    }

    /// <summary>
    /// Menu item to add ColorableImage to all existing UI Images in the scene.
    /// </summary>
    [MenuItem("Tools/Colorable/Add Wrappers to All UI Images in Scene")]
    public static void AddColorableToAllImages()
    {
        var images = Object.FindObjectsByType<UnityEngine.UI.Image>(FindObjectsSortMode.None);
        int addedCount = 0;

        foreach (var img in images)
        {
            if (img.GetComponent<ColorableImage>() == null)
            {
                img.gameObject.AddComponent<ColorableImage>();
                addedCount++;
            }
        }

        Debug.Log($"Added ColorableImage to {addedCount} GameObjects");
        EditorUtility.DisplayDialog("Complete", $"Added ColorableImage to {addedCount} GameObjects with UI.Image", "OK");
    }
}
