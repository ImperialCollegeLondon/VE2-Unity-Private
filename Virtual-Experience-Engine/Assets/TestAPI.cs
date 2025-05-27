using UnityEngine;
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
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CallOnTeleport()
    {
        Debug.Log("OnTeleport called");
    }

    public void CallOnSnapTurn(string direction)
    {
        Debug.Log($"OnSnapTurn called with direction: {direction}");
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
}
