using UnityEngine;

namespace VE2.Core.Player.Internal
{
    //[CreateAssetMenu(fileName = "Player2DMovementConfig", menuName = "Scriptable Objects/Player2DMovementConfig")]
    public class Player2DMovementConfig : ScriptableObject
    {
        [SerializeField] public float walkSpeed = 5f;
        [SerializeField] public float sprintSpeedMultiplier = 1.4f;
        [SerializeField] public float mouseSensitivity = 0.3f;
        [SerializeField] public float jumpForce = 5f;
        [SerializeField] public float crouchHeight = 0.7f;
        [SerializeField] public float minVerticalAngle = -90f; // Minimum vertical angle (looking down)
        [SerializeField] public float maxVerticalAngle = 90f;  // Maximum vertical angle (looking up)
    }
}
