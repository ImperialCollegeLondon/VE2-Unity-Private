using UnityEngine;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;

//Lives on the target GameObject
public class TargetController : MonoBehaviour
{
    [SerializeField] private float _maxTargetPos;
    [SerializeField] private float _minTargetPos;

    [SerializeField] private InterfaceReference<IV_HoldActivatable> _buttonRight;
    [SerializeField] private InterfaceReference<IV_HoldActivatable> _buttonLeft;

    private void Update()
    {
        if (_buttonRight.Interface.IsActivated)
        {
            transform.position += Vector3.right * Time.deltaTime;
        }
        else if (_buttonLeft.Interface.IsActivated)
        {
            transform.position += Vector3.left * Time.deltaTime;
        }
    }

    public PingPongGameColor TargetColour = PingPongGameColor.Red;
    private Renderer _targetRenderer;

    private void Start()
    {
        _targetRenderer = GetComponent<Renderer>();
        _targetRenderer.material.color = Color.red;
    }

    public void HandleChangeColourButtonPressed()
    {
        //Increment the target colour
        TargetColour = (PingPongGameColor)(((int)TargetColour + 1) % 3);

        switch (TargetColour)
        {
            case PingPongGameColor.Red:
                _targetRenderer.material.color = Color.red;
                break;
            case PingPongGameColor.Green:
                _targetRenderer.material.color = Color.green;
                break;
            case PingPongGameColor.Blue:
                _targetRenderer.material.color = Color.blue;
                break;
        }
    }
}

//This is our older version, before cleaning up the code to use the InterfaceReference<IV_HoldButton> field instead 
// //Lives on the target GameObject
// public class TargetController : MonoBehaviour
// {
//     public float MaxTargetPos;
//     public float MinTargetPos;

//     private bool _isButtonRightPressed = false;
//     private bool _isButtonLeftPressed = false;

//     public PingPongGameColor TargetColour = PingPongGameColor.Red;
//     private Renderer _targetRenderer;

//     private void Start()
//     {
//         _targetRenderer = GetComponent<Renderer>();
//         _targetRenderer.material.color = Color.red;
//     }

//     //Linked to our right button's OnActivate event in inspector
//     public void HandleButtonRightPress()
//     {
//         Debug.Log("Right button pressed");
//         _isButtonRightPressed = true;
//     }

//     //Linked to our right button's OnDeactivate event in inspector
//     public void HandleButtonRightRelease()
//     {
//         Debug.Log("Right button released");
//         _isButtonRightPressed = false;
//     }

//     //Linked to our left button's OnActivate event in inspector
//     public void HandleButtonLeftPress()
//     {
//         Debug.Log("Left button pressed");
//         _isButtonLeftPressed = true;
//     }

//     //Linked to our left button's OnActivate event in inspector
//     public void HandleButtonLeftRelease()
//     {
//         Debug.Log("Left button released");
//         _isButtonLeftPressed = false;
//     }

//     private void Update()
//     {
//         if (_isButtonRightPressed)
//         {
//             transform.position += Vector3.right * Time.deltaTime;
//         }
//         else if (_isButtonLeftPressed)
//         {
//             transform.position += Vector3.left * Time.deltaTime;
//         }
//     }

//     public void HandleChangeColourButtonPressed()
    // {
    //     //Increment the target colour
    //     TargetColour = (PingPongGameColor)(((int)TargetColour + 1) % 3);

    //     switch (TargetColour)
    //     {
    //         case PingPongGameColor.Red:
    //             _targetRenderer.material.color = Color.red;
    //             break;
    //         case PingPongGameColor.Green:
    //             _targetRenderer.material.color = Color.green;
    //             break;
    //         case PingPongGameColor.Blue:
    //             _targetRenderer.material.color = Color.cyan;
    //             break;
    //     }
    // }
// }