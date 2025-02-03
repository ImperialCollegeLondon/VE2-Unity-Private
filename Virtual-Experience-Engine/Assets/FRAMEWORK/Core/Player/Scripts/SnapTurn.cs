using System;
using UnityEngine;
using VE2.Core.Common;
using VE2.Core.Player;
public class SnapTurn
{   
    private float _snapTurnAmount = 45.0f;

    private readonly SnapTurnInputContainer _inputContainer;
    private readonly Transform _rootTransform; //For rotating the player
    public SnapTurn(SnapTurnInputContainer inputContainer, Transform rootTransform)
    {
        _inputContainer = inputContainer;
        _rootTransform = rootTransform;
    }
    public void HandleUpdate()
    {

    }
    public void HandleOEnable()
    {
        _inputContainer.SnapTurnLeft.OnStickPressed += HandleSnapTurnLeft;
        _inputContainer.SnapTurnRight.OnStickPressed += HandleSnapTurnRight;
    }

    public void HandleOnDisable()
    {
        _inputContainer.SnapTurnLeft.OnStickPressed -= HandleSnapTurnLeft;
        _inputContainer.SnapTurnRight.OnStickPressed -= HandleSnapTurnRight;
    }

    private void HandleSnapTurnLeft()
    {
        // Rotate the player by 45 degrees
        _rootTransform.rotation *= Quaternion.Euler(0, -_snapTurnAmount, 0);
        Debug.Log("Snap Turn Left");
    }
    private void HandleSnapTurnRight()
    {
        // Rotate the player by 45 degrees
        _rootTransform.rotation *= Quaternion.Euler(0, _snapTurnAmount, 0);
        Debug.Log("Snap Turn Right");
    }
}
