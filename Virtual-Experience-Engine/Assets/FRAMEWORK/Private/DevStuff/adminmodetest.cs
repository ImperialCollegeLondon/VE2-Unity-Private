using UnityEngine;
using UnityEngine.InputSystem;
using VE2.Common.API;

public class adminmodetest : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            VE2API.PlatformService.GrantLocalPlayerAdmin();
            Debug.Log("Granted Local Player Admin");
        }
        if (Keyboard.current.digit5Key.wasPressedThisFrame)
        {
            VE2API.PlatformService.RevokeLocalPlayerAdmin();
            Debug.Log("Revoked Local Player Admin");
        }
    }
}
