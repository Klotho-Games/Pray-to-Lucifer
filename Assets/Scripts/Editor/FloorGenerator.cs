using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using Codice.Client.Common.GameUI;

/// <summary>
/// Automatic floor generation using tile assets
///  with a possibility to save the map to prefabs
/// 
/// Technical Design:
/// - Creates a random map of tiles,
/// - Iterates to create more natural patterns,
/// - Randomly places decorative elements
///  satisfying their spawn conditions,
/// - Allows to save the generated map as a prefab for reuse.
/// </summary>
[System.Serializable]
public class FloorGenerator
{
    [Header("Setup")]
    [Tooltip("Folder path in Resources that the generation can be saved (e.g., 'FloorMaps')")]
    public string mapFolderPath = "FloorMaps";

    [Tooltip("List of floor tile sprites to use for floor generation")]
    public List<Sprite> floorTileSprites;

    [Tooltip("List of decorative element sprites to randomly place on the floor")]
    public List<Sprite> decorativeElementSprites;

    public bool GenerateFloorMap()
    {
        // Placeholder for floor map generation logic
        // This would include creating a grid of tiles,
        // applying randomization, and placing decorative elements
        Debug.Log("Generating floor map...");

        return PlaceDecorativeElements();
    }
    
    private bool PlaceDecorativeElements()
    {
        // Placeholder for decorative element placement logic
        Debug.Log("Placing decorative elements...");

        return true;
    }
}

#if UNITY_EDITOR
/// <summary>
/// Editor utility window for generating a new floor map
/// </summary>
public class FloorGeneratorLoaderWindow : EditorWindow
{
    private readonly FloorGenerator generator = new();
    private SerializedObject serializedGenerator;
    private SerializedProperty floorTileSpritesProperty;

    [MenuItem("Tools/Generate New Floor")]
    public static void ShowWindow()
    {
        GetWindow<FloorGeneratorLoaderWindow>("Floor Generator");
    }

    private void OnGUI()
    {
        if (serializedGenerator == null)
        {
            serializedGenerator = new SerializedObject(this);
            serializedGenerator.Update();
        }

        EditorGUILayout.LabelField("Floor Generator", EditorStyles.boldLabel);
        {
            EditorGUILayout.LabelField("Floor Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();


            // Setup
            EditorGUILayout.LabelField("Setup", EditorStyles.boldLabel);
            generator.mapFolderPath = EditorGUILayout.TextField("Map Save Folder (in Resources)", generator.mapFolderPath);

            EditorGUILayout.Space();

            EditorGUILayout.BeginFoldoutHeaderGroup(true, "Floor Tile Sprites in order");
            {
                EditorGUILayout.LabelField("Floor Tile Sprites:");
                for (int i = 0; i < generator.floorTileSprites.Count; i++)
                {
                    generator.floorTileSprites[i] = EditorGUILayout.ObjectField($"Tile {i}", generator.floorTileSprites[i], typeof(Sprite), false) as Sprite;
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.BeginFoldoutHeaderGroup(true, "Decoration Sprites in order");
            {
                EditorGUILayout.LabelField("Decoration Sprites:");
                for (int i = 0; i < generator.decorativeElementSprites.Count; i++)
                {
                    generator.decorativeElementSprites[i] = EditorGUILayout.ObjectField($"Decoration {i}", generator.decorativeElementSprites[i], typeof(Sprite), false) as Sprite;
                }
            }
            
            if (GUILayout.Button("Add Sprite"))
            {
                generator.floorTileSprites.Add(null);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space();

            // Load button
            if (GUILayout.Button("Load Colors from Images", GUILayout.Height(40)))
            {
                bool success = generator.GenerateFloorMap();
                if (success)
                {
                    EditorUtility.DisplayDialog("Success",
                        $"Successfully generated a map, look at it in the Scene or Game view!", "OK");
                }
            }
        }
    }
}
#endif