using UnityEngine;
using VE2.Core.Common;
using VE2.Core.Player;

public class DragLocomotor
{
    private Vector3 _previousHandPosition;

    private readonly GameObject _iconHolder; //Entire icon
    private readonly GameObject _horizontalMoveIndicator;
    private readonly GameObject _verticalMoveIndicator;
    private readonly GameObject _sphereIcon;

    private readonly DragLocomotorInputContainer _inputContainer;
    private readonly Transform _rootTransform; //For horizontal drag
    private readonly Transform _headOffsetTransform; //For vertical drag
    private readonly Transform _handTransform; //For measuring drag delta 

    public DragLocomotor(LocomotorVRReferences locomotorVRReferences, DragLocomotorInputContainer inputContainer,
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
    }

    public void HandleOEnable()
    {
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

    //Icons can be copied from the ViRSE locomotor, dump them in the HandVRLeft prefab and wire up the references
    //Should show the sphere icon when both buttons are pressed

    private void HandleHorizontalDragPressed()
    {
        //Show horizontal icon
        Debug.Log("Horizontal drag pressed");
    }

    private void HandleHorizontalDragReleased()
    {
        //Hide horizontal icon
        Debug.Log("Horizontal drag released");
    }

    private void HandleVerticalDragPressed()
    {
        //Show vertical icon
        Debug.Log("Vertical drag pressed");
    }

    private void HandleVerticalDragReleased()
    {
        //Hide vertical icon
        Debug.Log("Vertical drag released");
    }
}
