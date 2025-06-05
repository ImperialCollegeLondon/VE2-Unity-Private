using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VE2.Common.Shared;

namespace VE2.Common.Shared
{

    [ExecuteInEditMode]
    internal class V_UIColorHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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
        [SerializeField] private float _colorAlpha = 1f;
        [SerializeField, HideIf(nameof(_hasButton), false)] private ButtonType _buttonType;
        //[SerializeField, ]


        private bool _hasButton => _button != null; 

        private Image _image;
        private TMP_Text _text;
        private Button _button;

        private Image _subImage;
        private bool _hasSubImage => _subImage != null;

        private TMP_Text _subText;
        private bool _hasSubText => _subText != null;

        private bool _lockedToSelectedColor = false;
        private bool _isHighlighted = false;
        private ColorBlock _buttonNonSelectedColors;
        private Color _buttonSubElementsSelectedColor;
        private Color _buttonSubElementsNonSelectedColor;
        private Color _buttonSubElementsHighlightedColor;

        private ColorConfiguration _colorConfiguration => ColorConfiguration.Instance;

        private bool _doneSetup = false;

        private void Awake()
        {
            Setup();
        }

        internal void Setup()
        {
            if (_doneSetup)
                return;

            _image = GetComponent<Image>();
            _text = GetComponent<TMP_Text>();

            _subImage = GetComponentsInChildren<Image>(true)
                .FirstOrDefault(img => img.gameObject != gameObject && !img.gameObject.name.ToUpper().Contains("NOCOLORUIHANDLER"));

            _subText = GetComponentInChildren<TMP_Text>();

            _button = GetComponent<Button>();

            UpdateColor();
            AssignButtonColors();

            _doneSetup = true;
        }

        internal void LockSelectedColor() 
        {
            if (!_hasButton) //says there is a button, and awake has not been done
            {
                Debug.LogError("Cannot lock selected color on a non-button object - " + (GetComponent<Button>() == null ? "no button" : "has button") + " go name = " + gameObject.name + " doneAwake = " + _doneSetup); 
                return;
            }

            _lockedToSelectedColor = true;
            SetToSelectedColors();
        }

        internal void UnlockSelectedColor()
        {
            if (!_hasButton)
            {
                Debug.LogError("Cannot lock selected color on a non-button object");
                return;
            }

            _lockedToSelectedColor = false;
            SetToNonSelectedColors();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
                UpdateColor();
        }

        Color ApplyAlpha(Color color) => new Color(color.r, color.g, color.b, _colorAlpha);

        private void UpdateColor()
        {
            switch (_colorType)
            {
                case ColorType.Primary:
                    if (_image != null && !_hasButton)
                        _image.color = ApplyAlpha(_colorConfiguration.PrimaryColor);
                    if (_text != null)
                        _text.color = ApplyAlpha(_colorConfiguration.PrimaryColor);
                    break;
                case ColorType.Secondary:
                    if (_image != null && !_hasButton)
                        _image.color = ApplyAlpha(_colorConfiguration.SecondaryColor);
                    if (_text != null)
                        _text.color = ApplyAlpha(_colorConfiguration.SecondaryColor);
                    break;
                case ColorType.Tertiary:
                    if (_image != null && !_hasButton)
                        _image.color = ApplyAlpha(_colorConfiguration.TertiaryColor);
                    if (_text != null)
                        _text.color = ApplyAlpha(_colorConfiguration.TertiaryColor);
                    break;
                case ColorType.Quaternary:
                    if (_image != null && !_hasButton)
                        _image.color = ApplyAlpha(_colorConfiguration.QuaternaryColor);
                    if (_text != null)
                        _text.color = ApplyAlpha(_colorConfiguration.QuaternaryColor);
                    break;
                case ColorType.AccentPrimary:
                    if (_image != null && !_hasButton)
                        _image.color = ApplyAlpha(_colorConfiguration.AccentPrimaryColor);
                    if (_text != null)
                        _text.color = ApplyAlpha(_colorConfiguration.AccentPrimaryColor);
                    break;
                case ColorType.AccentSecondary:
                    if (_image != null && !_hasButton)
                        _image.color = ApplyAlpha(_colorConfiguration.AccentSecondaryColor);
                    if (_text != null)
                        _text.color = ApplyAlpha(_colorConfiguration.AccentSecondaryColor);
                    break;
            }

            if (_hasButton)
            {
                AssignButtonColors();
                SetToNonSelectedColors();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHighlighted = true;

            if (!_hasButton)
                return;

            if (_hasSubImage)
                _subImage.color = _buttonSubElementsHighlightedColor;
            if (_hasSubText)
                _subText.color = _buttonSubElementsHighlightedColor;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHighlighted = false;

            if (!_hasButton)
                return;
            
            if (_lockedToSelectedColor)
                SetToSelectedColors();
            else 
                SetToNonSelectedColors();

        }

        private void SetToSelectedColors()
        {
            _button.colors = new ColorBlock {
                normalColor = ApplyAlpha(_buttonNonSelectedColors.selectedColor),
                highlightedColor = ApplyAlpha(_buttonNonSelectedColors.highlightedColor),
                pressedColor = ApplyAlpha(_buttonNonSelectedColors.pressedColor),
                selectedColor = ApplyAlpha(_buttonNonSelectedColors.selectedColor),
                disabledColor = ApplyAlpha(_buttonNonSelectedColors.disabledColor),
                colorMultiplier = 1,
                fadeDuration = 0.1f,
            };

            if (!_isHighlighted) //Otherwise, OnPointerExit will take care of this
            {
                if (_hasSubImage)
                    _subImage.color = _buttonSubElementsSelectedColor;

                if (_hasSubText)
                    _subText.color =_buttonSubElementsSelectedColor;
            }
        }

        private void SetToNonSelectedColors()
        {
            _button.colors = _buttonNonSelectedColors;

            if (_hasSubImage)
                _subImage.color = _buttonSubElementsNonSelectedColor;

            if (_hasSubText)
                _subText.color =_buttonSubElementsNonSelectedColor;
        }

        private void AssignButtonColors() 
        {
            if (!_hasButton)
                return;

            switch (_buttonType)
            {
                case ButtonType.Standard:
                {
                    _buttonNonSelectedColors = new ColorBlock
                    {
                        normalColor = ApplyAlpha(_colorConfiguration.SecondaryColor),
                        highlightedColor = ApplyAlpha(_colorConfiguration.AccentSecondaryColor),
                        pressedColor = ApplyAlpha(_colorConfiguration.AccentSecondaryColor * 0.8f),
                        selectedColor = ApplyAlpha(_colorConfiguration.AccentPrimaryColor),
                        disabledColor = ApplyAlpha(_colorConfiguration.ButtonDisabledColor),
                        colorMultiplier = 1,
                        fadeDuration = 0.1f
                    };
                    _buttonSubElementsNonSelectedColor = _colorConfiguration.TertiaryColor;
                    _buttonSubElementsSelectedColor = _colorConfiguration.QuaternaryColor;
                    _buttonSubElementsHighlightedColor = _colorConfiguration.TertiaryColor;
                    break;
                }
                case ButtonType.Secondary:
                {
                    _buttonNonSelectedColors = new ColorBlock
                    {
                        normalColor = ApplyAlpha(_colorConfiguration.TertiaryColor),
                        highlightedColor = ApplyAlpha(_colorConfiguration.AccentSecondaryColor),
                        pressedColor = ApplyAlpha(_colorConfiguration.AccentSecondaryColor * 0.8f),
                        selectedColor = ApplyAlpha(_colorConfiguration.AccentPrimaryColor),
                        disabledColor = ApplyAlpha(_colorConfiguration.ButtonDisabledColor),
                        colorMultiplier = 1,
                        fadeDuration = 0.1f
                    };
                    _buttonSubElementsNonSelectedColor = ApplyAlpha(_colorConfiguration.TertiaryColor);
                    _buttonSubElementsSelectedColor = ApplyAlpha(_colorConfiguration.QuaternaryColor);
                    _buttonSubElementsHighlightedColor = ApplyAlpha(_colorConfiguration.TertiaryColor);
                    break;
                }
        
                case ButtonType.Close:
                {
                    _buttonNonSelectedColors = new ColorBlock
                    {
                        normalColor = ApplyAlpha(_colorConfiguration.TertiaryColor),
                        highlightedColor = Color.red,
                        pressedColor = Color.red  * 0.8f,
                        selectedColor = ApplyAlpha(_colorConfiguration.TertiaryColor),
                        disabledColor = ApplyAlpha(_colorConfiguration.ButtonDisabledColor),
                        colorMultiplier = 1,
                        fadeDuration = 0.1f
                    };

                    _buttonSubElementsNonSelectedColor = _colorConfiguration.QuaternaryColor;
                    _buttonSubElementsSelectedColor = _colorConfiguration.TertiaryColor;
                    _buttonSubElementsHighlightedColor = _colorConfiguration.QuaternaryColor;
                    break;
                }
            }
        }
    }
}