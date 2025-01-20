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

    public void HandleOEnable()
    {
        _inputContainer.SnapTurn.OnStickPressed += HandleSnapTurn;
    }

    public void HandleOnDisable()
    {
        _inputContainer.SnapTurn.OnStickPressed -= HandleSnapTurn;
    }

    private void HandleSnapTurn(bool isRightMovement)
    {
        if (isRightMovement)
        {
            // Rotate the player by 45 degrees
            _rootTransform.rotation *= Quaternion.Euler(0, _snapTurnAmount, 0);
            Debug.Log("Snap Turn Left");
        }
        else
        {
            // Rotate the player by 45 degrees
            _rootTransform.rotation *= Quaternion.Euler(0, -_snapTurnAmount, 0);
            Debug.Log("Snap Turn Right");
        }

    }
}
