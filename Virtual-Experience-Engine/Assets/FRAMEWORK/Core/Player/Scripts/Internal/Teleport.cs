using System;
using UnityEngine;
using VE2.Core.Player.API;

namespace VE2.Core.Player.Internal
{
    internal class Teleport
    {
        private readonly TeleportInputContainer _inputContainer;
        private readonly Transform _rootTransform; // For rotating the player
        private readonly Transform _thisHandTeleportRaycastOrigin; // Position of the teleport raycast origin
        private readonly Transform _otherHandTeleportRaycastOrigin; // Position of the other hand teleport raycast origin
        private readonly FreeGrabbableWrapper _thisHandGrabbableWrapper;
        private readonly FreeGrabbableWrapper _otherHandGrabbableWrapper;
        private GameObject _reticle;
        private LineRenderer _lineRenderer;
        private GameObject _lineRendererObject;
        private GameObject _arrowObject;
        private Vector3 _hitPoint;
        private float _teleportRayDistance = 50f;
        private Quaternion _teleportTargetRotation;
        private Quaternion _teleportRotation;
        private LayerMask _teleportLayerMask => LayerMask.GetMask("Traversible");
        private Vector2 _currentTeleportDirection;
        private int _lineSegmentCount = 20; // Number of segments in the Bezier curve
        private float _maxSlopeAngle = 45f; // Maximum slope angle in degrees

        public Teleport(TeleportInputContainer inputContainer, Transform rootTransform, Transform thisHandTeleportRaycastOrigin, Transform otherHandTeleportRaycastOrigin, FreeGrabbableWrapper thisHandGrabbableWrapper, FreeGrabbableWrapper otherHandGrabbableWrapper)
        {
            _inputContainer = inputContainer;
            _rootTransform = rootTransform;
            _thisHandTeleportRaycastOrigin = thisHandTeleportRaycastOrigin;
            _otherHandTeleportRaycastOrigin = otherHandTeleportRaycastOrigin;
            _thisHandGrabbableWrapper = thisHandGrabbableWrapper;
            _otherHandGrabbableWrapper = otherHandGrabbableWrapper;
        }

        public void HandleUpdate()
        {
            if (_inputContainer.Teleport.IsPressed)
            {
                CastTeleportRay();
            }
        }

        public void HandleOEnable()
        {
            CreateTeleportRayAndReticle();

            _inputContainer.Teleport.OnPressed += HandleTeleportActivated;
            _inputContainer.Teleport.OnReleased += HandleTeleportDeactivated;
        }

        public void HandleOnDisable()
        {
            _inputContainer.Teleport.OnPressed -= HandleTeleportActivated;
            _inputContainer.Teleport.OnReleased -= HandleTeleportDeactivated;
        }

        private void HandleTeleportActivated() { }

        private void HandleTeleportDeactivated()
        {
            if (_thisHandGrabbableWrapper.RangedFreeGrabInteraction != null)
                return;

            // Teleport User

            Vector3 initialHandPosition = _otherHandTeleportRaycastOrigin.position;
            Quaternion initialHandRotation = _otherHandTeleportRaycastOrigin.rotation;

            _rootTransform.position = _hitPoint;
            _rootTransform.rotation = _teleportRotation;


            Vector3 finallHandPosition = _otherHandTeleportRaycastOrigin.position;
            Quaternion finalHandRotation = _otherHandTeleportRaycastOrigin.rotation;
            //Get raycast origin pos/rot again 

            //Delta between the two 
            Vector3 deltaPosition = finallHandPosition - initialHandPosition;
            Quaternion deltaRotation = finalHandRotation * Quaternion.Inverse(initialHandRotation);

            if (_otherHandGrabbableWrapper.RangedFreeGrabInteraction != null)
            {
                _otherHandGrabbableWrapper.RangedFreeGrabInteraction.ApplyDeltaWhenGrabbed(deltaPosition, deltaRotation); //Handle the teleportation for the ranged grab interaction module
            }

            CancelTeleport();
        }

        private void CastTeleportRay()
        {
            if (_thisHandGrabbableWrapper.RangedFreeGrabInteraction != null)
                return;

            Vector3 startPosition = _thisHandTeleportRaycastOrigin.position;
            Vector3 direction = _thisHandTeleportRaycastOrigin.forward;
            _thisHandTeleportRaycastOrigin.gameObject.SetActive(false);

            if (Physics.Raycast(startPosition, direction, out RaycastHit hit, _teleportRayDistance, _teleportLayerMask))
            {
                if (IsValidSurface(hit.normal))
                {
                    _hitPoint = hit.point;
                    _reticle.transform.position = _hitPoint;
                    Vector3 arrowDirection = _thisHandTeleportRaycastOrigin.forward;
                    arrowDirection.y = 0;
                    if (arrowDirection != Vector3.zero)
                    {
                        arrowDirection.Normalize();
                    }
                    _arrowObject.transform.rotation = Quaternion.LookRotation(arrowDirection, Vector3.up);

                    // Update the line renderer positions
                    DrawBezierCurve(startPosition, _hitPoint);
                    _lineRenderer.material.color = Color.green;

                    _reticle.SetActive(true);
                    _arrowObject.SetActive(true);
                    UpdateTargetRotation(hit.normal);
                }
                else
                {
                    DrawBezierCurve(startPosition, hit.point);
                    // Surface is not valid for teleportation
                    _reticle.SetActive(false);
                    _arrowObject.SetActive(false);
                    _lineRenderer.material.color = Color.red;

                }
            }
            else
            {
                // Update the line renderer positions
                if (Physics.Raycast(startPosition, direction, out RaycastHit otherhit, _teleportRayDistance))
                {
                    DrawBezierCurve(startPosition, otherhit.point);
                }
                else
                {
                    DrawBezierCurve(startPosition, direction * _teleportRayDistance);
                }

                _reticle.SetActive(false);
                _arrowObject.SetActive(false);
                _lineRenderer.material.color = Color.red;
            }
            _lineRendererObject.SetActive(true);
        }

        private bool IsValidSurface(Vector3 normal)
        {
            // Check if the surface normal is within the acceptable slope angle
            float angle = Vector3.Angle(Vector3.up, normal);
            return angle <= _maxSlopeAngle;
        }

        private void CancelTeleport()
        {
            _reticle.SetActive(false);
            _lineRendererObject.SetActive(false);
            _arrowObject.SetActive(false);
            _thisHandTeleportRaycastOrigin.gameObject.SetActive(true);
        }

        private void CreateTeleportRayAndReticle()
        {
            if (_lineRendererObject == null)
            {
                _lineRendererObject = new GameObject("LineRendererObject");
                _lineRenderer = _lineRendererObject.AddComponent<LineRenderer>();
                _lineRenderer.positionCount = _lineSegmentCount + 1;
                _lineRenderer.startWidth = 0.01f;
                _lineRenderer.endWidth = 0.01f;
                _lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
                _lineRenderer.material.color = Color.yellow;
                _lineRendererObject.SetActive(false);
                _lineRendererObject.transform.SetParent(_thisHandTeleportRaycastOrigin.parent);
                _lineRendererObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }
            if (_reticle == null)
            {
                GameObject teleportCursorPrefab = Resources.Load<GameObject>("TeleportationCursor");
                if (teleportCursorPrefab != null)
                {
                    _reticle = GameObject.Instantiate(teleportCursorPrefab);
                    _arrowObject = _reticle.transform.Find("TeleportationCursorChild").gameObject;

                }
                else
                {
                    Debug.LogError("TeleportCursor prefab not found in Resources.");
                }
            }

            _reticle.SetActive(false);
            _arrowObject.SetActive(false);
        }

        private void UpdateTargetRotation(Vector3 surfaceNormal)
        {
            _currentTeleportDirection = _inputContainer.TeleportDirection.Value;
            float rotationAngle = Mathf.Atan2(_currentTeleportDirection.x, _currentTeleportDirection.y) * Mathf.Rad2Deg;
            _teleportTargetRotation = Quaternion.Euler(0, rotationAngle, 0);

            Debug.Log($"Current Teleport Direction: {_currentTeleportDirection}, Teleport Rotation: {rotationAngle}");

            _teleportRotation = _arrowObject.transform.rotation * _teleportTargetRotation;

            // Align the arrow object with the surface normal
            Quaternion surfaceRotation = Quaternion.FromToRotation(Vector3.up, surfaceNormal);
            _arrowObject.transform.rotation = surfaceRotation * _teleportTargetRotation;
        }

        private void DrawBezierCurve(Vector3 startPosition, Vector3 endPosition)
        {
            Vector3 controlPoint = (startPosition + endPosition) / 2 + Vector3.up * 2; // Control point for the Bezier curve

            for (int i = 0; i <= _lineSegmentCount; i++)
            {
                float t = i / (float)_lineSegmentCount;
                Vector3 position = CalculateQuadraticBezierPoint(t, startPosition, controlPoint, endPosition);
                _lineRenderer.SetPosition(i, position);
            }
        }

        private Vector3 CalculateQuadraticBezierPoint(float t, Vector3 startPosition, Vector3 controlPoint, Vector3 endPosition)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;

            Vector3 p = uu * startPosition; // (1-t)^2 * startPosition
            p += 2 * u * t * controlPoint; // 2 * (1-t) * t * controlPoint
            p += tt * endPosition; // t^2 * endPosition

            return p;
        }
    }
}
