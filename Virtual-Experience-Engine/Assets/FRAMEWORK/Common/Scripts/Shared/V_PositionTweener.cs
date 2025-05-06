using DG.Tweening;
using UnityEngine;

namespace VE2.Common.Shared
{
    internal class V_PositionTweener : MonoBehaviour
    {
        [Title("Values")]
        [BeginGroup, SerializeField] private Vector3 _minLocalPosition;
        [SerializeField] private Quaternion _minLocalRotation;

        [SerializeField] private Vector3 _maxLocalPosition;
        [EditorButton(nameof(CaptureStartValues), "Capture Start Values", Order = 110)]
        [EditorButton(nameof(CaptureEndValues), "Capture End Values", Order = 110)]
        [EndGroup, SerializeField] private Quaternion _maxLocalRotation;

        [SerializeField, SpaceArea(spaceBefore: 5)] private float _duration;
        [SerializeField] private Ease _easeType = Ease.Linear;

        private Tween _currentPosTween;
        private Tween _currentRotTween;

        public void GoToMax()
        {
            _currentPosTween?.Kill();
            _currentPosTween = transform.DOLocalMove(_maxLocalPosition, _duration).SetEase(_easeType);

            _currentRotTween?.Kill();
            _currentRotTween = transform.DOLocalRotateQuaternion(_maxLocalRotation, _duration).SetEase(_easeType);
        }

        public void GoToMin()
        {
            _currentPosTween?.Kill();
            _currentPosTween = transform.DOLocalMove(_minLocalPosition, _duration).SetEase(_easeType);

            _currentRotTween?.Kill();
            _currentRotTween = transform.DOLocalRotateQuaternion(_minLocalRotation, _duration).SetEase(_easeType);
        }

        private void CaptureStartValues()
        {
            _minLocalPosition = transform.localPosition;
            _minLocalRotation = transform.localRotation;
        }

        private void CaptureEndValues()
        {
            _maxLocalPosition = transform.localPosition;
            _maxLocalRotation = transform.localRotation;
        }
    }
}
