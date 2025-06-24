using UnityEngine;
using UnityEngine.InputSystem;
using VE2.Common.API;

public class TestAPI : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        VE2API.Player.OnTeleport.AddListener(CallOnTeleport);
        VE2API.Player.OnSnapTurn.AddListener(CallOnSnapTurn);
        VE2API.Player.OnHorizontalDrag.AddListener(CallOnHorizontalDrag);
        VE2API.Player.OnVerticalDrag.AddListener(CallOnVerticalDrag);
        VE2API.Player.OnJump2D.AddListener(CallOnJump2D);
        VE2API.Player.OnCrouch2D.AddListener(CallOnCrouch2D);
        VE2API.Player.OnChangeToVRMode.AddListener(CallOnChangeToVRMode);
        VE2API.Player.OnChangeTo2DMode.AddListener(CallOnChangeTo2DMode);
        VE2API.Player.OnResetViewVR.AddListener(CallOnResetViewVR);
        VE2API.PrimaryUIService.OnUIShow.AddListener(OnActivateMainMenu);
        VE2API.PrimaryUIService.OnUIHide.AddListener(OnDeactivateMainMenu);

    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            GetPlayerPosition();
        }
        else if (Keyboard.current.oKey.wasPressedThisFrame)
        {
            SetPlayerPosition(new Vector3(0, 5, 0));
        }
    }

    public void CallOnTeleport()
    {
        Debug.Log("OnTeleport called");
    }

    public void CallOnSnapTurn()
    {
        Debug.Log($"OnSnapTurn called");
    }

    public void CallOnHorizontalDrag()
    {
        Debug.Log("OnHorizontalDrag called");
    }

    public void CallOnVerticalDrag()
    {
        Debug.Log("OnVerticalDrag called");
    }

    public void CallOnJump2D()
    {
        Debug.Log("OnJump2D called");
    }

    public void CallOnCrouch2D()
    {
        Debug.Log("OnCrouch2D called");
    }
    public void CallOnChangeToVRMode()
    {
        Debug.Log("OnChangeToVRMode called");
    }
    public void CallOnChangeTo2DMode()
    {
        Debug.Log("OnChangeTo2DMode called");
    }
    public void CallOnResetViewVR()
    {
        Debug.Log("OnResetViewVR called");
    }

    public void SetPlayerPosition(Vector3 position)
    {
        VE2API.Player.SetPlayerPosition(position);
        Debug.Log($"Set player position to: {position}");
    }

    public Vector3 GetPlayerPosition()
    {
        Vector3 position = VE2API.Player.PlayerPosition;
        Debug.Log($"Get player position: {position}");
        return position;
    }

    public void OnActivateMainMenu()
    {
        Debug.Log("Main Menu Activated");
    }

    public void OnDeactivateMainMenu()
    {
        Debug.Log("Main Menu Deactivated");
    }
}
