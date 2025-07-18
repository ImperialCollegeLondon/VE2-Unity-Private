using UnityEngine;
using VE2.Core.Player.API;
using VE2.Core.VComponents.API;

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
        private readonly FreeGrabbingIndicator _grabbingIndicator;

        private PlayerLocomotor2DInputContainer _playerLocomotor2DInputContainer;
        private readonly Player2DMovementConfig _player2DMovementConfig;
        private LayerMask _traversableLayers => _movementModeConfig.TraversableLayers;
        private float _originalControllerHeight;
        private float verticalVelocity = 0f;
        private bool isCrouching = false;
        private float verticalRotation = 0f; // To keep track of vertical rotation
        private bool isCursorLocked = true;  // Flag to control camera movement

        private Vector3 _originalControllerCenter;
        private bool _wasFreeFlyMode;

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


        internal Player2DLocomotor(Locomotor2DReferences locomotor2DReferences, MovementModeConfig movementModeConfig, InspectModeIndicator inspectModeIndicator, PlayerLocomotor2DInputContainer playerLocomotor2DInputContainer, Player2DMovementConfig player2DMovementConfig, FreeGrabbingIndicator grabbingIndicator)
        {
            _characterController = locomotor2DReferences.Controller;
            _verticalOffsetTransform = locomotor2DReferences.VerticalOffsetTransform;
            _originalControllerHeight = locomotor2DReferences.Controller.height;
            _originalControllerCenter = locomotor2DReferences.Controller.center;
            _cameraTransform = locomotor2DReferences.CameraTransform;

            _movementModeConfig = movementModeConfig;
            _characterController.includeLayers = movementModeConfig.TraversableLayers | movementModeConfig.CollisionLayers;
            _player2DMovementConfig = player2DMovementConfig;

            _inspectModeIndicator = inspectModeIndicator;
            _grabbingIndicator = grabbingIndicator;

            //TODO tear down and unsubscribe
            grabbingIndicator.OnGrabStarted += HandleGrabStarted;
            grabbingIndicator.OnGrabEnded += HandleGrabEnded;

            _playerLocomotor2DInputContainer = playerLocomotor2DInputContainer;


            Application.focusChanged += OnFocusChanged;
            if (Application.isFocused)
                LockCursor();
        }

        private void HandleGrabStarted(IRangedFreeGrabInteractionModule freeGrabbable)
        {
            Collider collider = freeGrabbable.ColliderWrapper.Collider;

            if (collider != null) //Bit of a code smell, would be null in tests since we can't stub it out
                Physics.IgnoreCollision(_characterController, freeGrabbable.ColliderWrapper.Collider, true);
        }

        private void HandleGrabEnded(IRangedFreeGrabInteractionModule freeGrabbable)
        {
             Collider collider = freeGrabbable.ColliderWrapper.Collider;

            if (collider != null) //Bit of a code smell, would be null in tests since we can't stub it out
                Physics.IgnoreCollision(_characterController, freeGrabbable.ColliderWrapper.Collider, false);
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

            // Detect FreeFlyMode changes
            if (_movementModeConfig.FreeFlyMode != _wasFreeFlyMode)
            {
                if (_movementModeConfig.FreeFlyMode)
                    EnterFreeFlyMode();
                else
                    ExitFreeFlyMode();

                _wasFreeFlyMode = _movementModeConfig.FreeFlyMode;
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

                // FreeFlyMode movement
                if (_movementModeConfig.FreeFlyMode)
                {
                    Vector3 freeFlyMove = moveDirection * speed * Time.deltaTime;

                    // Move up when Jump is pressed
                    if (_playerLocomotor2DInputContainer.Jump.IsPressed)
                        freeFlyMove += Vector3.up * speed * Time.deltaTime;

                    // Move down when Crouch is pressed
                    if (_playerLocomotor2DInputContainer.Crouch.IsPressed)
                        freeFlyMove += Vector3.down * speed * Time.deltaTime;

                    _characterController.Move(freeFlyMove);
                    return; // Skip normal jump logic in free fly mode
                }

                // Jump
                if (_playerLocomotor2DInputContainer.Jump.IsPressed && IsGrounded())
                {
                    verticalVelocity = _player2DMovementConfig.jumpForce;
                    _movementModeConfig.OnJump2D?.Invoke();
                }
            }

            if (_movementModeConfig.FreeFlyMode)
                return; // No gravity in free fly mode
            // Apply gravity
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
            _characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
        }

        private void EnterFreeFlyMode()
        {
            _characterController.height = 0.1f;
            _characterController.center = new Vector3(0, 1.7f, 0);

            //_movementModeConfig.OnFreeFlyModeEnter2D?.Invoke(); //NOTE: FreeFlyMode is mostly changed by Plugin so we might not need events for this but it lives here for now
        }

        private void ExitFreeFlyMode()
        {
            // Check for ground below the player using traversable layers
            Vector3 rayOrigin = _transform.position + Vector3.up * 0.1f; // Slightly above to avoid self-collision
            float rayDistance = 100f; // Large enough to reach the ground

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, rayDistance, _traversableLayers))
            {
                // Ground found, snap player to ground and exit free fly mode
                Vector3 groundedPosition = _transform.position;
                groundedPosition.y = hit.point.y + (_originalControllerHeight / 2f);
                _characterController.enabled = false;
                _transform.position = groundedPosition;
                _characterController.enabled = true;

                _characterController.height = _originalControllerHeight;
                _characterController.center = _originalControllerCenter;
                _wasFreeFlyMode = false;
                _movementModeConfig.FreeFlyMode = false;
                //_movementModeConfig.OnFreeFlyModeExit2D?.Invoke(); //NOTE: FreeFlyMode is mostly changed by Plugin so we might not need events for this but it lives here for now
            }
            else
            {
                // No ground found, move to spawn and stay in free fly mode
                Vector3 spawnPosition = GetSpawnPosition();
                _characterController.enabled = false;
                _transform.position = spawnPosition;
                _characterController.enabled = true;

                Debug.LogError("No valid traversible layer found in the scene. Cannot exit free fly mode. Player moved to spawn position.");
                // Remain in free fly mode
                _wasFreeFlyMode = true;
                _movementModeConfig.FreeFlyMode = true;
            }
        }

        private void HandleCrouch()
        {
            if (_movementModeConfig.FreeFlyMode)
                return; // No crouch toggle in free fly mode

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
        private Vector3 GetSpawnPosition()
        {
            // TODO: Replace with your actual spawn position logic
            // Example: return VE2API.Player.PlayerSpawnPoint;
            return Vector3.zero;
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
