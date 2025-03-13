using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VE2.Core.Common
{

    [ExecuteInEditMode]
    public class V_ColorAssignment : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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
            Secondary,
            Close,
        }

        [SerializeField, HideIf(nameof(_hasButton), true)] private ColorType _colorType;
        [SerializeField, HideIf(nameof(_hasButton), false)] private ButtonType _buttonType;

        private bool _hasButton => _button != null; 

        private Image _image;
        private TMP_Text _text;
        private Button _button;

        private Image _subImage;
        private bool _hasSubImage => _subImage != null;

        private TMP_Text _subText;
        private bool _hasSubText => _subText != null;

        private ColorConfiguration _colorConfiguration;

        private void Awake()
        {
            _image = GetComponent<Image>();
            _text = GetComponent<TMP_Text>();

            _subImage = GetComponentsInChildren<Image>(true)
                .FirstOrDefault(img => img.gameObject != gameObject);

            _subText = GetComponentInChildren<TMP_Text>();

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
                    {
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

                    if (_hasSubImage)
                        _subImage.color = _colorConfiguration.TertiaryColor;
                    if (_hasSubText)
                        _subText.color = _colorConfiguration.TertiaryColor;
                    }

                    break;
                case ButtonType.Secondary:
                    if (_button != null)
                        _button.colors = new ColorBlock
                        {
                            
                        };
                    break;
                case ButtonType.Close:
                    if (_button != null)
                        _button.colors = new ColorBlock
                        {
                            normalColor = _colorConfiguration.TertiaryColor,
                            highlightedColor = Color.red,
                            pressedColor = Color.red,
                            selectedColor = _colorConfiguration.TertiaryColor,
                            disabledColor = _colorConfiguration.ButtonDisabledColor,
                            colorMultiplier = 1,
                            fadeDuration = 0.1f
                        };
                    break;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_hasButton)
                return;

            switch (_buttonType)
            {
                case ButtonType.Standard:
                    if (_hasSubImage)
                        _subImage.color = _colorConfiguration.AccentSecondaryColor;
                    if (_hasSubText)
                        _subText.color = _colorConfiguration.AccentSecondaryColor;
                    break;
                case ButtonType.Secondary:
                    break;
                case ButtonType.Close:
                    break;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Debug.Log($"Button {_button.name} no longer highlighted.");
        }
    }
}
