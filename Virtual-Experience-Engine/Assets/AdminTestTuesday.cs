using UnityEngine;
using UnityEngine.InputSystem;
using VE2.Common.API;

public class AdminTestTuesday : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            if (!VE2API.PlatformService.IsLocalPlayerAdmin)
                VE2API.PlatformService.GrantLocalPlayerAdmin();
            else
                VE2API.PlatformService.RevokeLocalPlayerAdmin();
        }
    }
}
