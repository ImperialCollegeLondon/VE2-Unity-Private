using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VE2.Core.Player.API
{
    #region Input Types 

    public interface IValueInput<T>
    {
        public T Value { get; }
    }

    public class ValueInput<T> : IValueInput<T> where T : struct
    {
        public T Value => _inputAction.ReadValue<T>(); 

        private readonly InputAction _inputAction;

        public ValueInput(InputAction inputAction)
        {
            _inputAction = inputAction;
            _inputAction.Enable();
        }
    }

    public interface IPressableInput
    {
        public event Action OnPressed;
        public event Action OnReleased;
        public bool IsPressed { get; }
    }

    public class PressableInput : IPressableInput
    {
        public event Action OnPressed;
        public event Action OnReleased;
        public bool IsPressed => _inputAction.IsPressed();

        private readonly InputAction _inputAction;

        public PressableInput(InputAction inputAction)
        {
            _inputAction = inputAction;
            _inputAction.Enable();
            _inputAction.performed += ctx => OnPressed?.Invoke();
            _inputAction.canceled += ctx => OnReleased?.Invoke();
        }
    }

    public interface IScrollInput
    {
        public event Action OnTickOver;
    }

    public class ScrollInput : IScrollInput
    {
        public event Action OnTickOver;

        private readonly InputAction _inputAction;
        private float _minThreshold;
        private float _maxThreshold;
        private float _minTicksPerSecond;
        private float _maxTicksPerSecond;
        private float _timeOfLastTick;
        private bool _scrollUp;

        public ScrollInput(InputAction inputAction, float minThreshold, float maxThreshold, float minTicksPerSecond, float maxTicksPerSecond, bool scrollUp)
        {
            _inputAction = inputAction;
            _inputAction.Enable();

            _minThreshold = minThreshold;
            _maxThreshold = maxThreshold;
            _minTicksPerSecond = minTicksPerSecond;
            _maxTicksPerSecond = maxTicksPerSecond;
            _scrollUp = scrollUp;
        }

        public void HandleUpdate()
        {
            float inputValue = _inputAction.ReadValue<Vector2>().y;

            if (!_scrollUp)
                inputValue = -inputValue;

            if (inputValue < _minThreshold)
                return;

            float inputProgressToMaxThreshold = Mathf.InverseLerp(_minThreshold, _maxThreshold, inputValue);
            float currentTickInterval = 1 / Mathf.Lerp(_minTicksPerSecond, _maxTicksPerSecond, inputProgressToMaxThreshold); //1 / speed
            float timeSinceLastTick = Time.time - _timeOfLastTick;
            bool shouldTick = timeSinceLastTick >= currentTickInterval;

            if (shouldTick)
            {
                OnTickOver?.Invoke();
                _timeOfLastTick = Time.time;
            }
        }
    }

    public interface IStickPressInput
    {
        public event Action OnStickPressed;
        public event Action OnStickReleased;
    }

    public class StickPressInput : IStickPressInput
    {
        public event Action OnStickPressed;
        public event Action OnStickReleased;

        private readonly InputAction _inputAction;
        private float _minThreshold;
        private bool _isHorizontalStickPress;
        private bool _wasPressed;
        private bool _isNegativeDirection;

        public StickPressInput(InputAction inputAction, float minThreshold, bool isHorizontalStickPress, bool isNegativeDirection)
        {
            _inputAction = inputAction;
            _inputAction.Enable();
            _minThreshold = minThreshold;
            _isHorizontalStickPress = isHorizontalStickPress;
            _wasPressed = false;
            _isNegativeDirection = isNegativeDirection;
            _inputAction.canceled += ctx => 
            {
                OnStickReleased?.Invoke();
                _wasPressed = false; 
            };
        }

        public void HandleUpdate()
        {
            Vector2 inputValue = _inputAction.ReadValue<Vector2>();
            if (_isHorizontalStickPress) //Handle horizontal stick press
            {
                if (_isNegativeDirection)
                {
                    if (inputValue.x < -_minThreshold && inputValue.y < _minThreshold) //Checking if the Y axis stick is not being moved too much
                    {
                        if (!_wasPressed)
                        {
                            OnStickPressed?.Invoke();
                            _wasPressed = true;
                            return;
                        }
                    }
                }
                else
                {
                    if (inputValue.x > _minThreshold && inputValue.y < _minThreshold) //Checking if the Y axis stick is not being moved too much
                    {
                        if (!_wasPressed)
                        {
                            OnStickPressed?.Invoke();
                            _wasPressed = true;
                            return;
                        }
                    }
                }
            }
            else //Handle vertical stick press
            {
                if (_isNegativeDirection)
                {
                    if (inputValue.y < -_minThreshold)
                    {
                        if (!_wasPressed)
                        {
                            OnStickPressed?.Invoke();
                            _wasPressed = true;
                            return;
                        }
                    }
                }
                else
                {
                    if (inputValue.y > _minThreshold)
                    {
                        if (!_wasPressed)
                        {
                            OnStickPressed?.Invoke();
                            _wasPressed = true;
                            return;
                        }
                    }
                }
            }
        }
    }

    public class TeleportInput : IPressableInput, IValueInput<Vector2>
    {
        public event Action OnPressed;
        public event Action OnReleased;

        private readonly InputAction _inputAction;
        private float _minThreshold;
        private float _maxNeutralThreshold;
        private bool _wasPressedLastFrame;

        private bool _isPressed;
        public bool IsPressed 
        { 
            get
            {
                HandleUpdate(); //TO DO: Add a better way to handle this, InputHandler to execution order?
                return _isPressed;
            } 
            
            private set
            {
                _isPressed = value;
            }
        }

        public Vector2 Value => _inputAction.ReadValue<Vector2>();

        public TeleportInput(InputAction inputAction, float minThreshold, float maxNeutralThreshold)
        {
            _inputAction = inputAction;
            _inputAction.Enable();
            _minThreshold = minThreshold;
            _maxNeutralThreshold = maxNeutralThreshold;
        }

        public void HandleUpdate()
        {
            _wasPressedLastFrame = _isPressed;
            Vector2 inputValue = _inputAction.ReadValue<Vector2>();

            if (inputValue.y > _minThreshold)
            {
                if (!_wasPressedLastFrame)
                {
                    OnPressed?.Invoke();
                    IsPressed = true;
                }
            }
            else if (Mathf.Abs(inputValue.x) < _maxNeutralThreshold && Mathf.Abs(inputValue.y) < _maxNeutralThreshold && _wasPressedLastFrame)
            {
                OnReleased?.Invoke();
                IsPressed = false;
            }
        }
    }
    #endregion

    #region Input Containers
    public class PlayerInputContainer
    {
        public IPressableInput ChangeMode { get; private set; }
        public Player2DInputContainer Player2DInputContainer { get; private set; }
        public PlayerVRInputContainer PlayerVRInputContainer { get; private set; }

        public PlayerInputContainer(
            IPressableInput changeMode2D,
            IPressableInput inspectModeButton,
            IPressableInput rangedClick2D, IPressableInput grab2D, IPressableInput handheldClick2D, IScrollInput scrollTickUp2D, IScrollInput scrollTickDown2D,
            IPressableInput resetViewVR,
            IValueInput<Vector3> handVRLeftPosition, IValueInput<Quaternion> handVRLeftRotation,
            IPressableInput rangedClickVRLeft, IPressableInput grabVRLeft, IPressableInput handheldClickVRLeft, IScrollInput scrollTickUpVRLeft, IScrollInput scrollTickDownVRLeft,
            IPressableInput horizontalDragVRLeft, IPressableInput verticalDragVRLeft,
            IValueInput<Vector3> handVRRightPosition, IValueInput<Quaternion> handVRRightRotation,
            IPressableInput rangedClickVRRight, IPressableInput grabVRRight, IPressableInput handheldClickVRRight, IScrollInput scrollTickUpVRRight, IScrollInput scrollTickDownVRRight,
            IPressableInput horizontalDragVRRight, IPressableInput verticalDragVRRight,
            IStickPressInput stickPressHorizontalLeftDirectionVRLeft, IStickPressInput stickPressHorizontalRightDirectionVRLeft, 
            IStickPressInput stickPressHorizontalLeftDirectionVRRight, IStickPressInput stickPressHorizontalRightDirectionVRRight,
            IPressableInput stickPressVerticalVRLeft, IValueInput<Vector2> teleportDirectionVRLeft, IPressableInput stickPressVerticalVRRight, IValueInput<Vector2> teleportDirectionVRRight)
        {
            ChangeMode = changeMode2D;

            Player2DInputContainer = new(
                inspectModeButton,
                new InteractorInputContainer(rangedClick2D, grab2D, handheldClick2D, scrollTickUp2D, scrollTickDown2D)
            );

            PlayerVRInputContainer = new(
                resetViewVR,
                new HandVRInputContainer(handVRLeftPosition, handVRLeftRotation, 
                    new InteractorInputContainer(rangedClickVRLeft, grabVRLeft, handheldClickVRLeft, scrollTickUpVRLeft, scrollTickDownVRLeft),
                    new DragLocomotorInputContainer(horizontalDragVRLeft, verticalDragVRLeft),
                    new SnapTurnInputContainer(stickPressHorizontalLeftDirectionVRLeft, stickPressHorizontalRightDirectionVRLeft),
                    new TeleportInputContainer(stickPressVerticalVRLeft, teleportDirectionVRLeft)),
                new HandVRInputContainer(handVRRightPosition, handVRRightRotation, 
                    new InteractorInputContainer(rangedClickVRRight, grabVRRight, handheldClickVRRight, scrollTickUpVRRight, scrollTickDownVRRight),
                    new DragLocomotorInputContainer(horizontalDragVRRight, verticalDragVRRight),
                    new SnapTurnInputContainer(stickPressHorizontalLeftDirectionVRRight, stickPressHorizontalRightDirectionVRRight),
                    new TeleportInputContainer(stickPressVerticalVRRight, teleportDirectionVRRight))
            );
        }

        public void HandleUpdate()
        {
            
        }
    }

    public class Player2DInputContainer
    {
        public IPressableInput InspectModeButton { get; private set; }
        public InteractorInputContainer InteractorInputContainer2D { get; private set; }

        public Player2DInputContainer(IPressableInput inspectModeButton, InteractorInputContainer interactorInputContainer2D)
        {
            InspectModeButton = inspectModeButton;
            InteractorInputContainer2D = interactorInputContainer2D;
        }
    }

    public class PlayerVRInputContainer
    {
        public IPressableInput ResetView { get; private set; }
        public HandVRInputContainer HandVRLeftInputContainer { get; private set; }
        public HandVRInputContainer HandVRRightInputContainer { get; private set; }

        public PlayerVRInputContainer(IPressableInput resetView, HandVRInputContainer handVRLeftInputContainer, HandVRInputContainer handVRRightInputContainer)
        {
            ResetView = resetView;
            HandVRLeftInputContainer = handVRLeftInputContainer;
            HandVRRightInputContainer = handVRRightInputContainer;
        }
    }

    public class HandVRInputContainer
    {
        public IValueInput<Vector3> HandPosition { get; private set; }
        public IValueInput<Quaternion> HandRotation { get; private set; }
        public InteractorInputContainer InteractorVRInputContainer { get; private set; }
        public DragLocomotorInputContainer DragLocomotorInputContainer { get; private set; }
        public SnapTurnInputContainer SnapTurnInputContainer { get; private set; }
        public TeleportInputContainer TeleportInputContainer { get; private set; }

        public HandVRInputContainer(IValueInput<Vector3> handPosition, IValueInput<Quaternion> handRotation, InteractorInputContainer interactorVRInputContainer, DragLocomotorInputContainer dragLocomotorInputContainer = null, SnapTurnInputContainer snapTurnInputContainer = null, TeleportInputContainer teleportInputContainer = null)
        {
            HandPosition = handPosition;
            HandRotation = handRotation;
            InteractorVRInputContainer = interactorVRInputContainer;
            DragLocomotorInputContainer = dragLocomotorInputContainer;
            SnapTurnInputContainer = snapTurnInputContainer;
            TeleportInputContainer = teleportInputContainer;
        }
    }

    public class InteractorInputContainer
    {
        public IPressableInput RangedClick { get; private set; }
        public IPressableInput Grab { get; private set; }
        public IPressableInput HandheldClick { get; private set; }
        public IScrollInput ScrollTickUp { get; private set; }
        public IScrollInput ScrollTickDown { get; private set; }

        public InteractorInputContainer(IPressableInput rangedClick, IPressableInput grab, IPressableInput handheldClick, IScrollInput scrollTickUp, IScrollInput scrollTickDown)
        {
            RangedClick = rangedClick;
            Grab = grab;
            HandheldClick = handheldClick;
            ScrollTickUp = scrollTickUp;
            ScrollTickDown = scrollTickDown;
        }
    }

    public class DragLocomotorInputContainer 
    {
        public IPressableInput HorizontalDrag { get; private set; }
        public IPressableInput VerticalDrag { get; private set; }

        public DragLocomotorInputContainer(IPressableInput horizontalDrag, IPressableInput verticalDrag)
        {
            HorizontalDrag = horizontalDrag;
            VerticalDrag = verticalDrag;
        }
    }

    public class SnapTurnInputContainer
    {
        public IStickPressInput SnapTurnLeft { get; private set; }
        public IStickPressInput SnapTurnRight { get; private set; }

        public SnapTurnInputContainer(IStickPressInput snapTurnLeft, IStickPressInput snapTurnRight)
        {
            SnapTurnLeft = snapTurnLeft;
            SnapTurnRight = snapTurnRight;
        }
    }
    public class TeleportInputContainer
    {
        public IPressableInput Teleport { get; private set; }

        public IValueInput<Vector2> TeleportDirection { get; private set; }
        public TeleportInputContainer(IPressableInput teleport, IValueInput<Vector2> teleportDirection)
        {
            Teleport = teleport;
            TeleportDirection = teleportDirection;
        }
    }

    #endregion

    public interface IInputHandler
    {
        public PlayerInputContainer PlayerInputContainer { get; }
        public IPressableInput ToggleMenu { get; }
    }

    //TODO: The actual handler could go into its own assembly... where to draw the line though? Each interface could also go into its own assembly too...
    //Could also expose different interfaces in the ServiceLocator, rather than the single IInputHandler
    public class InputHandler : MonoBehaviour, IInputHandler
    {
        private PlayerInputContainer _playerInputContainer;
        public PlayerInputContainer PlayerInputContainer { 
            get {
            if (_playerInputContainer == null)
                CreateInputs();
            return _playerInputContainer;
        } 
            private set => _playerInputContainer = value;
        }
        
        public IPressableInput _toggleMenu { get; private set; }
        public IPressableInput ToggleMenu {
            get
            {
                if (_toggleMenu == null)
                    CreateInputs();
                return _toggleMenu;
            }
            private set => _toggleMenu = value;
        }

        //Special cases, need to be updated manually to mimic the mouse scroll wheel notches
        private List<ScrollInput> _scrollInputs;
        private const float MIN_SCROLL_THRESHOLD_2D = 0.1f;
        private const float MAX_SCROLL_THRESHOLD_2D = 1;
        private const float MIN_SCROLL_THRESHOLD_VR = 0.15f;
        private const float MAX_SCROLL_THRESHOLD_VR = 1;
        private const float MIN_SCROLL_TICKS_PER_SECOND_2D = 1;
        private const float MAX_SCROLL_TICKS_PER_SECOND_2D = 10;
        private const float MIN_SCROLL_TICKS_PER_SECOND_VR = 0.5f;
        private const float MAX_SCROLL_TICKS_PER_SECOND_VR = 5f;

        //Minimum threshold to detect thumbstick movement to process stick press input and teleport input
        private const float MIN_STICKPRESS_THRESHOLD = 0.8f;
        private const float MIN_TELEPORT_STICKPRESS_THRESHOLD = 0.8f;
        private const float MAX_TELEPORT_NEUTRAL_THRESHOLD = 0.3f;
        private List<StickPressInput> _stickPressInputs;
        private List<TeleportInput> _teleportInputs;
        private void CreateInputs()
        {
            InputActionAsset inputActionAsset = Resources.Load<InputActionAsset>("V_InputActions");

            // Player Action Map
            InputActionMap actionMapPlayer = inputActionAsset.FindActionMap("InputPlayer");
            PressableInput changeMode2D = new(actionMapPlayer.FindAction("ToggleMode"));

            // 2D Action Map
            InputActionMap actionMap2D = inputActionAsset.FindActionMap("Input2D");
            PressableInput inspectModeButton = new(actionMap2D.FindAction("InspectMode"));

            // 2D Interactor Action Map
            InputActionMap actionMapInteractor2D = inputActionAsset.FindActionMap("InputInteractor2D");
            PressableInput rangedClick2D = new(actionMapInteractor2D.FindAction("RangedClick"));
            PressableInput grab2D = new(actionMapInteractor2D.FindAction("Grab"));
            PressableInput handheldClick2D = new(actionMapInteractor2D.FindAction("HandheldClick"));
            ScrollInput scrollTickUp2D = new(actionMapInteractor2D.FindAction("ScrollValue"), MIN_SCROLL_THRESHOLD_2D, MAX_SCROLL_THRESHOLD_2D, MIN_SCROLL_TICKS_PER_SECOND_2D, MAX_SCROLL_TICKS_PER_SECOND_2D, true);
            ScrollInput scrollTickDown2D = new(actionMapInteractor2D.FindAction("ScrollValue"), MIN_SCROLL_THRESHOLD_2D, MAX_SCROLL_THRESHOLD_2D, MIN_SCROLL_TICKS_PER_SECOND_2D, MAX_SCROLL_TICKS_PER_SECOND_2D, false);

            // VR Action Map
            InputActionMap actionMapVR = inputActionAsset.FindActionMap("InputVR");
            PressableInput resetViewVR = new(actionMapVR.FindAction("ResetView"));

            // VR Left Hand Action Map
            InputActionMap actionMapHandVRLeft = inputActionAsset.FindActionMap("InputHandVRLeft");
            ValueInput<Vector3> handVRLeftPosition = new(actionMapHandVRLeft.FindAction("HandPosition"));
            ValueInput<Quaternion> handVRLeftRotation = new(actionMapHandVRLeft.FindAction("HandRotation"));

            // VR Left Interactor Action Map
            InputActionMap actionMapInteractorVRLeft = inputActionAsset.FindActionMap("InputInteractorVRLeft");
            PressableInput rangedClickVRLeft = new(actionMapInteractorVRLeft.FindAction("RangedClick"));
            PressableInput grabVRLeft = new(actionMapInteractorVRLeft.FindAction("Grab"));
            PressableInput handheldClickVRLeft = new(actionMapInteractorVRLeft.FindAction("HandheldClick"));
            ScrollInput scrollTickUpVRLeft = new(actionMapInteractorVRLeft.FindAction("ScrollValue"), MIN_SCROLL_THRESHOLD_VR, MAX_SCROLL_THRESHOLD_VR, MIN_SCROLL_TICKS_PER_SECOND_VR, MAX_SCROLL_TICKS_PER_SECOND_VR, true);
            ScrollInput scrollTickDownVRLeft = new(actionMapInteractorVRLeft.FindAction("ScrollValue"), MIN_SCROLL_THRESHOLD_VR, MAX_SCROLL_THRESHOLD_VR, MIN_SCROLL_TICKS_PER_SECOND_VR, MAX_SCROLL_TICKS_PER_SECOND_VR, false);

            // VR Left Drag Locomotor Action Map
            InputActionMap actionMapDragVRLeft = inputActionAsset.FindActionMap("InputDragVRLeft");
            PressableInput horizontalDragVRLeft = new(actionMapDragVRLeft.FindAction("HorizontalDrag"));
            PressableInput verticalDragVRLeft = new(actionMapDragVRLeft.FindAction("VerticalDrag"));

            // VR Right Hand Action Map
            InputActionMap actionMapHandVRRight = inputActionAsset.FindActionMap("InputHandVRRight");
            ValueInput<Vector3> handVRRightPosition = new(actionMapHandVRRight.FindAction("HandPosition"));
            ValueInput<Quaternion> handVRRightRotation = new(actionMapHandVRRight.FindAction("HandRotation"));

            // VR Right Interactor Action Map
            InputActionMap actionMapInteractorVRRight = inputActionAsset.FindActionMap("InputInteractorVRRight");
            PressableInput rangedClickVRRight = new(actionMapInteractorVRRight.FindAction("RangedClick"));
            PressableInput grabVRRight = new(actionMapInteractorVRRight.FindAction("Grab"));
            PressableInput handheldClickVRRight = new(actionMapInteractorVRRight.FindAction("HandheldClick"));
            ScrollInput scrollTickUpVRRight = new(actionMapInteractorVRRight.FindAction("ScrollValue"), MIN_SCROLL_THRESHOLD_VR, MAX_SCROLL_THRESHOLD_VR, MIN_SCROLL_TICKS_PER_SECOND_VR, MAX_SCROLL_TICKS_PER_SECOND_VR, true);
            ScrollInput scrollTickDownVRRight = new(actionMapInteractorVRRight.FindAction("ScrollValue"), MIN_SCROLL_THRESHOLD_VR, MAX_SCROLL_THRESHOLD_VR, MIN_SCROLL_TICKS_PER_SECOND_VR, MAX_SCROLL_TICKS_PER_SECOND_VR, false);

            // VR Right Drag Locomotor Action Map
            InputActionMap actionMapDragVRRight = inputActionAsset.FindActionMap("InputDragVRRight");
            PressableInput horizontalDragVRRight = new(actionMapDragVRRight.FindAction("HorizontalDrag"));
            PressableInput verticalDragVRRight = new(actionMapDragVRRight.FindAction("VerticalDrag"));

            // UI Action Map 
            InputActionMap actionMapUI = inputActionAsset.FindActionMap("InputUI");
            ToggleMenu = new PressableInput(actionMapUI.FindAction("ToggleMenu"));

            // VR Stick Press Left Action Map
            InputActionMap actionMapStickPressVRLeft = inputActionAsset.FindActionMap("StickPressVRLeft");
            StickPressInput stickPressHorizontalLeftDirectionVRLeft = new(actionMapStickPressVRLeft.FindAction("StickPress"), MIN_STICKPRESS_THRESHOLD, true, true);
            StickPressInput stickPressHorizontalRightDirectionVRLeft = new(actionMapStickPressVRLeft.FindAction("StickPress"), MIN_STICKPRESS_THRESHOLD, true, false);
            TeleportInput stickPressVerticalVRLeft = new(actionMapStickPressVRLeft.FindAction("StickPress"), MIN_TELEPORT_STICKPRESS_THRESHOLD, MAX_TELEPORT_NEUTRAL_THRESHOLD);

            // VR Stick Press Right Action Map
            InputActionMap actionMapStickPressVRRight = inputActionAsset.FindActionMap("StickPressVRRight");
            StickPressInput stickPressHorizontalLeftDirectionVRRight = new(actionMapStickPressVRRight.FindAction("StickPress"), MIN_STICKPRESS_THRESHOLD, true, true);
            StickPressInput stickPressHorizontalRightDirectionVRRight = new(actionMapStickPressVRRight.FindAction("StickPress"), MIN_STICKPRESS_THRESHOLD, true, false);
            TeleportInput stickPressVerticalVRRight = new(actionMapStickPressVRRight.FindAction("StickPress"), MIN_TELEPORT_STICKPRESS_THRESHOLD, MAX_TELEPORT_NEUTRAL_THRESHOLD);
            // Initialize the PlayerInputContainer
            PlayerInputContainer = new(
                changeMode2D: changeMode2D,
                inspectModeButton: inspectModeButton,
                rangedClick2D: rangedClick2D,
                grab2D: grab2D,
                handheldClick2D: handheldClick2D,
                scrollTickUp2D: scrollTickUp2D,
                scrollTickDown2D: scrollTickDown2D,
                resetViewVR: resetViewVR,
                handVRLeftPosition: handVRLeftPosition,
                handVRLeftRotation: handVRLeftRotation,
                rangedClickVRLeft: rangedClickVRLeft,
                grabVRLeft: grabVRLeft,
                handheldClickVRLeft: handheldClickVRLeft,
                scrollTickUpVRLeft: scrollTickUpVRLeft,
                scrollTickDownVRLeft: scrollTickDownVRLeft,
                horizontalDragVRLeft: horizontalDragVRLeft,
                verticalDragVRLeft: verticalDragVRLeft,
                handVRRightPosition: handVRRightPosition,
                handVRRightRotation: handVRRightRotation,
                rangedClickVRRight: rangedClickVRRight,
                grabVRRight: grabVRRight,
                handheldClickVRRight: handheldClickVRRight,
                scrollTickUpVRRight: scrollTickUpVRRight,
                scrollTickDownVRRight: scrollTickDownVRRight,
                horizontalDragVRRight: horizontalDragVRRight,
                verticalDragVRRight: verticalDragVRRight,
                stickPressHorizontalLeftDirectionVRLeft: stickPressHorizontalLeftDirectionVRLeft,
                stickPressHorizontalRightDirectionVRLeft: stickPressHorizontalRightDirectionVRLeft,
                stickPressHorizontalLeftDirectionVRRight: stickPressHorizontalLeftDirectionVRRight,
                stickPressHorizontalRightDirectionVRRight: stickPressHorizontalRightDirectionVRRight,
                stickPressVerticalVRLeft: stickPressVerticalVRLeft,
                teleportDirectionVRLeft: stickPressVerticalVRLeft,
                stickPressVerticalVRRight: stickPressVerticalVRRight,
                teleportDirectionVRRight: stickPressVerticalVRRight
            );

            _scrollInputs = new List<ScrollInput> { scrollTickUp2D, scrollTickDown2D, scrollTickUpVRLeft, scrollTickDownVRLeft, scrollTickUpVRRight, scrollTickDownVRRight };
            _stickPressInputs = new List<StickPressInput> { stickPressHorizontalLeftDirectionVRLeft, stickPressHorizontalRightDirectionVRLeft, stickPressHorizontalLeftDirectionVRRight, stickPressHorizontalRightDirectionVRRight };
            _teleportInputs = new List<TeleportInput> { stickPressVerticalVRLeft, stickPressVerticalVRRight };    

        }

        private void Update()
        {
            foreach (ScrollInput scrollInput in _scrollInputs)
                scrollInput.HandleUpdate();

            foreach (StickPressInput stickPressInput in _stickPressInputs)
                stickPressInput.HandleUpdate();

            foreach (TeleportInput teleportInput in _teleportInputs)
                teleportInput.HandleUpdate();
        }
    }
}

