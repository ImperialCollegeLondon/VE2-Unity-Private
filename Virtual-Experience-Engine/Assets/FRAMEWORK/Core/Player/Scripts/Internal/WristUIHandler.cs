using DG.Tweening;
using DG.Tweening.Core;
using UnityEngine;
using VE2.Core.UI.API;

namespace VE2.Core.Player.Internal
{
    internal class WristUIHandler //TODO: Think about how the two hands communicate together... only show UI on one hand at a time - look into drag move pattern
    {
        private float _wristLookPrecision => 1; //TODO - config for this
        private bool _showingUI = false;
        private float _showAngle = 35f;
        private float _waitForCloseStartTime = -1;
        private Tween _currentTween;

        private readonly ISecondaryUIServiceInternal _secondaryUIService;
        private readonly Canvas _wristUICanvas;
        private readonly Transform _playerViewTransform;
        private readonly GameObject _indicator; //TODO - some visual assist tool to indicate the look angle
        private readonly Vector3 _initialCanvasLocalScale;

        internal WristUIHandler(ISecondaryUIServiceInternal secondaryUIService, Canvas wristUICanvas, Transform playerHeadTransform, GameObject indicator, bool needsToFlip)
        {
            _secondaryUIService = secondaryUIService;

            _wristUICanvas = wristUICanvas;
            _initialCanvasLocalScale = wristUICanvas.transform.localScale;

            _indicator = indicator;
            if (needsToFlip) //The whole left hand gets flipped to make the right, in that case, we need to unflip a few bits...
            {
                _indicator.transform.localScale = new Vector3(-indicator.transform.localScale.x, indicator.transform.localScale.y, indicator.transform.localScale.z);
                _wristUICanvas.transform.Rotate(0, 180, 0);
            }

            _playerViewTransform = playerHeadTransform;
        }

        internal void HandleUpdate()
        {
            if (_secondaryUIService == null)
                return;

            //Open canvas if we're looking directly at the indicator
            if (Vector3.Angle(_indicator.transform.up, _playerViewTransform.position - _indicator.transform.position) < (_showAngle * _wristLookPrecision) &&
                Vector3.Angle(_playerViewTransform.forward, _indicator.transform.position - _playerViewTransform.position) < (22.5f * _wristLookPrecision) /*&&
                Vector3.Angle(indicator.transform.right, _playerCamera.transform.right) < (27.5f * GetWristLookPrecision())*/)
            {
                _waitForCloseStartTime = -1;
                if (!_showingUI)
                    OpenCanvas();
            }
            //If canvas is open and we've looked a little further away, start a timer to close canvas
            else if (_showingUI && 
                Vector3.Angle(_indicator.transform.up, _playerViewTransform.position - _indicator.transform.position) < (45 * Mathf.Sqrt(_wristLookPrecision)) &&
                Vector3.Angle(_playerViewTransform.forward, _indicator.transform.position - _playerViewTransform.position) < (80f * Mathf.Sqrt(_wristLookPrecision)) /* &&
                Vector3.Angle(indicator.transform.right, _playerCamera.transform.right) < (27.5f * Mathf.Sqrt(GetWristLookPrecision())) */)
            {
                if (_waitForCloseStartTime == -1)
                    _waitForCloseStartTime = Time.time;
                else if (Time.time - _waitForCloseStartTime > 3)
                    CloseCanvas();

            }
            //If we're not looking anywhere near and canvas is still open, close it 
            else if (_showingUI)
            {
                CloseCanvas();
            }
        }

        private void CloseCanvas()
        {
            //_indicator.SetActive(true);
            _showingUI = false;

            if (_currentTween != null && _currentTween.active)
                _currentTween.Kill();

            _currentTween = _wristUICanvas.transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InExpo).SetDelay(0.5f);


            _waitForCloseStartTime = -1;
        }

        private void OpenCanvas()
        {
            _secondaryUIService.MoveSecondaryUIToHolderRect(_wristUICanvas.GetComponent<RectTransform>());

            //StaticData.Utils.openSecondaryUIVR?.Invoke();

            //_indicator.SetActive(false);
            _showingUI = true;

            if (_currentTween != null && _currentTween.active)
                _currentTween.Kill();

            _currentTween = _wristUICanvas.transform.DOScale(_initialCanvasLocalScale, 0.3f).SetEase(Ease.OutExpo);
        }
    }
}
