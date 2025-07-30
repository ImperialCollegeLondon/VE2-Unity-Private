using UnityEngine;
using UnityEngine.InputSystem;
using VE2.Core.VComponents.API;

public class ForceGrabTester : MonoBehaviour
{
    [SerializeField] private GameObject Grabbable1;
    [SerializeField] private GameObject Grabbable2;
    private IV_FreeGrabbable _grabbable1 => Grabbable1.GetComponent<IV_FreeGrabbable>();
    private IV_FreeGrabbable _grabbable2 => Grabbable2.GetComponent<IV_FreeGrabbable>();


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            Debug.Log($"Try local grab locked: {_grabbable1.TryLocalGrab(true)}");
        }
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            Debug.Log($"Try local grab unlocked: {_grabbable1.TryLocalGrab(false)}");
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            Debug.Log($"Force local grab locked");
            _grabbable1.ForceLocalGrab(true);
        }
        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            Debug.Log($"Force local grab unlocked");
            _grabbable1.ForceLocalGrab(false);
        }
        if (Keyboard.current.digit5Key.wasPressedThisFrame)
            _grabbable1.ForceLocalDrop();
        if (Keyboard.current.digit6Key.wasPressedThisFrame)
            _grabbable1.UnlockLocalGrab();

        if (Keyboard.current.digit7Key.wasPressedThisFrame)
        {
            _grabbable2.ForceLocalGrab(false, VRHandInteractorType.LeftHandVR);
        }
        if (Keyboard.current.digit8Key.wasPressedThisFrame)
        {
            Debug.Log($"Force local grab locked grabbable 2:");
            _grabbable2.ForceLocalGrab(false, VRHandInteractorType.RightHandVR);
        }

    }
}
