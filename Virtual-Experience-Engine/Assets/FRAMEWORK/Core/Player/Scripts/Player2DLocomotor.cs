using UnityEngine;
using UnityEngine.InputSystem;

namespace VE2.Core.Player
{
    public class Player2DLocomotor : MonoBehaviour
    {
        // Public variables
        public float moveSpeed = 5f;
        public float mouseSensitivity = 2f;
        public float jumpForce = 5f;
        public float crouchHeight = 0.7f;
        public LayerMask groundLayer;
        public float minVerticalAngle = -90f; // Minimum vertical angle (looking down)
        public float maxVerticalAngle = 90f;  // Maximum vertical angle (looking up)

        // Private variables
        private CharacterController controller;
        private float _originalControllerHeight;
        private Transform cameraTransform;
        private float verticalVelocity = 0f;
        private bool isCrouching = false;
        private float verticalRotation = 0f; // To keep track of vertical rotation
        private bool isCursorLocked = true;  // Flag to control camera movement

        //private IInputWrapperPlayer _inputHandler;

        // public void Initialize(IInputWrapperPlayer inputHandler)
        // {
        //     _inputHandler = inputHandler;
        // }

        private void OnEnable()
        {
            
        }

        void Start()
        {
            controller = GetComponent<CharacterController>();
            _originalControllerHeight = controller.height;
            cameraTransform = Camera.main.transform;

            //Debug.Log("START " + Application.isFocused);

            Application.focusChanged += OnFocusChanged;
            if (Application.isFocused)
                LockCursor();
        }

        private void OnFocusChanged(bool focus)
        {
            //Debug.Log("====================GAIN FOCUS " + focus);
            if (focus && isCursorLocked)
                LockCursor();
        }

        void Update() //TODO: Should listen to InputHandler, this should maybe go in FixedUpdate to keep grabbables happy (they are updated in FixedUpdate)
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
                transform.Rotate(Vector3.up * mouseX);

                float mouseY = Mouse.current.delta.y.ReadValue() * mouseSensitivity;
                verticalRotation -= mouseY;
                verticalRotation = Mathf.Clamp(verticalRotation, minVerticalAngle, maxVerticalAngle);
                cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

                // Movement
                float moveX = Keyboard.current.dKey.ReadValue() - Keyboard.current.aKey.ReadValue();
                float moveZ = Keyboard.current.wKey.ReadValue() - Keyboard.current.sKey.ReadValue();
                Vector3 moveDirection = transform.TransformDirection(new Vector3(moveX, 0, moveZ));
                controller.Move(moveDirection * moveSpeed * Time.deltaTime);

                // Jump
                if (Keyboard.current.spaceKey.wasPressedThisFrame && IsGrounded())
                {
                    verticalVelocity = jumpForce;
                }

                // Crouch
                if (Keyboard.current.leftCtrlKey.wasPressedThisFrame)
                {
                    if (isCrouching)
                    {
                        controller.Move(Vector3.up * (_originalControllerHeight - controller.height)); //Bodge so we don't fall through the floort
                        controller.height = _originalControllerHeight;
                    }
                    else
                        controller.height = crouchHeight;

                    isCrouching = !isCrouching;
                }
            }

            // Apply gravity
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
            controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
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

        bool IsGrounded()
        {
            RaycastHit hit;
            return Physics.Raycast(transform.position, Vector3.down, out hit, (controller.height / 2) + 0.1f, groundLayer);
        }
    }
}
