using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/// <summary>
/// Multi-image color extraction system that processes entire folders of PNG files to build comprehensive color palettes.
/// 
/// This system analyzes existing artwork to extract proven color combinations, enabling:
/// - Automatic palette generation from reference images
/// - Consistent color schemes across projects
/// - Rapid prototyping with established color harmony
/// - Batch processing of multiple art sources
/// 
/// Technical Design:
/// - Folder-based processing handles any number of PNG files automatically
/// - Left-to-right, top-to-bottom scanning ensures predictable color ordering
/// - Duplicate prevention across all images maintains palette efficiency
/// - Color tolerance filtering removes near-identical variants
/// - Per-image and global limits prevent palette bloat
/// 
/// Integration Strategy:
/// - Uses Resources folder for runtime/editor accessibility
/// - Preserves swatch 0 as default to prevent mixing user-assigned colors with SpriteRenderers that are assigned automatically
/// - Triggers automatic swatch reference updates via editor bridge
/// - Integrates with Unity's undo system for safe operations
/// </summary>
[System.Serializable]
public class ColorPaletteImageLoader
{
    [Header("Settings")]
    [Tooltip("Folder path in Resources containing PNG files (e.g., 'ColorPaletteImages')")]
    public string imagesFolderPath = "ColorPaletteImages";
    
    [Tooltip("Skip nearly identical colors (alpha differences less than this threshold)")]
    [Range(0f, 1f)]
    public float colorTolerance = 0.5f;

    [Tooltip("Skip too transparent colors (transparency value less than this threshold)")]
    [Range(0f, 1f)]
    public float minimalTransparencyValue = 1f;
    
    [Tooltip("Maximum number of colors to extract per image")]
    [Range(1, 32)]
    public int maxColorsPerImage = 16;
    
    [Tooltip("Maximum total colors across all images (excluding default swatch 0)")]
    [Range(1, 64)]
    public int maxTotalColors = 64;

    /// <summary>
    /// Main orchestration method that processes all PNG files in the designated folder and updates the ColorPalette.
    /// </summary>
    /// <returns>True if successful, false if no images found or other error</returns>
    public bool LoadColorsFromImages()
    {
        // Find all PNG files in the ColorPaletteImages folder
        List<Texture2D> paletteImages = LoadAllImagesFromFolder();
        
        if (paletteImages.Count == 0)
        {
            Debug.LogWarning($"[ColorPaletteImageLoader] No PNG files found in 'Resources/{imagesFolderPath}/' folder. " +
                           "Please place your palette images in Assets/Resources/ColorPaletteImages/");
            return false;
        }

        // Debug.Log($"[ColorPaletteImageLoader] Found {paletteImages.Count} images in 'Resources/{imagesFolderPath}/'");

        // Extract unique colors from all images
        List<Color> allExtractedColors = new();
        
        foreach (Texture2D image in paletteImages)
        {
            if (!image.isReadable)
            {
                Debug.LogError($"[ColorPaletteImageLoader] Texture '{image.name}' is not readable. " +
                              "Please set 'Read/Write Enabled' to true in the texture import settings.");
                continue;
            }

            List<Color> imageColors = ExtractUniqueColors(image, image.name);
            
            // Add colors from this image, avoiding duplicates across all images
            foreach (Color color in imageColors)
            {
                if (!IsColorSimilarToAny(color, allExtractedColors) && allExtractedColors.Count < maxTotalColors)
                {
                    allExtractedColors.Add(color);
                }
            }
            
            // Debug.Log($"[ColorPaletteImageLoader] Processed '{image.name}': {imageColors.Count} unique colors (Total so far: {allExtractedColors.Count})");
            
            if (allExtractedColors.Count >= maxTotalColors)
            {
                // Debug.Log($"[ColorPaletteImageLoader] Reached maximum total color limit ({maxTotalColors})");
                break;
            }
        }
        
        if (allExtractedColors.Count == 0)
        {
            Debug.LogWarning($"[ColorPaletteImageLoader] No unique colors found in any images from '{imagesFolderPath}' folder");
            return false;
        }

        // Load or create the ColorPalette ScriptableObject
        ColorPalette colorPalette = GetOrCreateColorPalette();
        
        // Update the palette (keeping swatch 0 as default)
        UpdateColorPalette(colorPalette, allExtractedColors);
        
        // Debug.Log($"[ColorPaletteImageLoader] Successfully loaded {allExtractedColors.Count} colors from {paletteImages.Count} images " +
        //         $"(Total swatches: {colorPalette.colors.Length})");
        
        return true;
    }

    /// <summary>
    /// Scans the designated Resources subfolder and loads all PNG files as Texture2D assets.
    /// </summary>
    private List<Texture2D> LoadAllImagesFromFolder()
    {
        List<Texture2D> images = new();
        
        #if UNITY_EDITOR
        // Get the full path to the Resources folder
        string resourcesFolderPath = Path.Combine(Application.dataPath, "Resources", imagesFolderPath);
        
        if (!Directory.Exists(resourcesFolderPath))
        {
            Directory.CreateDirectory(resourcesFolderPath);            
            // Refresh the AssetDatabase so Unity recognizes the new folder
            AssetDatabase.Refresh();
            
            // Since we just created the directory, it's empty, so return early
            Debug.LogWarning($"[ColorPaletteImageLoader] Directory had not been found and was just created meaning it's empty. Please add PNG files to: {resourcesFolderPath}");
            return images;
        }

        // Find all PNG files in the directory
        string[] pngFiles = Directory.GetFiles(resourcesFolderPath, "*.png", SearchOption.TopDirectoryOnly);
        
        foreach (string pngFile in pngFiles)
        {
            // Get the relative path from Resources folder
            string fileName = Path.GetFileNameWithoutExtension(pngFile);
            string resourcePath = Path.Combine(imagesFolderPath, fileName).Replace('\\', '/');
            
            // Load the texture
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
            {
                images.Add(texture);
                // Debug.Log($"[ColorPaletteImageLoader] Loaded image: {resourcePath}");
            }
            else
            {
                Debug.LogWarning($"[ColorPaletteImageLoader] Failed to load: {resourcePath}");
            }
        }
        #endif
        
        return images;
    }

    /// <summary>
    /// Extracts unique colors from a single texture using deterministic left-to-right, top-to-bottom scanning.
    /// 
    /// Performance considerations:
    /// - Uses HashSet for O(1) duplicate detection within the image
    /// - Applies transparency filtering early to avoid processing irrelevant pixels  
    /// - Implements per-image color limits to prevent single large images from dominating the palette
    /// - Converts to Color32 for consistent floating-point comparison behavior
    /// 
    /// Unity coordinate system note: Texture coordinates start from bottom-left, so y is inverted
    /// to achieve the desired top-to-bottom visual scanning order.
    /// </summary>
    private List<Color> ExtractUniqueColors(Texture2D texture, string imageName = "")
    {
        List<Color> uniqueColors = new();
        HashSet<Color32> seenColors = new();
        
        int width = texture.width;
        int height = texture.height;
        
        // Debug.Log($"[ColorPaletteImageLoader] Analyzing {imageName} {width}x{height} image ({width * height} pixels)");
        
        // Read pixels LEFT TO RIGHT, TOP TO BOTTOM
        for (int y = height - 1; y >= 0; y--) // Unity's texture coordinates start from bottom-left
        {
            for (int x = 0; x < width; x++)
            {
                Color pixelColor = texture.GetPixel(x, y);
                
                // Skip fully transparent pixels
                if (pixelColor.a < minimalTransparencyValue) continue;
                
                // Convert to Color32 for consistent comparison
                Color32 color32 = pixelColor;
                
                // Check if we've seen this color before (with tolerance)
                if (!IsColorSimilarToAny(pixelColor, uniqueColors))
                {
                    uniqueColors.Add(pixelColor);
                    seenColors.Add(color32);
                    
                    // Debug.Log($"[ColorPaletteImageLoader] Found unique color #{uniqueColors.Count} in {imageName}: " +
                    //          $"RGB({pixelColor.r:F3}, {pixelColor.g:F3}, {pixelColor.b:F3}) at pixel ({x}, {height - 1 - y})");
                    
                    // Stop if we've reached the maximum number of colors per image
                    if (uniqueColors.Count >= maxColorsPerImage)
                    {
                        Debug.LogWarning($"[ColorPaletteImageLoader] Reached maximum color limit per image ({maxColorsPerImage}) for {imageName}, stopping further extraction from this image. Adjust settings if needed.");
                        break;
                    }
                }
            }
            
            if (uniqueColors.Count >= maxColorsPerImage) break;
        }
        
        return uniqueColors;
    }

    /// <summary>
    /// Determines if a color is too similar to existing colors based on Manhattan distance calculation.
    /// 
    /// The tolerance system enables:
    /// - Filtering out compression artifacts and slight color variations
    /// - Maintaining palette efficiency by avoiding redundant similar colors
    /// - User control over color precision vs. palette size trade-offs
    /// - Consistent behavior across different image formats and quality settings
    /// 
    /// Includes alpha channel in comparison to handle transparency variations properly.
    /// </summary>
    private bool IsColorSimilarToAny(Color newColor, List<Color> existingColors)
    {
        foreach (Color existingColor in existingColors)
        {
            float distance = Mathf.Abs(newColor.r - existingColor.r) + 
                           Mathf.Abs(newColor.g - existingColor.g) + 
                           Mathf.Abs(newColor.b - existingColor.b) + 
                           Mathf.Abs(newColor.a - existingColor.a);
            
            if (distance < colorTolerance)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Retrieves the existing ColorPalette asset or creates a new one with sensible defaults.
    /// </summary>
    private ColorPalette GetOrCreateColorPalette()
    {
        ColorPalette palette = Resources.Load<ColorPalette>("ColorPalette");
        
        if (palette == null)
        {
            Debug.Log("[ColorPaletteImageLoader] Creating new ColorPalette asset");
            palette = ScriptableObject.CreateInstance<ColorPalette>();
            palette.colors = new Color[] { Color.white }; // Default swatch 0
            
            #if UNITY_EDITOR
            // Create Resources folder if it doesn't exist
            string resourcesPath = "Assets/Resources";
            if (!AssetDatabase.IsValidFolder(resourcesPath))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }
            
            // Save the new palette
            string assetPath = "Assets/Resources/ColorPalette.asset";
            AssetDatabase.CreateAsset(palette, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            #endif
        }
        
        return palette;
    }

    /// <summary>
    /// Updates the ColorPalette with extracted colors, preserving swatch 0 as default
    /// </summary>
    private void UpdateColorPalette(ColorPalette palette, List<Color> extractedColors)
    {
        // Create new color array: [default_swatch_0] + [extracted_colors]
        Color[] newColors = new Color[extractedColors.Count + 1];
        
        // Keep swatch 0 as the default color (preserve existing or use white)
        newColors[0] = palette.colors.Length > 0 ? palette.colors[0] : Color.white;
        
        // Add extracted colors starting from swatch 1
        for (int i = 0; i < extractedColors.Count; i++)
        {
            newColors[i + 1] = extractedColors[i];
        }
        
        // Update the palette
        #if UNITY_EDITOR
        Undo.RecordObject(palette, "Load Colors from Image");
        #endif
        
        palette.colors = newColors;
        
        #if UNITY_EDITOR
        EditorUtility.SetDirty(palette);
        AssetDatabase.SaveAssets();
        
        // Trigger swatch reference updates
        SwatchEditorBase.UpdateAllSwatchReferences();
        #endif
        
        // Debug.Log($"[ColorPaletteImageLoader] Updated ColorPalette: Swatch 0 (default) + {extractedColors.Count} extracted colors");
    }
}

#if UNITY_EDITOR
/// <summary>
/// Editor utility window for loading colors from ColorPaletteImage
/// </summary>
public class ColorPaletteImageLoaderWindow : EditorWindow
{
    private readonly ColorPaletteImageLoader loader = new();
    private Vector2 scrollPosition;

    [MenuItem("Tools/Swatch System/Load Colors from PNGs")]
    public static void ShowWindow()
    {
        GetWindow<ColorPaletteImageLoaderWindow>("Color Palette Image Loader");
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        EditorGUILayout.LabelField("Color Palette Image Loader", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        EditorGUILayout.HelpBox(
            "This tool extracts colors from ALL PNG files in a folder and updates your ColorPalette.\n\n" +
            "Color Extraction Order:\n" +
            "• Processes each PNG file in the ColorPaletteImages folder\n" +
            "• Per image: Left to Right, Top to Bottom (like reading text)\n\n" +
            "• First unique color across all images → Swatch 1\n" +
            "• Second unique color → Swatch 2, etc.\n\n" +
            "Swatch 0 remains as default (not replaced)",
            MessageType.Info);
        
        EditorGUILayout.Space();
        
        // Settings
        EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
        loader.imagesFolderPath = EditorGUILayout.TextField("Images Folder (in Resources)", loader.imagesFolderPath);
        loader.colorTolerance = EditorGUILayout.Slider("Color Tolerance", loader.colorTolerance, 0f, 1f);
        loader.minimalTransparencyValue = EditorGUILayout.Slider("Minimal Color Transparency", loader.minimalTransparencyValue, 0f, 1f);
        loader.maxColorsPerImage = EditorGUILayout.IntSlider("Max Colors Per Image", loader.maxColorsPerImage, 1, 32);
        loader.maxTotalColors = EditorGUILayout.IntSlider("Max Total Colors", loader.maxTotalColors, 1, 64);
        
        EditorGUILayout.Space();
        
        // Load button
        if (GUILayout.Button("Load Colors from Images", GUILayout.Height(40)))
        {
            bool success = loader.LoadColorsFromImages();
            if (success)
            {
                EditorUtility.DisplayDialog("Success", 
                    $"Successfully loaded colors from all PNG files in '{loader.imagesFolderPath}' folder!\nCheck the Console for details.", 
                    "OK");
            }
        }
        
        EditorGUILayout.Space();
        
        // Instructions
        EditorGUILayout.LabelField("Instructions:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("1. Create folder: Assets/Resources/ColorPaletteImages/", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("2. Place all your PNG files in that folder", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("3. Enable 'Read/Write Enabled' in texture import settings (Advanced)", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("4. Adjust settings as needed especially the color tolerance to make sure colors extracted are less than Max Colors", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("5. Click 'Load Colors from Images'", EditorStyles.wordWrappedLabel);

        EditorGUILayout.EndScrollView();
    }
}
#endif