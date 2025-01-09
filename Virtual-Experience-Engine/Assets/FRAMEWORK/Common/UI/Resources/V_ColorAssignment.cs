using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class V_ColorAssignment : MonoBehaviour
{
    private enum ColorType
    {
        Primary,
        Secondary,
        Tertiary,
        Quaternary,
        AccentPrimary,
        AccentSecondary
    }

    [SerializeField] private ColorType _colorType;

    private Image _image;
    private ColorConfiguration _colorConfiguration;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _colorConfiguration = (ColorConfiguration)Resources.Load("ColorConfiguration");
        UpdateColor();
    }

    private void OnValidate()
    {
        if (_image == null)
            return;

        UpdateColor();
    }

    private void UpdateColor() 
    {
        switch (_colorType)
        {
            case ColorType.Primary:
                _image.color = _colorConfiguration.PrimaryColor;
                break;
            case ColorType.Secondary:
                _image.color = _colorConfiguration.SecondaryColor;
                break;
            case ColorType.Tertiary:
                _image.color = _colorConfiguration.TertiaryColor;
                break;
            case ColorType.Quaternary:
                _image.color = _colorConfiguration.QuaternaryColor;
                break;
            case ColorType.AccentPrimary:
                _image.color = _colorConfiguration.AccentPrimaryColor;
                break;
            case ColorType.AccentSecondary:
                _image.color = _colorConfiguration.AccentSecondaryColor;
                break;
        }
    }
}
