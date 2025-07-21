using UnityEngine;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.Player.API;
using VE2.Core.VComponents.API;

namespace VE2.Core.Player.Internal
{
    internal class DragLocomotorController
    {   
        private Vector3 _previousHandPosition;
        private Vector3 _savedHeadOffset;
        private float _dragSpeed = 4.0f;
        private bool _isDraggingHorizontal = false; //This is used to set the state of the current hand dragging horizontally based on the release/pressed events from the input container.
        private bool _isDraggingVertical = false; //This is used to set the state of the current hand dragging vertically based on the release/pressed events from the input container.

        private readonly GameObject _iconHolder; //Entire icon
        private readonly GameObject _horizontalMoveIndicator;
        private readonly GameObject _verticalMoveIndicator;
        private readonly GameObject _sphereIcon;

        private readonly DragLocomotorInputContainer _inputContainer;
        private readonly DragLocomotorInputContainer _otherVRHandInputContainer;
        private readonly Transform _rootTransform; //For horizontal drag
        private readonly Transform _headOffsetTransform; //For vertical drag
        private readonly Transform _headTransform; //For orienting the drag icons towards the camera
        private readonly Transform _handTransform; //For measuring drag delta 
        private MovementModeConfig _movementModeConfig;
        private bool _previousFreeFlyModeStatus = false;

        public DragLocomotorController(DragLocomotorReferences locomotorVRReferences, DragLocomotorInputContainer inputContainer, DragLocomotorInputContainer otherVRHandInputContainer,
            Transform rootTransform, Transform headOffsetTransform, Transform headTransform, Transform handTransform, MovementModeConfig movementModeConfig)
        {
            _iconHolder = locomotorVRReferences.DragIconHolder;
            _horizontalMoveIndicator = locomotorVRReferences.HorizontalDragIndicator;
            _verticalMoveIndicator = locomotorVRReferences.VerticalDragIndicator;
            _sphereIcon = locomotorVRReferences.SphereDragIcon;

            _inputContainer = inputContainer;
            _otherVRHandInputContainer = otherVRHandInputContainer;

            _rootTransform = rootTransform;
            _headOffsetTransform = headOffsetTransform;
            _headTransform = headTransform;
            _handTransform = handTransform;

            _movementModeConfig = movementModeConfig;
            _previousFreeFlyModeStatus = _movementModeConfig.FreeFlyMode;
        }

        public void HandleUpdate()
        {
            // Check if FreeFlyMode has changed
            if (_movementModeConfig.FreeFlyMode != _previousFreeFlyModeStatus)
            {
                if (_movementModeConfig.FreeFlyMode)
                    EnterFreeFlyMode();
                else
                    ExitFreeFlyMode();

                _previousFreeFlyModeStatus = _movementModeConfig.FreeFlyMode;
            }

            Vector3 cameraToIcon = _sphereIcon.transform.position - _headTransform.position;
            Vector3 forwardDirection = Vector3.ProjectOnPlane(cameraToIcon, Vector3.up);
            _horizontalMoveIndicator.transform.forward = forwardDirection;
            _verticalMoveIndicator.transform.forward = forwardDirection;

            if (_isDraggingHorizontal)
            {
                Vector3 horizontalDragDirection = new Vector3(_previousHandPosition.x - _handTransform.position.x, 0, _previousHandPosition.z - _handTransform.position.z);
                PerformHorizontalDragMovement(horizontalDragDirection);
            }

            if (_isDraggingVertical)
            {
                Vector3 verticalDragDirection = new Vector3(0, _previousHandPosition.y - _handTransform.position.y, 0);
                HandleVerticalDragMovement(verticalDragDirection);
            }

            _previousHandPosition = _handTransform.position; 
        }

        private void EnterFreeFlyMode()
        {
            // Save the current head offset position
            _savedHeadOffset = _headTransform.localPosition;
            // Collapse the rig
            _headTransform.localPosition = Vector3.zero;
            // Collapse the rig: Move the vertical offset to zero
            _headOffsetTransform.localPosition = Vector3.zero;
            //_movementModeConfig.OnFreeFlyModeEnter?.Invoke(); //NOTE: FreeFlyMode is mostly changed by Plugin so we might not need events for this but it lives here for now
        }

        private void ExitFreeFlyMode()
        {
            // Raycast down to find the ground
            if (Physics.Raycast(_rootTransform.position, Vector3.down, out RaycastHit hit, Mathf.Infinity, _movementModeConfig.TraversableLayers))
            {
                // Ground found, move to ground position
                _rootTransform.position = hit.point;
            }
            else
            {
                // Ground not found, move to spawn position
                _rootTransform.position = GetSpawnPosition();
            }
            
            //Restore the head offset position
            _headTransform.localPosition = _savedHeadOffset;
            //_movementModeConfig.OnFreeFlyModeExit?.Invoke(); //NOTE: FreeFlyMode is mostly changed by Plugin so we might not need events for this but it lives here for now>>>>>>> origin/develop
        }

        private Vector3 GetSpawnPosition()
        {
            // TODO: Replace with actual spawn position retrieval logic
            return VE2API.Player.PlayerSpawnPoint;
        }

        public void HandleOEnable()
        {
            _horizontalMoveIndicator.SetActive(false);
            _verticalMoveIndicator.SetActive(false);
            _sphereIcon.SetActive(false);

            _inputContainer.HorizontalDrag.OnPressed += HandleHorizontalDragPressed;
            _inputContainer.HorizontalDrag.OnReleased += HandleHorizontalDragReleased;
            _inputContainer.VerticalDrag.OnPressed += HandleVerticalDragPressed;
            _inputContainer.VerticalDrag.OnReleased += HandleVerticalDragReleased;

            _otherVRHandInputContainer.HorizontalDrag.OnReleased += HandleOtherVRHorizontalDragReleased;
            _otherVRHandInputContainer.VerticalDrag.OnReleased += HandleOtherVRVerticalDragReleased;
        }

        public void HandleOnDisable()
        {
            _inputContainer.HorizontalDrag.OnPressed -= HandleHorizontalDragPressed;
            _inputContainer.HorizontalDrag.OnReleased -= HandleHorizontalDragReleased;
            _inputContainer.VerticalDrag.OnPressed -= HandleVerticalDragPressed;
            _inputContainer.VerticalDrag.OnReleased -= HandleVerticalDragReleased;

            _otherVRHandInputContainer.HorizontalDrag.OnReleased -= HandleOtherVRHorizontalDragReleased;
            _otherVRHandInputContainer.VerticalDrag.OnReleased -= HandleOtherVRVerticalDragReleased;
        }

        private void HandleHorizontalDragPressed()
        {
            //Show horizontal icon
            if (_otherVRHandInputContainer.HorizontalDrag.IsPressed)
                return;

            SetIsDraggingHorizontal(true);
        }

        private void HandleHorizontalDragReleased()
        {
            //Hide horizontal icon
            SetIsDraggingHorizontal(false);
        }

        private void HandleVerticalDragPressed()
        {
            //Show vertical icon
            if (_otherVRHandInputContainer.VerticalDrag.IsPressed) 
                return;

            SetIsDraggingVertical(true);
        }

        private void HandleVerticalDragReleased()
        {
            //Hide vertical icon
            SetIsDraggingVertical(false);
        }

        private void HandleOtherVRHorizontalDragReleased()
        {
            //Show horizontal icon
            if (!_inputContainer.HorizontalDrag.IsPressed) 
                return;

            SetIsDraggingHorizontal(true);
        }

        private void HandleOtherVRVerticalDragReleased()
        {
            //Show horizontal icon
            if (!_inputContainer.VerticalDrag.IsPressed) 
                return;

            SetIsDraggingVertical(true);
        }

        private void SetIsDraggingHorizontal(bool isDraggingStatus)
        {
            _isDraggingHorizontal = isDraggingStatus;
            _horizontalMoveIndicator.SetActive(isDraggingStatus);

            if (_verticalMoveIndicator.activeSelf)
                _sphereIcon.SetActive(true);

            if (!isDraggingStatus)
                _sphereIcon.SetActive(false);
        }

        private void SetIsDraggingVertical(bool isDraggingStatus)
        {
            _isDraggingVertical = isDraggingStatus;
            _verticalMoveIndicator.SetActive(isDraggingStatus);

            if (_horizontalMoveIndicator.activeSelf)
                _sphereIcon.SetActive(true);

            if (!isDraggingStatus)
                _sphereIcon.SetActive(false);
        }

        private void PerformHorizontalDragMovement(Vector3 dragVector)
        {
            Vector3 moveVector = dragVector * _dragSpeed;
            float collisionOffset = 0.2f;

            if (_movementModeConfig.FreeFlyMode)
            {
                // In freefly mode, move the head transform directly, but perform collision checks
                Vector3 targetPosition = _headTransform.position + moveVector;
                Vector3 direction = moveVector.normalized;
                float distance = moveVector.magnitude + collisionOffset;

                if (Physics.Raycast(_headTransform.position, direction, out RaycastHit hitInfo, distance, _movementModeConfig.CollisionLayers))
                    Debug.Log($"Movement aborted: Collision detected with {hitInfo.collider.name}.");
                else
                    _rootTransform.position += moveVector;
            }
            else
            {
                // Existing movement logic with ground checks
                float maxStepHeight = 0.5f;
                float stepHeight = 0.5f; // TODO: Make configurable
                float obstacleOffset = 1.0f;

                Vector3 currentRaycastPosition = _rootTransform.position + new Vector3(0, maxStepHeight, 0);
                Vector3 targetRaycastPosition = currentRaycastPosition + moveVector;

                //Raycast from where we are, to where we are trying to be, to check for objects in our way
                //If we hit something in the collision layers, abort movement
                if (Physics.CapsuleCast(
                       _headTransform.position,
                       _headTransform.position - new Vector3(0, obstacleOffset, 0),
                       collisionOffset,
                       moveVector.normalized,
                       out RaycastHit obstacleHit,
                       moveVector.magnitude + collisionOffset,
                       _movementModeConfig.CollisionLayers))
                {
                    Debug.Log($"Movement blocked by {obstacleHit.collider.name}.");
                    return;
                }
                //TODO: There's def a bug here, we're able to get stuck on non-ground objects, and then we can't move away

                //Rayacst down from current position to check how high we are above ground
                //If we don't hit anything, or if the thing we hit is not within the traversable layers, abort movement
                if (!Physics.Raycast(currentRaycastPosition, Vector3.down, out RaycastHit groundHitFromCurrentPos, maxStepHeight + collisionOffset, _movementModeConfig.TraversableLayers))
                {
                    Debug.LogWarning($"Movement aborted: Current position is not above ground.Ground hit {groundHitFromCurrentPos.collider?.gameObject.name}");
                    return;
                }

                float currentGroundHeight = groundHitFromCurrentPos.point.y;

                //Raycast down from where we are trying to be, to check how high we will _become_ above ground
                //If we don't hit anything, or if the thing we hit is not within the traversable layers, abort movement
                if (!Physics.Raycast(targetRaycastPosition, Vector3.down, out RaycastHit groundHitFromTargetPos, maxStepHeight + collisionOffset, _movementModeConfig.TraversableLayers))
                {
                    Debug.Log("Movement aborted: No floor at destination.");
                    return;
                }
                
                float targetGroundHeight = groundHitFromTargetPos.point.y;
                float heightDifference = Mathf.Abs(targetGroundHeight - currentGroundHeight);

                if (heightDifference > stepHeight)
                {
                    Debug.Log("Movement aborted: Elevation change exceeds maximum step size.");
                    return;
                }


                Vector3 finalPosition = _rootTransform.position + moveVector;
                finalPosition.y = targetGroundHeight;
                _rootTransform.position = finalPosition;

                //Raycast from where we are, to where we are trying to be, to check for objects in our way
                //If we hit something in the collision layers, abort movement
                RaycastHit[] collisionHits = Physics.RaycastAll(currentRaycastPosition, moveVector.normalized, moveVector.magnitude + collisionOffset, _movementModeConfig.CollisionLayers);
                foreach (RaycastHit hit in collisionHits)
                {
                    //If the thing we hit was within our layermask ^ and it is not a grabbable object that is currently grabbed by the local player, abort movement
                    if (!(hit.collider.gameObject.TryGetComponent(out IV_FreeGrabbable grabbable) && grabbable.IsGrabbed && grabbable.MostRecentInteractingClientID.IsLocal))
                    {
                        Debug.Log($"Movement aborted: {hit.collider.name} is blocking player movement.");
                        return;
                    }
                }    

                // Move the root transform to the target position, adjusting for ground height
                float newRootPositionY= targetGroundHeight + (currentRaycastPosition.y - maxStepHeight - currentGroundHeight);
                _rootTransform.position = new(targetRaycastPosition.x, newRootPositionY, targetRaycastPosition.z);
            }

            _movementModeConfig.OnHorizontalDrag?.Invoke();
        }

        private void HandleVerticalDragMovement(Vector3 dragVector)
        {
            Vector3 moveVector = dragVector * _dragSpeed;
            float collisionOffset = 0.05f;

            if (_movementModeConfig.FreeFlyMode)
            {
                // In freefly mode, move the root transform directly, but perform collision checks
                Vector3 targetPosition = _rootTransform.position + moveVector;
                Vector3 direction = moveVector.normalized;
                float distance = moveVector.magnitude + collisionOffset;

                if (Physics.Raycast(_headTransform.position, direction, out RaycastHit hitInfo, distance, _movementModeConfig.CollisionLayers))
                    Debug.Log($"Vertical movement aborted: Collision detected with {hitInfo.collider.name}.");
                else
                    _rootTransform.position = targetPosition;
            }
            else
            {
                // Existing vertical movement logic with collision checks
                Vector3 targetPosition = _headOffsetTransform.position + moveVector;

                if (Physics.Raycast(_headOffsetTransform.position, moveVector.normalized, out RaycastHit hit, moveVector.magnitude + collisionOffset, _movementModeConfig.CollisionLayers))
                    Debug.Log("Vertical movement aborted: Collision detected with " + hit.collider.name);
                else
                    _headOffsetTransform.position = targetPosition;
            }

            _movementModeConfig.OnVerticalDrag?.Invoke();
        }
    }
}
