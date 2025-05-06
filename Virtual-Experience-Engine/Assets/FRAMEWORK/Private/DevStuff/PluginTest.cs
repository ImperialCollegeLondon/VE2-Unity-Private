using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using VE2.Core.VComponents.API;
using VE2.NonCore.Instancing.API;

public class PluginTest : MonoBehaviour
{
    [SerializeField] private GameObject _lightOn;
    [SerializeField] private GameObject _lightOff;
    [SerializeField] private GameObject _pushButtonGO;
    [SerializeField] private GameObject _freeGrabbableGO;
    [SerializeField] private GameObject _handheldActivatableGO;
    [SerializeField] private GameObject _handheldAdjustableGO;
    [SerializeField] private GameObject _networkObjectGO;

    [SerializeField] private GameObject _linearAdjustableGO;
    [SerializeField] private GameObject _rotationalAdjustableGO;

    [SerializeField] private GameObject _rbSyncableGO;

    private IV_ToggleActivatable _pushActivatable => _pushButtonGO.GetComponent<IV_ToggleActivatable>();
    private IV_FreeGrabbable _freeGrabbable => _freeGrabbableGO.GetComponent<IV_FreeGrabbable>();
    private IV_HandheldActivatable _handheldActivatable => _handheldActivatableGO.GetComponent<IV_HandheldActivatable>();
    private IV_HandheldAdjustable _handheldAdjustable => _handheldAdjustableGO.GetComponent<IV_HandheldAdjustable>();
    private IV_NetworkObject _networkObject => _networkObjectGO.GetComponent<IV_NetworkObject>();
    private IV_RigidbodySyncable _rbSyncable => _rbSyncableGO.GetComponent<IV_RigidbodySyncable>();

    private IV_LinearAdjustable _linearAdjustable => _linearAdjustableGO.GetComponent<IV_LinearAdjustable>();
    private IV_RotationalAdjustable _rotationalAdjustable => _rotationalAdjustableGO.GetComponent<IV_RotationalAdjustable>();

    private int _counter = 0;

    // Start is called before the first frame update
    void Start()
    {
        _pushActivatable?.OnActivate.AddListener(OnButtonActivate);
        _pushActivatable?.OnDeactivate.AddListener(OnButtonDeactivate);

        _freeGrabbable?.OnGrab.AddListener(OnFreeGrabbableGrab);
        _freeGrabbable?.OnDrop.AddListener(OnFreeGrabbableDrop);

        _linearAdjustable?.OnGrab.AddListener(OnLinearAdjustableGrab);
        _linearAdjustable?.OnDrop.AddListener(OnLinearAdjustableDrop);

        _handheldActivatable?.OnActivate.AddListener(OnHandheldActivatableActivate);
        _handheldActivatable?.OnDeactivate.AddListener(OnHandheldActivatableDeactivate);
        _handheldAdjustable?.OnValueAdjusted.AddListener(OnHandheldAdjustableValueAdjusted);

        _linearAdjustable?.OnValueAdjusted.AddListener(OnLinearAdjustableValueAdjusted);
        _rotationalAdjustable?.OnValueAdjusted.AddListener(OnRotationalAdjustableValueAdjusted);

        _networkObject?.OnStateChange.AddListener(HandleNetworkObjectStateChange);
    }

    public void OnButtonActivate()
    {
        ushort clientID = _pushActivatable.MostRecentInteractingClientID.Value;
        Debug.Log("Button activated! ");
        Debug.Log($"Button state = {_pushActivatable.IsActivated}");

        if (clientID != ushort.MaxValue) 
            Debug.Log($"Activate by... {clientID.ToString()}");

        _lightOn.SetActive(true);
        _lightOff.SetActive(false);
        //HandleNewColor();
    }

    public void OnButtonDeactivate()
    {
        Debug.Log("Button deactivated!");
        Debug.Log($"Button state = {_pushActivatable.IsActivated}");

        _lightOn.SetActive(false);
        _lightOff.SetActive(true);
    }

    public void OnFreeGrabbableGrab()
    {
        Debug.Log("Free Grabbable grabbed!");
        Debug.Log($"Free Grabbable State = {_freeGrabbable.IsGrabbed}");
    }

    public void OnFreeGrabbableDrop()
    {
        Debug.Log("Free Grabbable dropped!");
        Debug.Log($"Free Grabbable State = {_freeGrabbable.IsGrabbed}");
    }


    public void OnLinearAdjustableGrab()
    {
        Debug.Log("Adjustable grabbed!");
        Debug.Log($"Adjustable State = {_linearAdjustable.IsGrabbed}");
    }

    public void OnLinearAdjustableDrop()
    {
        Debug.Log("Adjustable dropped!");
        Debug.Log($"Adjustable State = {_linearAdjustable.IsGrabbed}");
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            _pushActivatable.SetActivated(!_pushActivatable.IsActivated);
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            _pushActivatable.InteractRange = 0;
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            _counter++;
            _networkObject.NetworkObject = _counter;
        }
        else if (Keyboard.current.digit4Key.wasPressedThisFrame)
            _handheldActivatable.SetActivated(!_pushActivatable.IsActivated);
        else if(Keyboard.current.digit5Key.wasPressedThisFrame)
            _handheldAdjustable.SetValue(_handheldAdjustable.Value - 1);
        else if (Keyboard.current.digit6Key.wasPressedThisFrame)
            _handheldAdjustable.SetValue(_handheldAdjustable.Value + 1);
        else if (Keyboard.current.digit7Key.wasPressedThisFrame)
        {
            _rbSyncableGO.GetComponent<Rigidbody>().AddForce(Quaternion.Euler(Random.Range(-30, 30), 0, 0)*Vector3.up*10, ForceMode.Impulse);
            _rbSyncableGO.GetComponent<Rigidbody>().AddTorque(Random.Range(-30, 30) * Vector3.right);
        }
            

        else if(Keyboard.current.digit7Key.wasPressedThisFrame)
        {
            _linearAdjustable.SetValue(Random.Range(_linearAdjustable.MinimumOutputValue, _linearAdjustable.MaximumOutputValue));
        }
        else if(Keyboard.current.digit8Key.wasPressedThisFrame)
        {
            _rotationalAdjustable.SetValue(Random.Range(_rotationalAdjustable.MinimumOutputValue, _rotationalAdjustable.MaximumOutputValue));
        }
    }

    private void OnHandheldActivatableActivate()
    {
        Debug.Log("Handheld Activatable activated!");
        Debug.Log($"Handheld Activatable State = {_handheldActivatable.IsActivated}");
    }

    private void OnHandheldActivatableDeactivate()
    {
        Debug.Log("Handheld Activatable deactivated!");
        Debug.Log($"Handheld Activatable State = {_handheldActivatable.IsActivated}");
    }

    private void OnHandheldAdjustableValueAdjusted(float value)
    {
        Debug.Log("Handheld Adjustable Adjusted!");
        Debug.Log($"Handheld Adjustable Value = {_handheldAdjustable.Value}");
    }
    private void OnLinearAdjustableValueAdjusted(float value)
    {
        Debug.Log("Linear Adjustable Adjusted!");
        Debug.Log($"Linear Adjustable Value = {_linearAdjustable.Value}");
        Debug.Log($"Linear Adjustable Output Value = {_linearAdjustable.SpatialValue}");
    }

    private void OnRotationalAdjustableValueAdjusted(float value)
    {
        Debug.Log("Rotational Adjustable Adjusted!");
        Debug.Log($"Rotational Adjustable Value = {_rotationalAdjustable.Value}");
        Debug.Log($"Rotational Adjustable Output Value = {_rotationalAdjustable.SpatialValue}");
    }

    private void HandleNetworkObjectStateChange(object data)
    {
        _counter = (int)data;
        Debug.Log(_counter);
    }
}
