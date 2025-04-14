using UnityEngine;
using UnityEngine.VFX;
using DG.Tweening;

internal class ParticleEffectTweener : MonoBehaviour
{
    [Header("VFX Graph Controls")]
    [SerializeField] private VisualEffect _vfxComponent;

    [SerializeField] private float _attractionMin = 0f;
    [SerializeField] private float _attractionMax = 1f;

    [SerializeField] private Color _particleColorMin = Color.gray;
    [SerializeField] private Color _particleColorMax = new Color(1f, 0.5f, 0f); // Orange

    [SerializeField] private float _emissionMin = 0.5f;
    [SerializeField] private float _emissionMax = 2f;

    [Header("Icon Renderer Controls")]
    [SerializeField] private MeshRenderer _iconRenderer;

    [SerializeField] private Color _iconColorMin = Color.gray;
    [SerializeField] private Color _iconColorMax = new Color(1f, 0.5f, 0f);

    [SerializeField] private float _iconEmissionMin = 0.5f;
    [SerializeField] private float _iconEmissionMax = 2f;

    [Header("Color Tween Settings")]
    [SerializeField] private float _tweenDuration = 0.75f;
    [SerializeField] private Ease _ease = Ease.OutQuad;

    [Header("Icon Scale Tween Settings")]
    [SerializeField] private Transform _iconTransform;
    [SerializeField] private float _nonHighlightedScaleMult = 0.8f;
    [SerializeField] private float _scaleTweenDuration = 1.5f;
    [SerializeField] private Ease _scaleEase = Ease.OutBack;

    private Material _iconMaterialInstance;

    private Tween _attractionTween;
    private Tween _vfxColorTween;
    private Tween _iconColorTween;
    private Tween _scaleTween;
    private Vector3 _originalScale;

    private static readonly string AttractionProp = "AttractionToCenter";
    private static readonly string ColorProp = "Color";

    private void Awake()
    {
        if (_iconRenderer != null)
            _iconMaterialInstance = _iconRenderer.material;

        _originalScale = _iconTransform.localScale;
        _iconTransform.localScale = _originalScale * _nonHighlightedScaleMult;

        ToggleHighlight(false);
    }

    public void ToggleHighlight(bool enabled)
    {
        _attractionTween?.Kill();
        _vfxColorTween?.Kill();
        _iconColorTween?.Kill();
        _scaleTween?.Kill();

        // --- VFX Attraction
        float startAttraction = _vfxComponent.GetFloat(AttractionProp);
        float targetAttraction = enabled ? _attractionMax : _attractionMin;

        _attractionTween = DOTween.To(() => startAttraction, val =>
        {
            _vfxComponent.SetFloat(AttractionProp, val);
        }, targetAttraction, _tweenDuration).SetEase(_ease);

        // --- VFX Color + Emission
        Vector4 currentVfxColorVec = _vfxComponent.GetVector4(ColorProp);
        Color currentHdrColor = new Color(currentVfxColorVec.x, currentVfxColorVec.y, currentVfxColorVec.z, currentVfxColorVec.w);
        float currentEmission = currentHdrColor.maxColorComponent / Mathf.Max(ExtractBaseColor(currentHdrColor).maxColorComponent, 0.0001f);
        Color currentBaseColor = ExtractBaseColor(currentHdrColor);

        Color targetBaseColor = enabled ? _particleColorMax : _particleColorMin;
        float targetEmission = enabled ? _emissionMax : _emissionMin;

        _vfxColorTween = DOTween.To(() => 0f, t =>
        {
            Color lerpedBase = Color.Lerp(currentBaseColor, targetBaseColor, t);
            float lerpedEmission = Mathf.Lerp(currentEmission, targetEmission, t);

            Color finalHdr = lerpedBase * lerpedEmission;
            _vfxComponent.SetVector4(ColorProp, new Vector4(finalHdr.r, finalHdr.g, finalHdr.b, lerpedBase.a)); // Preserve alpha from base
        }, 1f, _tweenDuration).SetEase(_ease);

        // --- Icon Color + Emission
        if (_iconMaterialInstance != null)
        {
            Color currentIconColor = _iconMaterialInstance.color;
            float currentIconEmission = currentIconColor.maxColorComponent / Mathf.Max(ExtractBaseColor(currentIconColor).maxColorComponent, 0.0001f);
            Color currentIconBase = ExtractBaseColor(currentIconColor);

            Color targetIconBase = enabled ? _iconColorMax : _iconColorMin;
            float targetIconEmission = enabled ? _iconEmissionMax : _iconEmissionMin;

            _iconColorTween = DOTween.To(() => 0f, t =>
            {
                Color lerpedBase = Color.Lerp(currentIconBase, targetIconBase, t);
                float lerpedEmission = Mathf.Lerp(currentIconEmission, targetIconEmission, t);

                _iconMaterialInstance.color = lerpedBase * lerpedEmission;
            }, 1f, _tweenDuration).SetEase(_ease);
        }

        // --- Icon Scale
        if (_iconTransform != null)
        {
            Vector3 targetScale = enabled
                ? _originalScale
                : _originalScale * _nonHighlightedScaleMult;

            _scaleTween = _iconTransform.DOScale(targetScale, _scaleTweenDuration).SetEase(_scaleEase);
        }
    }

    private Color ExtractBaseColor(Color hdrColor)
    {
        float max = hdrColor.maxColorComponent;
        return max > 0f ? hdrColor / max : hdrColor;
    }
}
