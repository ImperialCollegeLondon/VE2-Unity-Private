using UnityEngine;
using VE2.Core.Common;
using VE2.Core.Player;

public class DragLocomotor
{   
    private DragLocomotorInputContainer _otherVRHandInputContainer;

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

        Vector3 horizontalDragDirection = new Vector3(_previousHandPosition.x - _handTransform.position.x, 0, _previousHandPosition.z - _handTransform.position.z);
        if (_inputContainer.HorizontalDrag.IsPressed)
            HandleHorizontalDragMovement(horizontalDragDirection);
        else
            _isDraggingHorizontal = _inputContainer.IsDraggingHorizontal = false;
            


        Vector3 verticalDragDirection = new Vector3(0, _previousHandPosition.y - _handTransform.position.y, 0);
        if(_inputContainer.VerticalDrag.IsPressed)
            HandleVerticalDragMovement(verticalDragDirection);
        else
            _isDraggingVertical = _inputContainer.IsDraggingVertical = false;

        _previousHandPosition = _handTransform.position; 
    }

    public void HandleOEnable()
    {
        HandleDragIconVisibilityWhenEnabled();
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
        if (_otherVRHandInputContainer.IsDraggingHorizontal) return;

        if (!_otherVRHandInputContainer.HorizontalDrag.IsPressed)
        {
            _horizontalMoveIndicator.SetActive(true);
            if (_verticalMoveIndicator.activeSelf)
            {
                _sphereIcon.SetActive(true);
            }            
        }       
        Debug.Log("Horizontal drag pressed");
    }

    private void HandleHorizontalDragReleased()
    {
        //Hide horizontal icon
        _horizontalMoveIndicator.SetActive(false);
        _sphereIcon.SetActive(false);
        Debug.Log("Horizontal drag released");
    }

    private void HandleVerticalDragPressed()
    {
        //Show vertical icon
        if (_otherVRHandInputContainer.IsDraggingVertical) return;

        if (!_otherVRHandInputContainer.HorizontalDrag.IsPressed)
        {
            _verticalMoveIndicator.SetActive(true);
            if (_horizontalMoveIndicator.activeSelf)
            {
                _sphereIcon.SetActive(true);
            }
        }
        Debug.Log("Vertical drag pressed");
    }

    private void HandleVerticalDragReleased()
    {
        //Hide vertical icon
        _verticalMoveIndicator.SetActive(false);
        _sphereIcon.SetActive(false);
        Debug.Log("Vertical drag released");
    }

    private void HandleDragIconVisibilityWhenEnabled()
    {
        _horizontalMoveIndicator.SetActive(false);
        _verticalMoveIndicator.SetActive(false);
        _sphereIcon.SetActive(false);
    }

    private void HandleOtherVRHorizontalDragReleased()
    {
        //Show horizontal icon
        if (!_inputContainer.HorizontalDrag.IsPressed) return;
        _horizontalMoveIndicator.SetActive(true);
        if (_verticalMoveIndicator.activeSelf)
        {
            _sphereIcon.SetActive(true);
        }
    }

    private void HandleOtherVRVerticalDragReleased()
    {
        //Show horizontal icon
        if (!_inputContainer.VerticalDrag.IsPressed) return;
        _verticalMoveIndicator.SetActive(true);
        if (_horizontalMoveIndicator.activeSelf)
        {
            _sphereIcon.SetActive(true);
        }
    }

    private void HandleHorizontalDragMovement(Vector3 dragVector)
    {
        if (_otherVRHandInputContainer.IsDraggingHorizontal) return;

        if (!_isDraggingHorizontal)
        {
            _isDraggingHorizontal = _inputContainer.IsDraggingHorizontal = true;
        }
        else
        {
            Vector3 moveVector = dragVector * _dragSpeed;
            Vector3 currentPosition = _rootTransform.position;
            Vector3 targetPosition = currentPosition + moveVector;

            // Perform raycast from current position
            if (Physics.Raycast(currentPosition, Vector3.down, out RaycastHit currentHit, Mathf.Infinity, groundLayerMask))
            {
                float currentGroundHeight = currentHit.point.y;

                // Perform raycast from target position
                if (Physics.Raycast(targetPosition, Vector3.down, out RaycastHit targetHit, Mathf.Infinity, groundLayerMask))
                {
                    float targetGroundHeight = targetHit.point.y;
                    float heightDifference = Mathf.Abs(targetGroundHeight - currentGroundHeight);

                    // Check if the height difference is within the allowable step size
                    if (heightDifference <= 0.5f)//TO DO: Make step size configurable
                    {
                        // Adjust vertical position to maintain consistent height above ground
                        targetPosition.y = targetGroundHeight + (currentPosition.y - currentGroundHeight);
                        _rootTransform.position = targetPosition;
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
    }

    //private void HandleVerticalDragMovement(Vector3 dragVector)
    //{
    //    if (_otherVRHandInputContainer.IsDraggingVertical) return;

    //    if (!_isDraggingVertical)
    //    {
    //        _isDraggingVertical = _inputContainer.IsDraggingVertical = true;
    //    }
    //    else
    //    {
    //        Vector3 moveVector = dragVector * _dragSpeed;
    //        _headOffsetTransform.position += moveVector;
    //    }
    //}

    private void HandleVerticalDragMovement(Vector3 dragVector)
    {
        if (_otherVRHandInputContainer.IsDraggingVertical) return;

        if (!_isDraggingVertical)
        {
            _isDraggingVertical = _inputContainer.IsDraggingVertical = true;
        }
        else
        {
            Vector3 moveVector = dragVector * _dragSpeed;
            Vector3 targetPosition = _headOffsetTransform.position + moveVector;

            // Perform raycast to check for collisions
            if (Physics.Raycast(_headOffsetTransform.position, moveVector.normalized, out RaycastHit hit, moveVector.magnitude + 0.5f))//TO DO: Maybe make this into a readonly variable?
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
