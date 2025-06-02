using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace VE2.Common.Shared
{
    [AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
    internal class RadialProgressBar : MonoBehaviour
    {
        [SerializeField] private float _maxValue = 100;

        [SerializeField, Disable] private float _currentValue;

        [SerializeField] public Image _loadingBar;
        [SerializeField] private TextMeshProUGUI _loadingText;

        [SerializeField] private bool _addSuffix = true;
        [SerializeField, ShowIf(nameof(_addSuffix), true)] private string suffix = "%";

        // [SerializeField] private bool tweenProgressBar = false;
        // [SerializeField, ShowIf(nameof(_addSuffix), true)]  private float tweenDuration = 0.25f;

        private void Awake()
        {
            _loadingBar.fillAmount = _currentValue / _maxValue;
            SetLoadingText();
        }
        private void UpdateBarAndText()
        {
            // if (!tweenProgressBar)
            // {
            //     _loadingBar.fillAmount = _currentPercent / _maxValue;
            // }
            // else
            // {
            //     _loadingBar.DOFillAmount(_currentPercent / _maxValue, tweenDuration);
            // }
            _loadingBar.fillAmount = _currentValue / _maxValue;
            SetLoadingText();
        }

        private void SetLoadingText()
        {
            if (_loadingText != null)
            {
                if (_addSuffix)
                {
                    _loadingText.text = _currentValue.ToString() + suffix;
                }
                else
                {
                    _loadingText.text = _currentValue.ToString();
                }
            }
        }

        public void SetValue(float value)
        {
            _currentValue = value;
            UpdateBarAndText();
        }
    }
}