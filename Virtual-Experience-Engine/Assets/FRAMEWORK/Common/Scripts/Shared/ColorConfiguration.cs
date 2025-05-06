using UnityEngine;

namespace VE2.Common.Shared
{
    [CreateAssetMenu(fileName = "ColorConfiguration", menuName = "Scriptable Objects/ColorConfiguration")]
    internal class ColorConfiguration : ScriptableObject
    {
        private static ColorConfiguration _instance;
        public static ColorConfiguration Instance { 
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<ColorConfiguration>("ColorConfiguration");

                if (_instance == null)
                    Debug.LogError("ColorConfiguration instance not found. Please create a ColorConfiguration asset in the Resources folder.");

                return _instance;
            }
        }

        public Color PrimaryColor;
        public Color SecondaryColor; 
        public Color TertiaryColor;
        public Color QuaternaryColor;
        public Color AccentPrimaryColor;
        public Color AccentSecondaryColor;

        public Color ButtonDisabledColor;

        public Color PointerIdleColor; 
        public Color PointerHighlightColor;

        public Color TeleportValidColor;
        public Color TeleportInvalidColor;
    }
}

