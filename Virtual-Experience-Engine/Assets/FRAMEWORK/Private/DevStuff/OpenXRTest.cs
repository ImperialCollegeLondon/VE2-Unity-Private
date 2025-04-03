using UnityEngine;
using UnityEngine.InputSystem;

public class OpenXRTest : MonoBehaviour
{
    InputActionMap actionMapLeftHand;
    InputActionMap actionMapRightHand;

    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;

    void Start()
    {
        InputActionAsset inputActionAsset = Resources.Load<InputActionAsset>("V_InputActions");
        actionMapLeftHand = inputActionAsset.FindActionMap("InputHandVRLeft");
        actionMapRightHand = inputActionAsset.FindActionMap("InputHandVRRight");

        actionMapLeftHand.Enable();
        actionMapRightHand.Enable();

    }

    void Update()
    {
        leftHand.localPosition = actionMapLeftHand["HandPosition"].ReadValue<Vector3>();
        leftHand.localRotation = actionMapLeftHand["HandRotation"].ReadValue<Quaternion>();

        rightHand.localPosition = actionMapRightHand["HandPosition"].ReadValue<Vector3>();
        rightHand.localRotation = actionMapRightHand["HandRotation"].ReadValue<Quaternion>();
    }
}
