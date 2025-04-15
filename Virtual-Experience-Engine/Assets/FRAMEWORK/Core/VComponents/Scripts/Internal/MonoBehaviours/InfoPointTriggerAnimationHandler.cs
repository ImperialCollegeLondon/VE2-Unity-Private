using UnityEngine;
using UnityEngine.VFX;
using DG.Tweening;

namespace VE2.Core.VComponents.Internal
{
    [AddComponentMenu("")] //Unlikely to be useful outside the infopoint context, so hide it from the menu
    internal class InfoPointTriggerAnimationHandler : MonoBehaviour
    {
        [Header("Global Highlight Settings")]
        [SerializeField] private float _highlightTweenDuration = 0.75f;
        [SerializeField] private Ease _highlightEase = Ease.OutQuad;


        [Header("Particle Highlight Settings")]
        [SerializeField] private VisualEffect _vfxComponent;

        [SerializeField] private float _particleDefaultAttraction = 0f;
        [SerializeField] private float _particleHighlightAttraction = 0.2f;

        [SerializeField] private Color _particleDefaultColor = Color.gray;
        [SerializeField] private Color _particleHighlightColor = new Color(1f, 0.5f, 0f); // Orange

        [SerializeField] private float _particleDefaultEmission = 2f;
        [SerializeField] private float _particleHighlightEmission = 6f;


        [Header("Icon Renderer Highlight Settings")]
        [SerializeField] private MeshRenderer _iconRenderer;

        [SerializeField] private Color _iconColorMin = Color.gray;
        [SerializeField] private Color _iconColorMax = new Color(1f, 0.5f, 0f); // Orage

        [SerializeField] private float _iconEmissionMin = 1;
        [SerializeField] private float _iconEmissionMax = 4;


        [Header("Icon Scale Highlight Settings")]
        [SerializeField] private Transform _iconTransform;
        [SerializeField] private float _nonHighlightedScaleMult = 0.9f;


        [Header("Point light Highlight Settings")]
        [SerializeField] private Light _light;
        [SerializeField] private float _lightDefaultIntensity = 0f;
        [SerializeField] private float _lightMaxIntensity = 6f;


        [Header("Open/Close Trigger Settings")]
        [SerializeField] private float _openCloseTweenTime = 0.4f;
        [SerializeField] private Ease _openCloseTweenEase = Ease.InOutSine;
        private Tween _openCloseTween;
        private Vector3 _originalTriggerScale;

        private Material _iconMaterialInstance;

        private Tween _highlightTween;
        private Vector3 _originalScale;

        private static readonly string AttractionProp = "AttractionToCenter";
        private static readonly string ColorProp = "Color";

        private void Awake()
        {
            if (_iconRenderer != null)
                _iconMaterialInstance = _iconRenderer.material;

            _originalScale = _iconTransform.localScale;
            _iconTransform.localScale = _originalScale * _nonHighlightedScaleMult;

            _originalTriggerScale = gameObject.transform.localScale;
            _light.intensity = _lightDefaultIntensity;
        }

        public void ToggleHighlight(bool enabled)
        {
            // Kill any active tweens======================
            _highlightTween?.Kill();

            // Read current states========================
            float startAttraction = _vfxComponent.GetFloat(AttractionProp);
            float targetAttraction = enabled ? _particleHighlightAttraction : _particleDefaultAttraction;

            Vector4 currentVfxColorVec = _vfxComponent.GetVector4(ColorProp);
            Color currentHdrColor = new Color(currentVfxColorVec.x, currentVfxColorVec.y, currentVfxColorVec.z, currentVfxColorVec.w);
            Color currentBaseColor = ExtractBaseColor(currentHdrColor);
            float currentEmission = currentHdrColor.maxColorComponent / Mathf.Max(currentBaseColor.maxColorComponent, 0.0001f);

            Color targetBaseColor = enabled ? _particleHighlightColor : _particleDefaultColor;
            float targetEmission = enabled ? _particleHighlightEmission : _particleDefaultEmission;

            float currentLightIntensity = _light.intensity;
            float targetLightIntensity = enabled ? _lightMaxIntensity : _lightDefaultIntensity;

            Color currentIconColor = _iconMaterialInstance != null ? _iconMaterialInstance.color : Color.black;
            Color currentIconBase = ExtractBaseColor(currentIconColor);
            float currentIconEmission = currentIconColor.maxColorComponent / Mathf.Max(currentIconBase.maxColorComponent, 0.0001f);

            Color targetIconBase = enabled ? _iconColorMax : _iconColorMin;
            float targetIconEmission = enabled ? _iconEmissionMax : _iconEmissionMin;

            Vector3 currentScale = _iconTransform.localScale;
            Vector3 targetScale = enabled ? _originalScale : _originalScale * _nonHighlightedScaleMult;

            //Tween values===============================
            _highlightTween = DOTween.To(() => 0f, t =>
            {
                // VFX Attraction
                float attraction = Mathf.Lerp(startAttraction, targetAttraction, t);
                _vfxComponent.SetFloat(AttractionProp, attraction);

                // VFX Color
                Color lerpedBaseVFX = Color.Lerp(currentBaseColor, targetBaseColor, t);
                float lerpedEmissionVFX = Mathf.Lerp(currentEmission, targetEmission, t);
                Color finalHdrVFX = lerpedBaseVFX * lerpedEmissionVFX;
                _vfxComponent.SetVector4(ColorProp, new Vector4(finalHdrVFX.r, finalHdrVFX.g, finalHdrVFX.b, lerpedBaseVFX.a));

                // Light intensity
                _light.intensity = Mathf.Lerp(currentLightIntensity, targetLightIntensity, t);

                // Icon color
                if (_iconMaterialInstance != null)
                {
                    Color lerpedBaseIcon = Color.Lerp(currentIconBase, targetIconBase, t);
                    float lerpedEmissionIcon = Mathf.Lerp(currentIconEmission, targetIconEmission, t);
                    _iconMaterialInstance.color = lerpedBaseIcon * lerpedEmissionIcon;
                }

                // Icon scale
                if (_iconTransform != null)
                {
                    _iconTransform.localScale = Vector3.Lerp(currentScale, targetScale, t);
                }

            }, 1f, _highlightTweenDuration).SetEase(_highlightEase);
        }


        public void ToggleShowTrigger(bool showTrigger) => ToggleShowTrigger(showTrigger, false);

        public void ToggleShowTrigger(bool showTrigger, bool instant = false)
        {
            Vector3 triggerStartScale = gameObject.transform.localScale;
            Vector3 triggerTargetScale = showTrigger ? _originalTriggerScale : Vector3.zero;

            float tweenTimeToUse = instant ? 0f : _openCloseTweenTime;

            _openCloseTween?.Kill();
            _openCloseTween = DOTween.To(() => 0f, t =>
            {
                gameObject.transform.localScale = Vector3.Lerp(triggerStartScale, triggerTargetScale, t);
            }, 1f, tweenTimeToUse).SetEase(_openCloseTweenEase);
        }

        private Color ExtractBaseColor(Color hdrColor)
        {
            float max = hdrColor.maxColorComponent;
            return max > 0f ? hdrColor / max : hdrColor;
        }
    }
}
