using UnityEngine;
using VE2.Core.Player.API;

namespace VE2.Core.Player.Internal
{
    internal class Player2DLocomotor
    {
        private Transform _transform => _characterController.transform;
        private readonly CharacterController _characterController;
        private readonly Transform _verticalOffsetTransform;
        private readonly Transform _cameraTransform;

        private readonly MovementModeConfig _movementModeConfig;
        private readonly InspectModeIndicator _inspectModeIndicator;

        private PlayerLocomotor2DInputContainer _playerLocomotor2DInputContainer;
        private readonly Player2DMovementConfig _player2DMovementConfig;
        private LayerMask _traversableLayers => _movementModeConfig.TraversableLayers;
        private float _originalControllerHeight;
        private float verticalVelocity = 0f;
        private bool isCrouching = false;
        private float verticalRotation = 0f; // To keep track of vertical rotation
        private bool isCursorLocked = true;  // Flag to control camera movement

        public Vector3 RootPosition
        {
            get => _transform.position;
            set
            {
                _characterController.enabled = false;
                _transform.position = value;
                _characterController.enabled = true;
            }
        }

        public Quaternion RootRotation 
        {
            get => _transform.rotation;
            set => _transform.rotation = value;
        }

        public Transform HeadTransform => _cameraTransform;

        public Vector3 HeadLocalPosition 
        {
            get => _cameraTransform.localPosition;
            set => _cameraTransform.localPosition = value;
        }

        public Quaternion HeadLocalRotation 
        {
            get => _cameraTransform.localRotation;
            set => _cameraTransform.localRotation = value;
        }

        public float VerticalOffset 
        {
            get => _verticalOffsetTransform.localPosition.y;
            set 
            {
                _verticalOffsetTransform.localPosition = new Vector3(0, value, 0);
                _characterController.height = value + 0.2f;
                _characterController.center = new Vector3(0, _characterController.height / 2, 0);
            }    
        }

        internal Player2DLocomotor(Locomotor2DReferences locomotor2DReferences, MovementModeConfig movementModeConfig, InspectModeIndicator inspectModeIndicator, PlayerLocomotor2DInputContainer playerLocomotor2DInputContainer, Player2DMovementConfig player2DMovementConfig)
        {
            _characterController = locomotor2DReferences.Controller;
            _verticalOffsetTransform = locomotor2DReferences.VerticalOffsetTransform;
            _originalControllerHeight = locomotor2DReferences.Controller.height;
            _cameraTransform = locomotor2DReferences.CameraTransform;

            _movementModeConfig = movementModeConfig;
            _characterController.includeLayers = movementModeConfig.TraversableLayers | movementModeConfig.CollisionLayers;
            _player2DMovementConfig = player2DMovementConfig;

            _inspectModeIndicator = inspectModeIndicator;
            _playerLocomotor2DInputContainer = playerLocomotor2DInputContainer;

            Application.focusChanged += OnFocusChanged;
            if (Application.isFocused)
                LockCursor();
        }

        private void OnFocusChanged(bool focus)
        {
            if (focus && isCursorLocked)
                LockCursor();
        }

        public void HandleOnEnable()
        {
            //Listen to input 
            _playerLocomotor2DInputContainer.Crouch.OnPressed += HandleCrouch;
        }

        public void HandleOnDisable()
        {
            //Stop listening to input
            _playerLocomotor2DInputContainer.Crouch.OnPressed -= HandleCrouch;
        }

        public void HandleUpdate() //TODO: Should listen to InputHandler, this should maybe go in FixedUpdate to keep grabbables happy (they are updated in FixedUpdate) 
        {
            //TODO: This should be in a separate class, maybe even in the interactor2D? 
            //Needs to be injected with UIService, needs to know to not lock cursor when detecting left click if UI is showing

            // Handle Escape key to unlock the cursor
            if (_playerLocomotor2DInputContainer.UnlockCursor.IsPressed)
            {
                UnlockCursor();
            }

            // Check for mouse click to re-lock the cursor
            if (_playerLocomotor2DInputContainer.LockCursor.IsPressed && !isCursorLocked)
            {
                LockCursor();
            }

            if (Application.isFocused && isCursorLocked)
            {
                // Mouse look
                if (!_inspectModeIndicator.IsInspectModeActive)
                {
                    float mouseX = _playerLocomotor2DInputContainer.MouseDelta.Value.x * _player2DMovementConfig.mouseSensitivity;
                    _transform.Rotate(Vector3.up * mouseX);

                    float mouseY = _playerLocomotor2DInputContainer.MouseDelta.Value.y * _player2DMovementConfig.mouseSensitivity;
                    verticalRotation -= mouseY;
                    verticalRotation = Mathf.Clamp(verticalRotation, _player2DMovementConfig.minVerticalAngle, _player2DMovementConfig.maxVerticalAngle);
                    _cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
                }

                float moveX = (_playerLocomotor2DInputContainer.Right?.IsPressed == true ? 1f : 0f)
                                - (_playerLocomotor2DInputContainer.Left?.IsPressed == true ? 1f : 0f);
                float moveZ = (_playerLocomotor2DInputContainer.Forward?.IsPressed == true ? 1f : 0f)
                                - (_playerLocomotor2DInputContainer.Backward?.IsPressed == true ? 1f : 0f);

                Vector3 moveDirection = _transform.TransformDirection(new Vector3(moveX, 0, moveZ));

                float speed = _playerLocomotor2DInputContainer.IsSprinting2D.IsPressed ? _player2DMovementConfig.walkSpeed * _player2DMovementConfig.sprintSpeedMultiplier : _player2DMovementConfig.walkSpeed;
                _characterController.Move(moveDirection * speed * Time.deltaTime);

                // Jump
                if (_playerLocomotor2DInputContainer.Jump.IsPressed && IsGrounded())
                {
                    verticalVelocity = _player2DMovementConfig.jumpForce;
                    _movementModeConfig.OnJump2D?.Invoke();
                }
            }

            // Apply gravity
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
            _characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
        }

        private void HandleCrouch()
        {
            if (isCrouching)
            {
                _characterController.Move(Vector3.up * (_originalControllerHeight - _characterController.height)); //Bodge so we don't fall through the floor
                _characterController.height = _originalControllerHeight;
            }
            else
            {
                _characterController.height = _player2DMovementConfig.crouchHeight;
                _movementModeConfig.OnCrouch2D?.Invoke();
            }

            isCrouching = !isCrouching;
        }
        private void LockCursor()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            isCursorLocked = true;
        }

        private void UnlockCursor()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            isCursorLocked = false;
        }

        bool IsGrounded() =>
            Physics.Raycast(_transform.position, Vector3.down, out RaycastHit hit, (_characterController.height / 2) + 0.1f, _traversableLayers);
    }
}
