using UnityEngine;

[CreateAssetMenu(fileName = "ColorConfiguration", menuName = "Scriptable Objects/ColorConfiguration")]
public class ColorConfiguration : ScriptableObject
{
    public Color PrimaryColor;
    public Color SecondaryColor; 
    public Color TertiaryColor;
    public Color QuaternaryColor;
    public Color AccentPrimaryColor;
    public Color AccentSecondaryColor;

    public Color ButtonDisabledColor;
}
