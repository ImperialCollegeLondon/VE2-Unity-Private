using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VE2.Core.Common
{

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

    private enum ButtonType
    {
        Standard, 
        Secondary
    }

    [SerializeField, HideIf(nameof(_hasButton), true)] private ColorType _colorType;
    [SerializeField, HideIf(nameof(_hasButton), false)] private ButtonType _buttonType;

    private bool _hasButton => _button != null; 

    private Image _image;
    private TMP_Text _text;
    private Button _button;

    private ColorConfiguration _colorConfiguration;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _text = GetComponent<TMP_Text>();
        _button = GetComponent<Button>();
        _colorConfiguration = (ColorConfiguration)Resources.Load("ColorConfiguration");
        UpdateColor();
    }

    private void OnValidate()
    {
        UpdateColor();
    }

    private void UpdateColor() 
    {
        switch (_colorType)
        {
            case ColorType.Primary:
                if (_image != null && !_hasButton)
                    _image.color = _colorConfiguration.PrimaryColor;
                if (_text != null)
                    _text.color = _colorConfiguration.PrimaryColor;
                break;
            case ColorType.Secondary:
                if (_image != null && !_hasButton)
                    _image.color = _colorConfiguration.SecondaryColor;
                if (_text != null)
                    _text.color = _colorConfiguration.SecondaryColor;
                break;
            case ColorType.Tertiary:
                if (_image != null && !_hasButton)
                    _image.color = _colorConfiguration.TertiaryColor;
                if (_text != null)
                    _text.color = _colorConfiguration.TertiaryColor;
                break;
            case ColorType.Quaternary:
                if (_image != null && !_hasButton)
                    _image.color = _colorConfiguration.QuaternaryColor;
                if (_text != null)
                    _text.color = _colorConfiguration.QuaternaryColor;
                break;
            case ColorType.AccentPrimary:
                if (_image != null && !_hasButton)
                    _image.color = _colorConfiguration.AccentPrimaryColor;
                if (_text != null)
                    _text.color = _colorConfiguration.AccentPrimaryColor;
                break;
            case ColorType.AccentSecondary:
                if (_image != null && !_hasButton)
                    _image.color = _colorConfiguration.AccentSecondaryColor;
                if (_text != null)
                    _text.color = _colorConfiguration.AccentSecondaryColor;
                break;
        }

        switch (_buttonType)
        {
            case ButtonType.Standard:
                if (_button != null)
                    _button.colors = new ColorBlock
                    {
                        //TODO: add explicit button colors to colour config
                        normalColor = _colorConfiguration.SecondaryColor,
                        highlightedColor = _colorConfiguration.AccentSecondaryColor,
                        pressedColor = _colorConfiguration.AccentSecondaryColor,
                        selectedColor = _colorConfiguration.AccentPrimaryColor,
                        disabledColor = _colorConfiguration.ButtonDisabledColor,
                        colorMultiplier = 1,
                        fadeDuration = 0.1f
                    };
                break;
            case ButtonType.Secondary:
                if (_button != null)
                    _button.colors = new ColorBlock
                    {
                        
                    };
                break;
        }
    }
}
}
