using UnityEngine;
using UnityEngine.InputSystem;
using VE2.Common.API;

public class test : MonoBehaviour
{
    void Update()
    {
        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            Debug.Log("Toggling FreeFlyMode on");
            VE2API.Player.ToggleFreeFlyMode(true);
        }
        else if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            Debug.Log("Toggling FreeFlyMode off");
            VE2API.Player.ToggleFreeFlyMode(false);
        }
    }
}