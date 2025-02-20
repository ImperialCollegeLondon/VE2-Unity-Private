using UnityEngine;
using UnityEngine.InputSystem;

namespace VE2.Core.Player.Internal
{
    internal class Player2DLocomotor
    {
        //TODO: make private, could be wired in via scriptable object?
        // Public variables
        public float moveSpeed = 5f;
        public float mouseSensitivity = 0.3f;
        public float jumpForce = 5f;
        public float crouchHeight = 0.7f;
        public float minVerticalAngle = -90f; // Minimum vertical angle (looking down)
        public float maxVerticalAngle = 90f;  // Maximum vertical angle (looking up)

        private Transform _transform => _characterController.transform;
        private readonly CharacterController _characterController;
        private readonly Transform _verticalOffsetTransform;
        private readonly Transform _cameraTransform;
        private readonly LayerMask _groundLayer;

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

        //TODO: Wire in input
        internal Player2DLocomotor(Locomotor2DReferences locomotor2DReferences)
        {
            _characterController = locomotor2DReferences.Controller;
            _verticalOffsetTransform = locomotor2DReferences.VerticalOffsetTransform;
            _originalControllerHeight = locomotor2DReferences.Controller.height;
            _cameraTransform = locomotor2DReferences.CameraTransform;
            _groundLayer = locomotor2DReferences.GroundLayer;

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
            //TODO: listen to input 
        }

        public void HandleOnDisable()
        {
            //TODO: stop listening to input
        }

        public void HandleUpdate() //TODO: Should listen to InputHandler, this should maybe go in FixedUpdate to keep grabbables happy (they are updated in FixedUpdate) 
        {
            // Handle Escape key to unlock the cursor
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                UnlockCursor();
            }

            // Check for mouse click to re-lock the cursor
            if (Mouse.current.leftButton.wasPressedThisFrame && !isCursorLocked)
            {
                LockCursor();
            }

            if (Application.isFocused && isCursorLocked)
            {
                // Mouse look
                float mouseX = Mouse.current.delta.x.ReadValue() * mouseSensitivity;
                _transform.Rotate(Vector3.up * mouseX);

                float mouseY = Mouse.current.delta.y.ReadValue() * mouseSensitivity;
                verticalRotation -= mouseY;
                verticalRotation = Mathf.Clamp(verticalRotation, minVerticalAngle, maxVerticalAngle);
                _cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

                // Movement
                float moveX = Keyboard.current.dKey.ReadValue() - Keyboard.current.aKey.ReadValue();
                float moveZ = Keyboard.current.wKey.ReadValue() - Keyboard.current.sKey.ReadValue();
                Vector3 moveDirection = _transform.TransformDirection(new Vector3(moveX, 0, moveZ));
                _characterController.Move(moveDirection * moveSpeed * Time.deltaTime);

                // Jump
                if (Keyboard.current.spaceKey.wasPressedThisFrame && IsGrounded())
                {
                    verticalVelocity = jumpForce;
                }

                // Crouch
                if (Keyboard.current.cKey.wasReleasedThisFrame)
                {
                    if (isCrouching)
                    {
                        _characterController.Move(Vector3.up * (_originalControllerHeight - _characterController.height)); //Bodge so we don't fall through the floor
                        _characterController.height = _originalControllerHeight;
                    }
                    else
                        _characterController.height = crouchHeight;

                    isCrouching = !isCrouching;
                }
            }

            // Apply gravity
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
            _characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
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
            Physics.Raycast(_transform.position, Vector3.down, out RaycastHit hit, (_characterController.height / 2) + 0.1f, _groundLayer);
    }
}
