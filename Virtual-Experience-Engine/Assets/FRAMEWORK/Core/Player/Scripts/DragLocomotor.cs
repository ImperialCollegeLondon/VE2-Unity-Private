using UnityEngine;
using VE2.Core.Common;
using VE2.Core.Player;

public class DragLocomotor
{   
    private DragLocomotor _otherDragLocomotor;

    private Vector3 _previousHandPosition;
    protected float _dragSpeed = 2.0f;
    private bool _isDraggingHorizontal = false;
    private bool _isDraggingVertical = false;
    
    private readonly GameObject _iconHolder; //Entire icon
    private readonly GameObject _horizontalMoveIndicator;
    private readonly GameObject _verticalMoveIndicator;
    private readonly GameObject _sphereIcon;

    private readonly DragLocomotorInputContainer _inputContainer;
    private readonly Transform _rootTransform; //For horizontal drag
    private readonly Transform _headOffsetTransform; //For vertical drag
    private readonly Transform _handTransform; //For measuring drag delta 



    public DragLocomotor(DragLocomotorReferences locomotorVRReferences, DragLocomotorInputContainer inputContainer,
        Transform rootTransform, Transform headOffsetTransform, Transform handTransform)
    {
        _iconHolder = locomotorVRReferences.DragIconHolder;
        _horizontalMoveIndicator = locomotorVRReferences.HorizontalDragIndicator;
        _verticalMoveIndicator = locomotorVRReferences.VerticalDragIndicator;
        _sphereIcon = locomotorVRReferences.SphereDragIcon;

        _inputContainer = inputContainer;

        _rootTransform = rootTransform;
        _headOffsetTransform = headOffsetTransform;
        _handTransform = handTransform;
    }

    public void HandleUpdate()
    {
        /*
            if horizontal input is pressed, move the player based on the change in position of the handTransform

        */
        Vector3 horizontalDragDirection = new Vector3(_previousHandPosition.x - _handTransform.position.x, 0, _previousHandPosition.z - _handTransform.position.z);
        if (_inputContainer.HorizontalDrag.IsPressed)
            HandleHorizontalDragMovement(horizontalDragDirection);
        else 
            _isDraggingHorizontal = false;

        Vector3 verticalDragDirection = new Vector3(0, _previousHandPosition.y - _handTransform.position.y, 0);
        if(_inputContainer.VerticalDrag.IsPressed)
            HandleVerticalDragMovement(verticalDragDirection);
        else
            _isDraggingVertical = false;


        HandleSphereIconVisibility();

        _previousHandPosition = _handTransform.position; 
    }

    public void HandleOEnable()
    {
        HandleDragIconVisibilityWhenEnabled();
        _inputContainer.HorizontalDrag.OnPressed += HandleHorizontalDragPressed;
        _inputContainer.HorizontalDrag.OnReleased += HandleHorizontalDragReleased;
        _inputContainer.VerticalDrag.OnPressed += HandleVerticalDragPressed;
        _inputContainer.VerticalDrag.OnReleased += HandleVerticalDragReleased;

    }

    public void HandleOnDisable()
    {
        _inputContainer.HorizontalDrag.OnPressed -= HandleHorizontalDragPressed;
        _inputContainer.HorizontalDrag.OnReleased -= HandleHorizontalDragReleased;
        _inputContainer.VerticalDrag.OnPressed -= HandleVerticalDragPressed;
        _inputContainer.VerticalDrag.OnReleased -= HandleVerticalDragReleased;
    }

    public void HandleOtherDragLocomotor(DragLocomotor otherDragLocomotor)
    {
        _otherDragLocomotor = otherDragLocomotor;
    }

    //Icons can be copied from the ViRSE locomotor, dump them in the HandVRLeft prefab and wire up the references
    //Should show the sphere icon when both buttons are pressed

    private void HandleHorizontalDragPressed()
    {
        //Show horizontal icon
        //_horizontalMoveIndicator.SetActive(true);
        Debug.Log("Horizontal drag pressed");
    }

    private void HandleHorizontalDragReleased()
    {
        //Hide horizontal icon
        //_horizontalMoveIndicator.SetActive(false);
        Debug.Log("Horizontal drag released");
    }

    private void HandleVerticalDragPressed()
    {
        //Show vertical icon
        //_verticalMoveIndicator.SetActive(true);
        Debug.Log("Vertical drag pressed");
    }

    private void HandleVerticalDragReleased()
    {
        //Hide vertical icon
        //_verticalMoveIndicator.SetActive(false);
        Debug.Log("Vertical drag released");
    }

    private void HandleDragIconVisibilityWhenEnabled()
    {
        _horizontalMoveIndicator.SetActive(false);
        _verticalMoveIndicator.SetActive(false);
        _sphereIcon.SetActive(false);
    }

    private void HandleSphereIconVisibility()
    {
        _verticalMoveIndicator.SetActive(_isDraggingVertical);
        _horizontalMoveIndicator.SetActive(_isDraggingHorizontal);
        _sphereIcon.SetActive(_isDraggingHorizontal && _isDraggingVertical);
    }

    private void HandleHorizontalDragMovement(Vector3 dragVector)
    {
        if (_otherDragLocomotor._isDraggingHorizontal) return;

        if (!_isDraggingHorizontal)
        {
            _isDraggingHorizontal = true;
        }
        else
        {
            Vector3 moveVector = dragVector * _dragSpeed;
            _rootTransform.position += moveVector;
        }        
    }

    private void HandleVerticalDragMovement(Vector3 dragVector)
    {
        if (_otherDragLocomotor._isDraggingVertical) return;

        if (!_isDraggingVertical)
        {
            _isDraggingVertical = true;
        }
        else
        {
            Vector3 moveVector = dragVector * _dragSpeed;
            _headOffsetTransform.position += moveVector;
        }
    }
}
