using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VE2.Core.Common
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
        public event Action<bool> OnStickPressed; //True if positive, false if negative
        public event Action<bool> OnStickReleased; //True if positive, false if negative
    }

    public class StickPressInput : IStickPressInput
    {
        public event Action<bool> OnStickPressed;
        public event Action<bool> OnStickReleased;

        private readonly InputAction _inputAction;
        private float _minThreshold;
        private bool _isHorizontalStickPress;
        private bool _wasPressed;

        public StickPressInput(InputAction inputAction, float minThreshold, bool isHorizontalStickPress)
        {
            _inputAction = inputAction;
            _inputAction.Enable();
            _minThreshold = minThreshold;
            _isHorizontalStickPress = isHorizontalStickPress;
            _wasPressed = false;
        }

        public void HandleUpdate()
        {
            float inputValue;

            if (_isHorizontalStickPress)
            {
                inputValue = _inputAction.ReadValue<Vector2>().x;
                if (inputValue < -_minThreshold || inputValue > _minThreshold)
                {
                    if (!_wasPressed)
                    {
                        OnStickPressed?.Invoke(inputValue > 0);
                        _wasPressed = true;
                    }
                }
                else
                {
                    if (_wasPressed)
                    {
                        OnStickReleased?.Invoke(inputValue > 0);
                        _wasPressed = false;
                    }
                }
            }
            else
            {
                inputValue = _inputAction.ReadValue<Vector2>().y;
                if (inputValue > _minThreshold)
                {
                    if (!_wasPressed)
                    {
                        OnStickPressed?.Invoke(inputValue > 0);
                        _wasPressed = true;
                    }
                }
                else
                {
                    if (_wasPressed)
                    {
                        OnStickReleased?.Invoke(inputValue > 0);
                        _wasPressed = false;
                    }
                }
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
            IStickPressInput stickPressHorizontalVRLeft, IStickPressInput stickPressVerticalVRLeft,
            IStickPressInput stickPressHorizontalVRRight, IStickPressInput stickPressVerticalVRRight)
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
                    new SnapTurnInputContainer(stickPressHorizontalVRLeft)),
                new HandVRInputContainer(handVRRightPosition, handVRRightRotation, 
                    new InteractorInputContainer(rangedClickVRRight, grabVRRight, handheldClickVRRight, scrollTickUpVRRight, scrollTickDownVRRight),
                    new DragLocomotorInputContainer(horizontalDragVRRight, verticalDragVRRight),
                    new SnapTurnInputContainer(stickPressHorizontalVRRight))
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

        public HandVRInputContainer(IValueInput<Vector3> handPosition, IValueInput<Quaternion> handRotation, InteractorInputContainer interactorVRInputContainer, DragLocomotorInputContainer dragLocomotorInputContainer = null, SnapTurnInputContainer snapTurnInputContainer = null)
        {
            HandPosition = handPosition;
            HandRotation = handRotation;
            InteractorVRInputContainer = interactorVRInputContainer;
            DragLocomotorInputContainer = dragLocomotorInputContainer;
            SnapTurnInputContainer = snapTurnInputContainer;
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
        public IStickPressInput SnapTurn { get; private set; }

        public SnapTurnInputContainer(IStickPressInput snapTurn)
        {
            SnapTurn = snapTurn;
        }
    }
    public class TeleportInputContainer
    {
        public IStickPressInput Teleport { get; private set; }

        public TeleportInputContainer(IStickPressInput teleport)
        {
            Teleport = teleport;
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

        //Minimum threshold to detext thumbstick movement to process stick press input
        private const float MIN_STICKPRESS_THRESHOLD = 0.7f;
        private List<StickPressInput> _stickPressInputs;
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
            StickPressInput stickPressHorizontalVRLeft = new(actionMapStickPressVRLeft.FindAction("StickPress"), MIN_STICKPRESS_THRESHOLD, true);
            StickPressInput stickPressVerticalVRLeft = new(actionMapStickPressVRLeft.FindAction("StickPress"), MIN_STICKPRESS_THRESHOLD, false);

            // VR Stick Press Right Action Map
            InputActionMap actionMapStickPressVRRight = inputActionAsset.FindActionMap("StickPressVRRight");
            StickPressInput stickPressHorizontalVRRight = new(actionMapStickPressVRRight.FindAction("StickPress"), MIN_STICKPRESS_THRESHOLD, true);
            StickPressInput stickPressVerticalVRRight = new(actionMapStickPressVRLeft.FindAction("StickPress"), MIN_STICKPRESS_THRESHOLD, false);

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
                stickPressHorizontalVRLeft: stickPressHorizontalVRLeft,
                stickPressVerticalVRLeft: stickPressVerticalVRLeft,
                stickPressHorizontalVRRight: stickPressHorizontalVRRight,
                stickPressVerticalVRRight: stickPressVerticalVRRight
            );

            _scrollInputs = new List<ScrollInput> { scrollTickUp2D, scrollTickDown2D, scrollTickUpVRLeft, scrollTickDownVRLeft, scrollTickUpVRRight, scrollTickDownVRRight };
            _stickPressInputs = new List<StickPressInput> { stickPressHorizontalVRLeft, stickPressVerticalVRLeft, stickPressHorizontalVRRight, stickPressVerticalVRRight };
        }

        private void Update()
        {
            foreach (ScrollInput scrollInput in _scrollInputs)
                scrollInput.HandleUpdate();

            foreach (StickPressInput stickPressInput in _stickPressInputs)
                stickPressInput.HandleUpdate();
        }
    }
}

