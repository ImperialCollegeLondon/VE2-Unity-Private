using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;
using VE2.NonCore.Instancing.API;

public class PluginTest : MonoBehaviour
{
    [SerializeField] private GameObject _pushLightOn;
    [SerializeField] private GameObject _pushLightOff;
    [SerializeField] private GameObject _holdLightOn;
    [SerializeField] private GameObject _holdLightOff;
    [SerializeField] private GameObject _pressurePlateLightOff;
    [SerializeField] private GameObject _pressurePlateLightOn;
    [SerializeField] private GameObject _pushButtonGO;
    [SerializeField] private GameObject _holdButtonGO;
    [SerializeField] private GameObject _pressurePlateGO;
    [SerializeField] private GameObject _freeGrabbableGO;
    [SerializeField] private GameObject _handheldActivatableGO;
    [SerializeField] private GameObject _handheldAdjustableGO;
    [SerializeField] private GameObject _networkObjectGO;

    [SerializeField] private TMP_Text _roomColorText;


    private IV_ToggleActivatable _pushActivatable => _pushButtonGO.GetComponent<IV_ToggleActivatable>();
    private IV_HoldActivatable _holdActivatable => _holdButtonGO.GetComponent<IV_HoldActivatable>();
    private IV_PressurePlate _pressurePlate => _pressurePlateGO.GetComponent<IV_PressurePlate>();
    private IV_FreeGrabbable _freeGrabbable => _freeGrabbableGO.GetComponent<IV_FreeGrabbable>();
    private IV_HandheldActivatable _handheldActivatable => _handheldActivatableGO.GetComponent<IV_HandheldActivatable>();
    private IV_HandheldAdjustable _handheldAdjustable => _handheldAdjustableGO.GetComponent<IV_HandheldAdjustable>();
    private IV_NetworkObject _networkObject => _networkObjectGO.GetComponent<IV_NetworkObject>();

    private int _counter = 0;

    // Start is called before the first frame update
    void Start()
    {
        if (_roomColorText != null)
        {
            _roomColorText.text = "Green Room";
            _roomColorText.color = Color.green;
        }

        //_pushActivatable.OnActivate.AddListener(OnButtonActivate);
        //_pushActivatable.OnDeactivate.AddListener(OnButtonDeactivate);

        _freeGrabbable.OnGrab.AddListener(OnFreeGrabbableGrab);
        _freeGrabbable.OnDrop.AddListener(OnFreeGrabbableDrop);

        _handheldActivatable.OnActivate.AddListener(OnHandheldActivatableActivate);
        _handheldActivatable.OnDeactivate.AddListener(OnHandheldActivatableDeactivate);
        _handheldAdjustable.OnValueAdjusted.AddListener(OnHandheldAdjustableValueAdjusted);

        _networkObject.OnDataChange.AddListener(HandleNetworkObjectStateChange);

    }

    public void OnPushButtonActivate()
    {
        IClientIDWrapper clientID = _pushActivatable.MostRecentInteractingClientID;
        Debug.Log("Button activated! ");
        Debug.Log($"Button state = {_pushActivatable.IsActivated}");

        Debug.Log($"Activate by... {clientID.Value.ToString()}");

        _pushLightOn.SetActive(true);
        _pushLightOff.SetActive(false);
        //HandleNewColor();
    }

    public void OnPushButtonDeactivate()
    {
        Debug.Log("Button deactivated!");
        Debug.Log($"Button state = {_pushActivatable.IsActivated}");

        _pushLightOn.SetActive(false);
        _pushLightOff.SetActive(true);
    }

        public void OnHoldButtonActivate()
    {
        // ushort clientID = _holdActivatable.MostRecentInteractingClientID;
        // Debug.Log("Button activated! ");
        // Debug.Log($"Button state = {_holdActivatable.IsActivated}");

        // if (clientID != ushort.MaxValue) 
        //     Debug.Log($"Activate by... {clientID}");

        // Debug.Log($"Current Interacting Clients: {_holdActivatable.CurrentlyInteractingClientIDs.Count}");

        _holdLightOn.SetActive(true);
        _holdLightOff.SetActive(false);
        //HandleNewColor();
    }

    public void OnHoldButtonDeactivate()
    {
        // Debug.Log("Button deactivated!");
        // Debug.Log($"Button state = {_holdActivatable.IsActivated}");
        // Debug.Log($"Current Interacting Clients: {_holdActivatable.CurrentlyInteractingClientIDs.Count}");

        _holdLightOn.SetActive(false);
        _holdLightOff.SetActive(true);
    }

    public void OnPressurePlateActivate()
    {
        Debug.Log("Pressure Plate activated!");
        Debug.Log($"Pressure Plate state = {_pressurePlate.IsActivated}");

        _pressurePlateLightOn.SetActive(true);
        _pressurePlateLightOff.SetActive(false);
    }

    public void OnPressurePlateDeactivate()
    {
        Debug.Log("Pressure Plate deactivated!");
        Debug.Log($"Pressure Plate state = {_pressurePlate.IsActivated}");

        _pressurePlateLightOn.SetActive(false);
        _pressurePlateLightOff.SetActive(true);
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
            _networkObject.UpdateData(_counter);
        }
        else if(Keyboard.current.digit4Key.wasPressedThisFrame)
            _handheldActivatable.SetActivated(!_pushActivatable.IsActivated);
        else if(Keyboard.current.digit5Key.wasPressedThisFrame)
            _handheldAdjustable.SetValue(_handheldAdjustable.Value - 1);
        else if (Keyboard.current.digit6Key.wasPressedThisFrame)
            _handheldAdjustable.SetValue(_handheldAdjustable.Value + 1);

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

    private void HandleNetworkObjectStateChange(object data)
    {
        _counter = (int)data;
        Debug.Log(_counter);
    }

    // private void HandleNewColor()
    // {
    //     if (lightColor == LightColours.Bue)
    //         lightColor = LightColours.White;
    //     else
    //         lightColor++;

    //     _light.color = lightColor switch
    //     {
    //         LightColours.Red => Color.red,
    //         LightColours.Green => Color.green,
    //         LightColours.Bue => Color.blue,
    //         _ => Color.white,
    //     };
    // }

    // private LightColours lightColor = LightColours.White;
    // private enum LightColours
    // {
    //     White, 
    //     Red, 
    //     Green,
    //     Bue,
    // }
}
