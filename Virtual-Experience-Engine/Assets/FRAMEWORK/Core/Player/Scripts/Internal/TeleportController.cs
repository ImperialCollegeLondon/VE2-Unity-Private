using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.Common.Shared;
using VE2.Core.Player.API;

namespace VE2.Core.Player.Internal
{
    internal class TeleportController
    {
        #region Dependencies
        private readonly TeleportInputContainer _inputContainer;

        private LineRenderer _teleportLineRenderer;
        private readonly LineRenderer _interactorLineRenderer; // Position of the teleport raycast origin
        private readonly Material _teleportLineMaterial;
        private ColorConfiguration _colorConfig => ColorConfiguration.Instance;
        private readonly GameObject _teleportCursor;
        private const float LINE_EMISSION_INTENSITY = 15;

        private readonly Transform _rootTransform; // For rotating the player
        private readonly Transform _headTransform; // For setting the player's position in free fly mode TODO: Doesn't seem to be assigned
        private readonly Transform _otherHandTransformReference; // Position of the other hand teleport raycast origin
        private Transform _teleportRayOrigin => _teleportLineRenderer.transform;

        private readonly FreeGrabbableWrapper _thisHandGrabbableWrapper;
        private readonly FreeGrabbableWrapper _otherHandGrabbableWrapper;

        private readonly HoveringOverScrollableIndicator _hoveringOverScrollableIndicator;
        private readonly MovementModeConfig _movementModeConfig;
        #endregion

        #region Runtime data
        private bool _teleporterActive = false;
        private Vector3 _hitPoint;

        private Quaternion _teleportTargetRotation;
        private Quaternion _teleportRotation;

        private float _initialTeleportProjectileSpeed => 10f * _movementModeConfig.TeleportRangeMultiplier; // Initial speed of the projectile motion
        private float _simulationTimeStep = 0.05f;

        private Vector2 _currentTeleportDirection;
        private int _lineSegmentCount = 20; // Number of segments in the Bezier curve
        private float _maxSlopeAngle = 45f; // Maximum slope angle in degrees
        private bool _isTeleportTargetValid = false;
        #endregion

        public TeleportController(TeleportInputContainer inputContainer,
            LineRenderer teleportLineRenderer, LineRenderer interactorLineRenderer, GameObject teleportCursorPrefab,
            Transform rootTransform, Transform headTransform, Transform otherHandTransformReference,  
            FreeGrabbableWrapper thisHandGrabbableWrapper, FreeGrabbableWrapper otherHandGrabbableWrapper,
            HoveringOverScrollableIndicator hoveringOverScrollableIndicator, MovementModeConfig movementModeConfig)
        {
            _inputContainer = inputContainer;
            _rootTransform = rootTransform;
            _headTransform = headTransform;

            _interactorLineRenderer = interactorLineRenderer.GetComponent<LineRenderer>();
            _otherHandTransformReference = otherHandTransformReference;

            _teleportLineRenderer = teleportLineRenderer;
            _teleportLineRenderer.positionCount = _lineSegmentCount + 1;
            _teleportLineRenderer.enabled = false;
            _teleportLineRenderer.useWorldSpace = false;
            _teleportLineMaterial = Application.isPlaying ? _teleportLineRenderer.material : null;
            _teleportLineMaterial?.EnableKeyword("_EMISSION");

            _teleportCursor = GameObject.Instantiate(teleportCursorPrefab);
            _teleportCursor.transform.SetParent(_teleportRayOrigin.parent);
            _teleportCursor.SetActive(false);

            _thisHandGrabbableWrapper = thisHandGrabbableWrapper;
            _otherHandGrabbableWrapper = otherHandGrabbableWrapper;
            _hoveringOverScrollableIndicator = hoveringOverScrollableIndicator;

            _movementModeConfig = movementModeConfig;
        }

        public void HandleUpdate()
        {
            if (_teleporterActive) 
                UpdateTeleportRay();
        }

        public void HandleOEnable()
        {
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
            if (_hoveringOverScrollableIndicator.IsHoveringOverScrollableObject || _thisHandGrabbableWrapper.RangedFreeGrabInteraction != null)
                return; 

            _teleporterActive = true;
            ToggleTeleportVisual(true);
        }

        private void HandleTeleportDeactivated()
        {
            if (!_teleporterActive)
                return;

            _teleporterActive = false;
            ToggleTeleportVisual(false);

            if (_isTeleportTargetValid)
            {
                //Get raycast origin pos/rot
                Vector3 initialHandPosition = _otherHandTransformReference.position;
                Quaternion initialHandRotation = _otherHandTransformReference.rotation;

                // Teleport User
                _rootTransform.position = _hitPoint;
                _rootTransform.rotation = _teleportRotation;

                //Get raycast origin pos/rot again 
                Vector3 finallHandPosition = _otherHandTransformReference.position;
                Quaternion finalHandRotation = _otherHandTransformReference.rotation;

                //Delta between the two 
                Vector3 deltaPosition = finallHandPosition - initialHandPosition;
                Quaternion deltaRotation = finalHandRotation * Quaternion.Inverse(initialHandRotation);

                //if the other hand is grabbing something, teleport it along with us 
                //We don't have to worry about that for this hand, can't be teleporting if we're grabbing!
                _otherHandGrabbableWrapper.RangedFreeGrabInteraction?.ApplyDeltaWhenGrabbed(deltaPosition, deltaRotation); 
            }
        }

        private void ToggleTeleportVisual(bool toggle)
        {
            if (!toggle) //If turning on, wait until it has its position
                _teleportCursor.SetActive(toggle);

            _teleportLineRenderer.enabled = toggle;
            _interactorLineRenderer.enabled = !toggle;
        }

        private void UpdateTeleportRay()
        {
            Vector3 startPosition = _teleportRayOrigin.position;
            Vector3 direction = _teleportRayOrigin.forward;

            if (_movementModeConfig.FreeFlyMode)
            {
                float stickX = _inputContainer.TeleportDirection.Value.x;
                float distance = Mathf.Lerp(1f, 3f, stickX); // TODO: Make this configurable
                Vector3 teleportDestination = startPosition + direction * distance;
                
                DrawStraightLine(startPosition, teleportDestination);

                if (CanTeleportToFreeFlyDestination(startPosition, teleportDestination))
                {
                    _teleportCursor.transform.position = teleportDestination;

                    /*TODO: We may need to rethink this. Terrain below may not be level, or even present at all 
                    possible approach: When entering freefly, maybe we should intead reconfigure the rig so the head is always at the root 
                    Then we just teleport the whole root to the target, and verticalDragMove up by root?
                    Would need to reconfigure the rig back when leaving freefly though
                    Maybe better approach: No rig reconfiguring, just look at the target teleport pos, and work out what delta we need to apply 
                    to the root transform so that's where the camera ends up... 
                    ...this approach might not work as well for drag though, maybe collapsing the rig is simpler after all*/
                    Vector3 delta = teleportDestination - _headTransform.position;
                    Vector3 newRootPosition = _rootTransform.position + delta;
                    if (Physics.Raycast(teleportDestination, Vector3.down, out RaycastHit hit, _movementModeConfig.TraversableLayers))
                    {
                        newRootPosition.y = Mathf.Max(newRootPosition.y, hit.point.y);
                    }

                    _hitPoint = newRootPosition;
                    Vector3 arrowDirection = _teleportRayOrigin.forward;
                    arrowDirection.y = 0;
                    if (arrowDirection != Vector3.zero)
                    {
                        arrowDirection.Normalize();
                    }
                    _teleportRotation =  Quaternion.LookRotation(arrowDirection, Vector3.up);

                    ToggleTeleportLineVisualShowsValid(true);

                    _teleportCursor.SetActive(true);
                    _isTeleportTargetValid = true;
                }
                else
                {
                    ToggleTeleportLineVisualShowsValid(false);

                    _teleportCursor.SetActive(false);
                    _isTeleportTargetValid = false;
                }
            }
            else
            {
                // Adjust initialSpeed to control how far/fast the arc travels.
                Vector3 initialVelocity = direction.normalized * _initialTeleportProjectileSpeed;

                // We simulate projectile motion: position = start + v0 * t + 0.5 * g * t^2.
                _simulationTimeStep = Mathf.Clamp(_simulationTimeStep, 0.001f, 0.1f); // Ensure it doesn't go above 0.1 seconds.
                float simulationMaxTime = 5.0f; // Adjust maximum simulation time if needed.
                List<Vector3> trajectoryPoints = new List<Vector3> { startPosition };

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

                    //First check, does the teleporter hit anything in the traversable, or collision layers?
                    if (Physics.Raycast(previousPoint, segmentDir, out hit, segmentDistance, _movementModeConfig.CollisionLayers | _movementModeConfig.TraversableLayers))
                    {
                        GameObject hitObject = hit.collider.gameObject;

                        // Second check, is the thing that was hit traversable? If not, it's a blocking object, and we can't teleport
                        if (!CommonUtils.IsGameObjectInLayerMask(hitObject, _movementModeConfig.TraversableLayers))
                        {
                            hitFound = false;
                            break;
                        }

                        // Otherwise, it's traversable, so mark as valid and update the trajectory point.
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
                    _teleportCursor.transform.position = _hitPoint;

                    UpdateTargetRotation(hit.normal);

                    // Update the line renderer with the simulated trajectory points.
                    _teleportLineRenderer.positionCount = trajectoryPoints.Count;
                    for (int i = 0; i < trajectoryPoints.Count; i++)
                    {
                        _teleportLineRenderer.SetPosition(i, _teleportLineRenderer.transform.InverseTransformPoint(trajectoryPoints[i]));
                    }
                    ToggleTeleportLineVisualShowsValid(true);
                    _teleportCursor.SetActive(true);
                    _isTeleportTargetValid = true;
                }
                else
                {
                    // If no valid hit, render the full arc in red.
                    _teleportLineRenderer.positionCount = trajectoryPoints.Count;
                    for (int i = 0; i < trajectoryPoints.Count; i++)
                    {
                        _teleportLineRenderer.SetPosition(i, _teleportLineRenderer.transform.InverseTransformPoint(trajectoryPoints[i]));
                    }
                    ToggleTeleportLineVisualShowsValid(false);
                    _teleportCursor.SetActive(false);
                    _isTeleportTargetValid = false;
                }
            }
        }

        private void ToggleTeleportLineVisualShowsValid(bool toggle)
        {
            if (_teleportLineMaterial == null)
                return;

            Color newColor = toggle ? _colorConfig.TeleportValidColor : _colorConfig.TeleportInvalidColor;
            _teleportLineMaterial.color = newColor;
            _teleportLineMaterial.SetColor("_EmissionColor", newColor * LINE_EMISSION_INTENSITY);
        }

        private bool IsValidSurface(Vector3 normal)
        {
            // Check if the surface normal is within the acceptable slope angle
            float angle = Vector3.Angle(Vector3.up, normal);
            return angle <= _maxSlopeAngle;
        }

        private void UpdateTargetRotation(Vector3 surfaceNormal)
        {
            // Set arrow direction based on the raycast origin's horizontal forward.
            Vector3 arrowDirection = _teleportRayOrigin.forward;
            arrowDirection.y = 0;
            if (arrowDirection != Vector3.zero)
                arrowDirection.Normalize();
            _teleportCursor.transform.rotation = Quaternion.LookRotation(arrowDirection, Vector3.up);

            // Get input direction and compute an additional rotation from it.
            _currentTeleportDirection = _inputContainer.TeleportDirection.Value;
            float rotationAngle = Mathf.Atan2(_currentTeleportDirection.x, _currentTeleportDirection.y) * Mathf.Rad2Deg;
            _teleportTargetRotation = Quaternion.Euler(0, rotationAngle, 0);
            _teleportRotation = _teleportCursor.transform.rotation * _teleportTargetRotation;

            // Use the input rotation to determine a desired forward direction.
            Vector3 desiredForward = _teleportTargetRotation * arrowDirection;
            // Project that forward vector onto the plane defined by the surface normal.
            desiredForward = Vector3.ProjectOnPlane(desiredForward, surfaceNormal).normalized;
            if (desiredForward == Vector3.zero)
            {
                // Fallback: if projection fails, use the original forward vector.
                desiredForward = arrowDirection;
            }
            _teleportCursor.transform.rotation = Quaternion.LookRotation(desiredForward, surfaceNormal);
        }

        private void DrawStraightLine(Vector3 startPosition, Vector3 endPosition)
        {
            _teleportLineRenderer.positionCount = 2;
            _teleportLineRenderer.SetPosition(0, _teleportLineRenderer.transform.InverseTransformPoint(startPosition));
            _teleportLineRenderer.SetPosition(1, _teleportLineRenderer.transform.InverseTransformPoint(endPosition));
        }

        private bool CanTeleportToFreeFlyDestination(Vector3 startPosition, Vector3 teleportDestination)
        {
            Vector3 direction = teleportDestination - startPosition;
            float distance = direction.magnitude;
            direction.Normalize();

            if (Physics.Raycast(startPosition, direction, distance, _movementModeConfig.CollisionLayers))
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
