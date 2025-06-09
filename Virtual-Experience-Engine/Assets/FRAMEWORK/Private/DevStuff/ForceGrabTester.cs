using UnityEngine;
using UnityEngine.InputSystem;
using VE2.Core.VComponents.API;

public class ForceGrabTester : MonoBehaviour
{
    [SerializeField] private GameObject Grabbable;
    private IV_FreeGrabbable _grabbable => Grabbable.GetComponent<IV_FreeGrabbable>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            Debug.Log($"Try local grab locked: {_grabbable.TryLocalGrab(true)}");
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            Debug.Log($"Try local grab unlocked: {_grabbable.TryLocalGrab(false)}");
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            Debug.Log($"Force local grab locked");
            _grabbable.ForceLocalGrab(true);
        }
        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            Debug.Log($"Force local grab unlocked");
            _grabbable.ForceLocalGrab(false);
        }
        if (Keyboard.current.digit5Key.wasPressedThisFrame)
            _grabbable.ForceLocalDrop();
        if (Keyboard.current.digit6Key.wasPressedThisFrame)
            _grabbable.UnlockLocalGrab();
    }
}
