using UnityEngine;
using VE2.Core.Player.API;

namespace VE2.Core.Player.Internal
{
    internal class DragLocomotor
    {   
        private DragLocomotorInputContainer _otherVRHandInputContainer;

        private Vector3 _previousHandPosition;
        private float _dragSpeed = 2.0f;
        private bool _isDraggingHorizontal = false; //This is used to set the state of the current hand dragging horizontally based on the release/pressed events from the input container.
        private bool _isDraggingVertical = false; //This is used to set the state of the current hand dragging vertically based on the release/pressed events from the input container.

        private readonly GameObject _iconHolder; //Entire icon
        private readonly GameObject _horizontalMoveIndicator;
        private readonly GameObject _verticalMoveIndicator;
        private readonly GameObject _sphereIcon;

        private readonly DragLocomotorInputContainer _inputContainer;
        private readonly Transform _rootTransform; //For horizontal drag
        private readonly Transform _headOffsetTransform; //For vertical drag
        private readonly Transform _handTransform; //For measuring drag delta 
        private LayerMask groundLayerMask => LayerMask.GetMask("Ground");


        public DragLocomotor(DragLocomotorReferences locomotorVRReferences, DragLocomotorInputContainer inputContainer, DragLocomotorInputContainer otherVRHandInputContainer,
            Transform rootTransform, Transform headOffsetTransform, Transform handTransform)
        {
            _iconHolder = locomotorVRReferences.DragIconHolder;
            _horizontalMoveIndicator = locomotorVRReferences.HorizontalDragIndicator;
            _verticalMoveIndicator = locomotorVRReferences.VerticalDragIndicator;
            _sphereIcon = locomotorVRReferences.SphereDragIcon;

            _inputContainer = inputContainer;
            _otherVRHandInputContainer = otherVRHandInputContainer;

            _rootTransform = rootTransform;
            _headOffsetTransform = headOffsetTransform;
            _handTransform = handTransform;
        }

        public void HandleUpdate()
        {
            Vector3 cameraToIcon = _sphereIcon.transform.position - _headOffsetTransform.position;
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
            float maxStepHeight = 0.5f; // Maximum step height the player can have 
            float stepHeight = 0.5f; // User-defined step height. TODO: Make it configurable
            float collisionOffset = 0.5f; // Collision offset to stop player entering into walls

            Vector3 currentRaycastPosition = _rootTransform.position + new Vector3(0, maxStepHeight, 0);
            Vector3 targetRaycastPosition = currentRaycastPosition + moveVector;

            // Perform raycast from current raycast position to check for ground
            if (Physics.Raycast(currentRaycastPosition, Vector3.down, out RaycastHit currentHit, Mathf.Infinity, groundLayerMask))
            {
                float currentGroundHeight = currentHit.point.y;

                // Perform raycast from target position to check for ground
                if (Physics.Raycast(targetRaycastPosition, Vector3.down, out RaycastHit targetHit, Mathf.Infinity, groundLayerMask))
                {
                    float targetGroundHeight = targetHit.point.y;
                    float heightDifference = Mathf.Abs(targetGroundHeight - currentGroundHeight);

                    // Check if the height difference is within the allowable step size
                    if (heightDifference <= stepHeight)
                    {
                        // Perform raycast to check for collisions in the direction of movement
                        if (Physics.Raycast(currentRaycastPosition, moveVector.normalized, out RaycastHit objectHit, moveVector.magnitude + collisionOffset))
                        {
                            Debug.Log($"Movement aborted: {objectHit.collider.name} is blocking player movement.");
                        }
                        else
                        {                          
                            targetRaycastPosition.y = targetGroundHeight + (currentRaycastPosition.y - maxStepHeight - currentGroundHeight);
                            _rootTransform.position = targetRaycastPosition;
                        }
                    }
                    else
                    {
                        Debug.Log("Movement aborted: Elevation change exceeds maximum step size.");
                    }
                }
                else
                {
                    Debug.Log("Movement aborted: Target position is not above ground.");
                }
            }
            else
            {
                Debug.LogWarning("Current position is not above ground.");
            }
        }

        private void HandleVerticalDragMovement(Vector3 dragVector)
        {
            Vector3 moveVector = dragVector * _dragSpeed;
            Vector3 targetPosition = _headOffsetTransform.position + moveVector;
            float collisionOffset = 0.5f; // Collision offset to stop player entering into ceilings/floor

            // Perform raycast to check for collisions
            if (Physics.Raycast(_headOffsetTransform.position, moveVector.normalized, out RaycastHit hit, moveVector.magnitude + collisionOffset))
            {
                Debug.Log("Vertical movement aborted: Collision detected with " + hit.collider.name);
            }
            else
            {
                _headOffsetTransform.position = targetPosition;
            }
        }
    }
}
