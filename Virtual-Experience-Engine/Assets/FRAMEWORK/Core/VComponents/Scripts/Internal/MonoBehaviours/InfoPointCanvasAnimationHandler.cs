using UnityEngine;
using UnityEngine.VFX;
using DG.Tweening;

namespace VE2.Core.VComponents.Internal
{
    [AddComponentMenu("")] //Unlikely to be useful outside the infopoint context, so hide it from the menu
    internal class InfoPointCanvasAnimationHandler : MonoBehaviour
    {
        [Header("Open/Close Tween Settings")]
        [SerializeField] private float _tweenTime = 0.4f;
        [SerializeField] private Ease _tweenEase = Ease.InOutSine;

        private Tween _animationTween;
        private Vector3 _originalCanvasScale;
        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _originalCanvasScale = gameObject.transform.localScale;

            gameObject.transform.localScale = Vector3.zero;
            _canvasGroup = gameObject.GetComponent<CanvasGroup>();
        }

        public void ToggleShowCanvas(bool canvasActive) => ToggleShowCanvas(canvasActive, false);

        public void ToggleShowCanvas(bool canvasActive, bool instant = false)
        {
            Vector3 canvasStartScale = gameObject.transform.localScale;
            Vector3 canvasTargetScale = canvasActive ? _originalCanvasScale : Vector3.zero;

            float canvasStartAlpha = _canvasGroup.alpha;
            float canvasTargetAlpha = canvasActive ? 1f : 0f;

            float tweenTimeToUse = instant ? 0f : _tweenTime;

            _animationTween?.Kill();
            _animationTween = DOTween.To(() => 0f, t =>
            {
                gameObject.transform.localScale = Vector3.Lerp(canvasStartScale, canvasTargetScale, t);
                _canvasGroup.alpha = Mathf.Lerp(canvasStartAlpha, canvasTargetAlpha, t);
            }, 1f, tweenTimeToUse).SetEase(_tweenEase);
        }
    }
}
