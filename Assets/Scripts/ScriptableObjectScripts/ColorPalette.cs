using UnityEngine;

[CreateAssetMenu(fileName = "ColorPalette", menuName = "Palette/ColorPalette")]
public class ColorPalette : ScriptableObject
{
    public Color[] colors;
    public string[] names;
}
