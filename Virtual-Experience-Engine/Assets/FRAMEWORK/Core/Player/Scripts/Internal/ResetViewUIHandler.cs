using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using VE2.Common.Shared;

namespace VE2.Core.Player.Internal
{
    [AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
    internal class ResetViewUIHandler : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private V_RadialProgressBar _radialProgressBar;
        [SerializeField] private Image _centerImage;

        private static readonly Color _defaultColor = new Color(1f, 1f, 1f, 1f);    
        private static readonly Color _highlightColor = new Color(1f, 0.5f, 0f, 1f);

        private Tween _alphaTween;
        private static readonly float _alphaTweenTime = 0.2f;
        private static readonly Ease _highlightEase = Ease.Linear;

        private void Awake()
        {
            StopShowing();
        }

        internal void StartShowing()
        {
            _radialProgressBar.ChangeValue(0);
            _centerImage.color = _defaultColor;
            _radialProgressBar._loadingBar.color = _defaultColor;

            gameObject.SetActive(true);
            _canvasGroup.alpha = 1f;

            _alphaTween?.Kill();
            _alphaTween = DOTween.To(() => 0f, t =>
            {
                _canvasGroup.alpha = t;
            }, 1f, _alphaTweenTime).SetEase(_highlightEase);
        }

        internal void SetProgress(float progress)
        {
            _radialProgressBar.ChangeValue(progress * 100f);
        }

        internal void SetResetViewPrimed()
        {
            //Set to orange
            _radialProgressBar.ChangeValue(100f);
            _centerImage.color = _highlightColor;
            _radialProgressBar._loadingBar.color = _highlightColor;
        }

        internal void StopShowing()
        {
            gameObject.SetActive(false);
            _alphaTween?.Kill();
            _canvasGroup.alpha = 0f;
        }
    }
}
