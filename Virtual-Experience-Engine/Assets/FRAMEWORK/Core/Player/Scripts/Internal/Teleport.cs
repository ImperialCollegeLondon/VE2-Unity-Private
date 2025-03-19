using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.Core.Player.API;

namespace VE2.Core.Player.Internal
{
    internal class Teleport
    {
        private readonly TeleportInputContainer _inputContainer;
        private readonly Transform _rootTransform; // For rotating the player

        private readonly Transform _headTransform; // For setting the player's position in free fly mode

        private readonly Transform _thisHandTeleportRaycastOrigin; // Position of the teleport raycast origin
        private readonly Transform _otherHandTeleportRaycastOrigin; // Position of the other hand teleport raycast origin
        private readonly FreeGrabbableWrapper _thisHandGrabbableWrapper;
        private readonly FreeGrabbableWrapper _otherHandGrabbableWrapper;

        private readonly bool _enableFreeFlyMode;

        private readonly HoveringOverScrollableIndicator _hoveringOverScrollableIndicator;

        private bool _teleporterActive = false;
        private GameObject _reticle;
        private LineRenderer _lineRenderer;
        private GameObject _lineRendererObject;
        private GameObject _arrowObject;
        private Vector3 _hitPoint;

        private Quaternion _teleportTargetRotation;
        private Quaternion _teleportRotation;

        private float _initialSpeed = 10f; // Initial speed of the projectile motion
        private float _simulationTimeStep = 0.05f;

        private LayerMask _teleportLayerMask => LayerMask.GetMask("Ground");
        private Vector2 _currentTeleportDirection;
        private int _lineSegmentCount = 20; // Number of segments in the Bezier curve
        private float _maxSlopeAngle = 45f; // Maximum slope angle in degrees
        private bool _isTeleportSuccessful = false;
        private Color _teleportColor = new Color(0.0f, 0.9764706f, 0.7921569f, 0.3176471f);  //TODO - wire into TP config

        public Teleport(TeleportInputContainer inputContainer, Transform rootTransform, Transform thisHandTeleportRaycastOrigin, 
            Transform otherHandTeleportRaycastOrigin, FreeGrabbableWrapper thisHandGrabbableWrapper, FreeGrabbableWrapper otherHandGrabbableWrapper,
            HoveringOverScrollableIndicator hoveringOverScrollableIndicator, bool enableFreeFlyMode)
        {
            _inputContainer = inputContainer;
            _rootTransform = rootTransform;

            _thisHandTeleportRaycastOrigin = thisHandTeleportRaycastOrigin;
            _otherHandTeleportRaycastOrigin = otherHandTeleportRaycastOrigin;
            _thisHandGrabbableWrapper = thisHandGrabbableWrapper;
            _otherHandGrabbableWrapper = otherHandGrabbableWrapper;

            _enableFreeFlyMode = enableFreeFlyMode;

            _hoveringOverScrollableIndicator = hoveringOverScrollableIndicator;

        }

        public void HandleUpdate()
        {
            if (_teleporterActive) 
                CastTeleportRay();
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

        private void HandleTeleportActivated() 
        { 
            //only handle TP if that we were clear of interactables at the time of activation
            //Fixes case of "point at a scrollable, hold up, point away, release stick", which causees an unexpected TP
            if (!_hoveringOverScrollableIndicator.IsHoveringOverScrollableObject && _thisHandGrabbableWrapper.RangedFreeGrabInteraction == null)
                _teleporterActive = true;
        }

        private void HandleTeleportDeactivated()
        {
            bool wasTeleporterActive = _teleporterActive;
            _teleporterActive = false;

            if (!wasTeleporterActive)
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

            _otherHandGrabbableWrapper.RangedFreeGrabInteraction?.ApplyDeltaWhenGrabbed(deltaPosition, deltaRotation); //Handle the teleportation for the ranged grab interaction module

            CancelTeleport();
        }

        private void CastTeleportRay()
        {
            if (_thisHandGrabbableWrapper.RangedFreeGrabInteraction != null)
                return;

            Vector3 startPosition = _thisHandTeleportRaycastOrigin.position;
            Vector3 direction = _thisHandTeleportRaycastOrigin.forward;

            _thisHandTeleportRaycastOrigin.gameObject.SetActive(false);

            if (_enableFreeFlyMode)
            {
                // Free-fly mode remains unchanged.
                float stickX = _inputContainer.TeleportDirection.Value.x;
                float distance = Mathf.Lerp(1f, 3f, stickX); // TODO: Make this configurable
                Vector3 teleportDestination = startPosition + direction * distance;
                if (CanTeleport(startPosition, teleportDestination))
                {
                    _reticle.transform.position = teleportDestination;

                    Vector3 delta = teleportDestination - _headTransform.position;
                    Vector3 finalPosition = _rootTransform.position + delta;
                    if (Physics.Raycast(teleportDestination, Vector3.down, out RaycastHit hit, _teleportLayerMask))
                    {
                        finalPosition.y = Mathf.Max(finalPosition.y, hit.point.y);
                    }

                    _hitPoint = finalPosition;
                    Vector3 arrowDirection = _thisHandTeleportRaycastOrigin.forward;
                    arrowDirection.y = 0;
                    if (arrowDirection != Vector3.zero)
                    {
                        arrowDirection.Normalize();
                    }
                    _arrowObject.transform.rotation = Quaternion.LookRotation(arrowDirection, Vector3.up);
                    _teleportRotation = _arrowObject.transform.rotation;

                    DrawStraightLine(startPosition, teleportDestination);

                    _lineRenderer.material.color = Color.green;
                    _reticle.SetActive(true);
                    _arrowObject.SetActive(true);
                    _isTeleportSuccessful = true;
                }
                else
                {
                    DrawStraightLine(startPosition, teleportDestination);
                    _lineRenderer.material.color = Color.red;
                    _reticle.SetActive(false);
                    _arrowObject.SetActive(false);
                    _isTeleportSuccessful = false;
                }
            }
            else
            {
                // Adjust initialSpeed to control how far/fast the arc travels.
                _initialSpeed = 10f; // Configurable: tweak to change the arc length and curvature.
                Vector3 initialVelocity = direction.normalized * _initialSpeed;

                // We simulate projectile motion: position = start + v0 * t + 0.5 * g * t^2.
                _simulationTimeStep = Mathf.Clamp(_simulationTimeStep, 0.001f, 0.1f); // Ensure it doesn't go above 0.1 seconds.
                float simulationMaxTime = 5.0f; // Adjust maximum simulation time if needed.
                List<Vector3> trajectoryPoints = new List<Vector3>();
                trajectoryPoints.Add(startPosition);

                bool hitFound = false;
                RaycastHit hit = new RaycastHit();
                float simulationTime = _simulationTimeStep;

                while (simulationTime < simulationMaxTime)
                {
                    Vector3 previousPoint = trajectoryPoints[trajectoryPoints.Count - 1];
                    Vector3 currentPoint = startPosition + initialVelocity * simulationTime + 0.5f * Physics.gravity * simulationTime * simulationTime;
                    trajectoryPoints.Add(currentPoint);

                    Vector3 segmentDir = (currentPoint - previousPoint).normalized;
                    float segmentDistance = Vector3.Distance(currentPoint, previousPoint);

                    // First check: is there any object not on the Traversible layer blocking this segment?
                    RaycastHit blockingHit;
                    if (Physics.Raycast(previousPoint, segmentDir, out blockingHit, segmentDistance, ~_teleportLayerMask))
                    {
                        // A non-traversible object is in the way  cancel teleport.
                        hitFound = false;
                        break;
                    }

                    // Second check: does the segment hit a valid teleport surface (i.e. on the Traversible layer)?
                    if (Physics.Raycast(previousPoint, segmentDir, out hit, segmentDistance, _teleportLayerMask))
                    {
                        // If a valid hit is detected, update the trajectory point and mark teleport as possible.
                        trajectoryPoints[trajectoryPoints.Count - 1] = hit.point;
                        hitFound = true;
                        break;
                    }
                    simulationTime += _simulationTimeStep;
                }

                // --- Update Visuals and Teleport Target ---
                if (hitFound && IsValidSurface(hit.normal))
                {
                    _hitPoint = hit.point;
                    _reticle.transform.position = _hitPoint;

                    UpdateTargetRotation(hit.normal);

                    // Update the line renderer with the simulated trajectory points.
                    _lineRenderer.positionCount = trajectoryPoints.Count;
                    for (int i = 0; i < trajectoryPoints.Count; i++)
                    {
                        _lineRenderer.SetPosition(i, trajectoryPoints[i]);
                    }
                    _lineRenderer.material.color = _teleportColor;
                    _reticle.SetActive(true);
                    _arrowObject.SetActive(true);
                    _isTeleportSuccessful = true;
                }
                else
                {
                    // If no valid hit, render the full arc in red.
                    _lineRenderer.positionCount = trajectoryPoints.Count;
                    for (int i = 0; i < trajectoryPoints.Count; i++)
                    {
                        _lineRenderer.SetPosition(i, trajectoryPoints[i]);
                    }
                    _lineRenderer.material.color = Color.red;
                    _reticle.SetActive(false);
                    _arrowObject.SetActive(false);
                    _isTeleportSuccessful = false;
                }
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
            // Set arrow direction based on the raycast origin's horizontal forward.
            Vector3 arrowDirection = _thisHandTeleportRaycastOrigin.forward;
            arrowDirection.y = 0;
            if (arrowDirection != Vector3.zero)
                arrowDirection.Normalize();
            _arrowObject.transform.rotation = Quaternion.LookRotation(arrowDirection, Vector3.up);

            // Get input direction and compute an additional rotation from it.
            _currentTeleportDirection = _inputContainer.TeleportDirection.Value;
            float rotationAngle = Mathf.Atan2(_currentTeleportDirection.x, _currentTeleportDirection.y) * Mathf.Rad2Deg;
            _teleportTargetRotation = Quaternion.Euler(0, rotationAngle, 0);
            _teleportRotation = _arrowObject.transform.rotation * _teleportTargetRotation;

            // Use the input rotation to determine a desired forward direction.
            Vector3 desiredForward = _teleportTargetRotation * arrowDirection;
            // Project that forward vector onto the plane defined by the surface normal.
            desiredForward = Vector3.ProjectOnPlane(desiredForward, surfaceNormal).normalized;
            if (desiredForward == Vector3.zero)
            {
                // Fallback: if projection fails, use the original forward vector.
                desiredForward = arrowDirection;
            }
            _arrowObject.transform.rotation = Quaternion.LookRotation(desiredForward, surfaceNormal);
        }

        private void DrawStraightLine(Vector3 startPosition, Vector3 endPosition)
        {
            _lineRenderer.positionCount = 2;
            _lineRenderer.SetPosition(0, startPosition);
            _lineRenderer.SetPosition(1, endPosition);
        }

        private bool CanTeleport(Vector3 startPosition, Vector3 teleportDestination)
        {
            Vector3 direction = teleportDestination - startPosition;
            float distance = direction.magnitude;
            direction.Normalize();

            // Perform a raycast without any layer mask filtering, are there obstacles we would want to leave out? Hmmm...
            if (Physics.Raycast(startPosition, direction, distance))
            {
                return false; // There is an obstacle in the way
            }

            return true; // No obstacles, can teleport
        }
    }
    
    internal class HoveringOverScrollableIndicator
    {
        public bool IsHoveringOverScrollableObject = false;
    }
}
