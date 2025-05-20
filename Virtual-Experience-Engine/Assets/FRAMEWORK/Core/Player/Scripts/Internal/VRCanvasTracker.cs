using UnityEngine;
using VE2.Common.API;
using VE2.Core.UI.API;

namespace VE2.Core.Player.Internal
{
    [AddComponentMenu("")] // Prevents this MonoBehaviour from showing in the Add Component menu
    internal class VRCanvasTracker : MonoBehaviour
    {
        [SerializeField] private Transform _cameraTransform; 
        [SerializeField] private Collider _canvasCollider;

        [SerializeField] private float _distanceFromCamera = 1.5f;
        [SerializeField] private float _distanceTolerance = 0.2f; // Tolerance in meters.

        [SerializeField] private float _verticalOffset = -0.1f;

        private bool _needsToRotate = false;
        [SerializeField] private float _angleToStartMoving = 30f; // Tolerance in degrees.

        private float _timeEnteredStopMovingThreshold = -1;
        [SerializeField] private float _timeToBeWithinStopMovingAngleToStopMoving = 0.5f;
        [SerializeField] private float _angleToStopMoving = 5f; // Tolerance in degrees.

        
        [SerializeField] private float _moveSpeed = 5;

        private Vector3 targetLocalPosition;
        private Quaternion targetLocalRotation;
        private IPrimaryUIService _primaryUIService;

        private void Awake()
        {
            _primaryUIService = VE2API.PrimaryUIService;
            if (_primaryUIService != null)
            {
                _primaryUIService.OnUIShow += HandlePrimaryUIShown;
                _primaryUIService.OnUIHide += HandlePrimaryUIHidden;
            }

            _canvasCollider.enabled = false;
        }

        void Update()
        {
            Vector3 cameraForward = _cameraTransform.parent.InverseTransformDirection(new Vector3(_cameraTransform.forward.x, 0, _cameraTransform.forward.z).normalized);

            //If the canvas is outside the large angle, start rotating towards the camera forward 
            //Once we're rotating, the canvas must be within a smaller angle, to the camera forward (for a certain duration) to stop rotating 
            //This means the canvas will still track changes in camera direction once inside the larger threshold 
            if (!_needsToRotate)
            {
                if (!IsCanvasWithinBounds(cameraForward, _angleToStartMoving))
                    _needsToRotate = true;
            }
            else 
            {
                if (IsCanvasWithinBounds(cameraForward, _angleToStopMoving))
                {
                    if (_timeEnteredStopMovingThreshold == -1)
                    {
                        _timeEnteredStopMovingThreshold = Time.time;
                    }
                    else if (Time.time - _timeEnteredStopMovingThreshold > _timeToBeWithinStopMovingAngleToStopMoving)
                    {
                        _timeEnteredStopMovingThreshold = -1;
                        _needsToRotate = false;
                    }
                }
                else 
                {
                    _timeEnteredStopMovingThreshold = -1;
                }
            }

            if (_needsToRotate)
                UpdateTargetTransform(cameraForward);

            SmoothMoveToTarget();
        }

        private bool IsCanvasWithinBounds(Vector3 cameraForward, float angle)
        {
            Vector3 canvasForward = transform.localRotation * Vector3.forward;
            canvasForward.y = 0; // Ensure it's XZ plane

            float angleDot = Vector3.Dot(cameraForward, canvasForward.normalized); // More efficient than Angle()
            bool angleExceeded = angleDot < Mathf.Cos(angle * Mathf.Deg2Rad); // Compare against cosine of tolerance

            float localDistance = Vector3.Distance(transform.localPosition, _cameraTransform.localPosition); 
            bool distanceExceeded = Mathf.Abs(localDistance - _distanceFromCamera) > _distanceTolerance;

            return !angleExceeded && !distanceExceeded;
        }

        private void UpdateTargetTransform(Vector3 cameraForward)
        {
            targetLocalPosition = (cameraForward * _distanceFromCamera + _cameraTransform.localPosition) + (Vector3.up * _verticalOffset);
            targetLocalRotation = Quaternion.LookRotation(cameraForward, Vector3.up);
        }

        private void SmoothMoveToTarget()
        {
            transform.SetLocalPositionAndRotation(
                Vector3.Lerp(transform.localPosition, targetLocalPosition, Time.deltaTime * _moveSpeed), 
                Quaternion.Slerp(transform.localRotation, targetLocalRotation, Time.deltaTime * _moveSpeed));
        }

        private void HandlePrimaryUIShown() 
        {
            _canvasCollider.enabled = true;
        }

        private void HandlePrimaryUIHidden() 
        {
            _canvasCollider.enabled = false;
        }
    }
}
