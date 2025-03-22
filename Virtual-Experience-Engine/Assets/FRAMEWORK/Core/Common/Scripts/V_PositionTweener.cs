using DG.Tweening;
using UnityEngine;

public class V_PositionTweener : MonoBehaviour
{
    [Title("Values")]
    [BeginGroup, SerializeField] private Vector3 _minPosition;
    [SerializeField] private Quaternion _minRotation;

    [SerializeField] private Vector3 _maxPosition;
    [EditorButton(nameof(CaptureStartValues), "Capture Start Values", Order = 110)]
    [EditorButton(nameof(CaptureEndValues), "Capture End Values", Order = 110)]
    [EndGroup, SerializeField] private Quaternion _maxRotation;

    [SerializeField, SpaceArea(spaceBefore: 5)] private float _duration;
    [SerializeField] private Ease _easeType = Ease.Linear;

    private Tween _currentPosTween;
    private Tween _currentRotTween;

    public void GoToMax()
    {
        _currentPosTween?.Kill();
        _currentPosTween = transform.DOMove(_maxPosition, _duration).SetEase(_easeType);

        _currentRotTween?.Kill();
        _currentRotTween = transform.DORotateQuaternion(_maxRotation, _duration).SetEase(_easeType);
    }

    public void GoToMin()
    {
        _currentPosTween?.Kill();
        _currentPosTween = transform.DOMove(_minPosition, _duration).SetEase(_easeType);

        _currentRotTween?.Kill();
        _currentRotTween = transform.DORotateQuaternion(_minRotation, _duration).SetEase(_easeType);
    }

    private void CaptureStartValues()
    {
        _minPosition = transform.position;
        _minRotation = transform.rotation;
    }

    private void CaptureEndValues()
    {
        _maxPosition = transform.position;
        _maxRotation = transform.rotation;
    }
}
