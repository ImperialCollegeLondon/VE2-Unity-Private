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
            _grabbable.TryLocalGrab(true);
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
            _grabbable.TryLocalGrab(false);
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
            _grabbable.ForceLocalDrop();
        if (Keyboard.current.digit4Key.wasPressedThisFrame)
            _grabbable.UnlockLocalGrab();
    }
}
