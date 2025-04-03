using DG.Tweening;
using UnityEngine;

namespace VE2.Core.Common
{
    public class V_LightTweener : MonoBehaviour
    {
        [Title("Light Intensity Tweening")]
        [SerializeField] private bool _tweenLight = false;
        [BeginGroup(ApplyCondition = true), SerializeField, HideIf(nameof(_tweenLight), false)] private Light _light;
        [SerializeField, HideIf(nameof(_tweenLight), false)] private float _minIntensity = 0.5f;
        [SerializeField, EndGroup(ApplyCondition = true), HideIf(nameof(_tweenLight), false)] private float _maxIntensity = 1.5f;

        [Title("Emissive Intensity Tweening")]
        [SerializeField] private bool _tweenEmission = false;
        [BeginGroup(ApplyCondition = true), SerializeField, HideIf(nameof(_tweenEmission), false)] private Renderer _renderer;
        [SerializeField, HideIf(nameof(_tweenEmission), false)] private float _minEmissiveIntensity = 0.5f;
        [SerializeField, HideIf(nameof(_tweenEmission), false)] private float _maxEmissiveIntensity = 1.5f;
        [SerializeField, HideIf(nameof(_tweenEmission), false)] private int _emissiveMaterialIndex = 0;
        [SerializeField, EndGroup(ApplyCondition = true), HideIf(nameof(_tweenEmission), false)] private Color _emissiveColor = Color.white;

        [SerializeField, SpaceArea(spaceBefore: 5)] private float _tweenDuration = 1f;
        [SerializeField] private Ease _easeType = Ease.Linear;
        [SerializeField] private bool _startAtMin = true;

        private Tween _currentLightTween;
        private Tween _currentEmissiveTween;
        private float _currentEmissiveIntensity;
        private Material _emissiveMaterial => _renderer.materials[_emissiveMaterialIndex];

        private void Awake()
        {
            if (_tweenEmission)
            {
                _emissiveMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                _renderer.material.EnableKeyword("_EMISSION");
            }

            if (_startAtMin)
                GoToMin();
        }

        public void GoToMax()
        {
            if (_tweenLight)
            {
                _currentLightTween?.Kill();
                _currentLightTween = _light.DOIntensity(_maxIntensity, _tweenDuration).SetEase(_easeType);
            }

            if (_tweenEmission)
            {
                _currentEmissiveTween?.Kill();
                _currentEmissiveTween = DOVirtual.Float(_currentEmissiveIntensity, _maxEmissiveIntensity, _tweenDuration, value =>
                {
                    _currentEmissiveIntensity = value;
                    _emissiveMaterial.SetColor("_EmissionColor", _emissiveColor * _currentEmissiveIntensity);
                    DynamicGI.SetEmissive(_renderer, _emissiveColor * _currentEmissiveIntensity);
                }).SetEase(_easeType);
            }
        }

        public void GoToMin()
        {
            if (_tweenLight)
            {
                _currentLightTween?.Kill();
                _currentLightTween = _light.DOIntensity(_minIntensity, _tweenDuration).SetEase(_easeType);
            }

            if (_tweenEmission)
            {
                _currentEmissiveTween?.Kill();
                _currentEmissiveTween = DOVirtual.Float(_currentEmissiveIntensity, _minEmissiveIntensity, _tweenDuration, value =>
                {
                    _currentEmissiveIntensity = value;
                    _emissiveMaterial.SetColor("_EmissionColor", _emissiveColor * _currentEmissiveIntensity);
                    DynamicGI.SetEmissive(_renderer, _emissiveColor * _currentEmissiveIntensity);
                }).SetEase(_easeType);
            }
        }

        public void GoTo(float value)
        {
            if (_tweenLight)
            {
                float targetIntensity = Mathf.Lerp(_minIntensity, _maxIntensity, value);
                _currentLightTween?.Kill();
                _currentLightTween = _light.DOIntensity(targetIntensity, _tweenDuration).SetEase(_easeType);
            }

            if (_tweenEmission)
            {
                float targetEmmissiveIntensity = Mathf.Lerp(_minEmissiveIntensity, _maxEmissiveIntensity, value);
                _currentEmissiveTween?.Kill();
                _currentEmissiveTween = DOVirtual.Float(_currentEmissiveIntensity, targetEmmissiveIntensity, _tweenDuration, v =>
                {
                    _currentEmissiveIntensity = v;
                    _emissiveMaterial.SetColor("_EmissionColor", _emissiveColor * _currentEmissiveIntensity);
                    DynamicGI.SetEmissive(_renderer, _emissiveColor * _currentEmissiveIntensity);
                }).SetEase(_easeType);
            }
        }
    }
}
