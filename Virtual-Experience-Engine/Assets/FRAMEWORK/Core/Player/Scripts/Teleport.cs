using UnityEngine;
using VE2.Core.Common;
using VE2.Core.Player;
public class Teleport
{
    private readonly TeleportInputContainer _inputContainer;
    private readonly Transform _rootTransform; //For rotating the player
    private readonly Transform _teleportRaycastOrigin; //Position of the teleport raycast origin
    public Teleport(TeleportInputContainer inputContainer, Transform rootTransform)
    {
        _inputContainer = inputContainer;
        _rootTransform = rootTransform;
    }

    public void HandleOEnable()
    {
        _inputContainer.Teleport.OnStickPressed += HandleTeleportStart;
        _inputContainer.Teleport.OnStickReleased += HandleTeleportEnd;
    }

    public void HandleOnDisable()
    {
        _inputContainer.Teleport.OnStickPressed -= HandleTeleportStart;
        _inputContainer.Teleport.OnStickReleased -= HandleTeleportEnd;
    }

    private void HandleTeleportStart(bool isForwardMovement)
    {
        if (isForwardMovement)
        {
            //Teleport User
            Debug.Log("Teleport Activated");
        }
    }

    private void HandleTeleportEnd(bool isForwardMovement)
    {
        if (isForwardMovement)
        {
            //Teleport User 
            Debug.Log("Teleport Deactivated");
        }
    }
}
