using UnityEngine;
using DG.Tweening;

internal class ParticleEffectTweener : MonoBehaviour
{
    private ParticleSystem _particleSystem;
    private Material _materialInstance;
    private float _initialRadialVelocity;
    private float _initialEmissionRate;
    private Color _initialColor;

    private Tween _radialTween;
    private Tween _colorTween;
    private Tween _emissionTween;

    [SerializeField] private Color _highlightColor = new Color(1f, 0.5f, 0f); // Orange
    [SerializeField] private float _highlightRadial = -0.1f;
    [SerializeField] private float _emissionBoostMultiplier = 1.5f;
    [SerializeField] private float _tweenDuration = 0.75f;

    private void Awake()
    {
        _particleSystem = GetComponent<ParticleSystem>();
        var renderer = _particleSystem.GetComponent<ParticleSystemRenderer>();
        _materialInstance = renderer.material; // Creates a unique instance per renderer
        _initialColor = _materialInstance.color;

        _initialRadialVelocity = _particleSystem.velocityOverLifetime.radial.constant;

        var emission = _particleSystem.emission;
        _initialEmissionRate = emission.rateOverTime.constant; // Note: assumes single constant mode
    }

    public void ToggleHighlight(bool enabled)
    {
        _radialTween?.Kill();
        _colorTween?.Kill();
        _emissionTween?.Kill();

        // --- Radial velocity
        var velocity = _particleSystem.velocityOverLifetime;
        float startRadial = velocity.radial.constant;
        float targetRadial = enabled ? _highlightRadial : _initialRadialVelocity;

        _radialTween = DOTween.To(() => startRadial, val =>
        {
            var v = _particleSystem.velocityOverLifetime;
            var curve = v.radial;
            curve.constant = val;
            v.radial = curve;
        }, targetRadial, _tweenDuration).SetEase(Ease.OutQuad);

        // --- Material color (actual material color, not start color)
        Color startColor = _materialInstance.color;
        Color targetColor = enabled ? _highlightColor : _initialColor;

        _colorTween = DOTween.To(() => startColor, val =>
        {
            _materialInstance.color = val;
        }, targetColor, _tweenDuration).SetEase(Ease.OutQuad);

        // --- Emission rate (tweening the constant from a MinMaxCurve)
        var emission = _particleSystem.emission;
        float startRate = emission.rateOverTime.constant;
        float targetRate = enabled ? _initialEmissionRate * _emissionBoostMultiplier : _initialEmissionRate;

        _emissionTween = DOTween.To(() => startRate, val =>
        {
            var e = _particleSystem.emission;
            var curve = e.rateOverTime;
            curve.mode = ParticleSystemCurveMode.Constant;
            curve.constant = val;
            e.rateOverTime = curve;
        }, targetRate, _tweenDuration).SetEase(Ease.OutQuad);
    }
}
