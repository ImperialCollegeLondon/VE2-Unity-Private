using UnityEngine;

namespace VE2.Core.Common
{
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

        public Color PointerIdleColor; 
        public Color PointerHighlightColor;
    }
}

