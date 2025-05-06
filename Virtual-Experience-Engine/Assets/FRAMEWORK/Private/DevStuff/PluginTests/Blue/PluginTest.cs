using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using VE2.Core.Common;
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

    [SerializeField] private TMP_Text _roomColorText;


    private IV_ToggleActivatable _pushActivatable => _pushButtonGO.GetComponent<IV_ToggleActivatable>();
    private IV_FreeGrabbable _freeGrabbable => _freeGrabbableGO.GetComponent<IV_FreeGrabbable>();
    private IV_HandheldActivatable _handheldActivatable => _handheldActivatableGO.GetComponent<IV_HandheldActivatable>();
    private IV_HandheldAdjustable _handheldAdjustable => _handheldAdjustableGO.GetComponent<IV_HandheldAdjustable>();
    private IV_NetworkObject _networkObject => _networkObjectGO.GetComponent<IV_NetworkObject>();

    private int _counter = 0;

    // Start is called before the first frame update
    void Start()
    {
        _roomColorText.text = "Blue Room";
        _roomColorText.color = Color.blue;

        //_pushActivatable.OnActivate.AddListener(OnButtonActivate);
        //_pushActivatable.OnDeactivate.AddListener(OnButtonDeactivate);

        _freeGrabbable.OnGrab.AddListener(OnFreeGrabbableGrab);
        _freeGrabbable.OnDrop.AddListener(OnFreeGrabbableDrop);

        _handheldActivatable.OnActivate.AddListener(OnHandheldActivatableActivate);
        _handheldActivatable.OnDeactivate.AddListener(OnHandheldActivatableDeactivate);
        _handheldAdjustable.OnValueAdjusted.AddListener(OnHandheldAdjustableValueAdjusted);

        _networkObject.OnStateChange.AddListener(HandleNetworkObjectStateChange);

    }

    public void OnButtonActivate()
    {
        IClientIDWrapper clientID = _pushActivatable.MostRecentInteractingClientID;
        Debug.Log("Button activated! ");
        Debug.Log($"Button state = {_pushActivatable.IsActivated}");

        Debug.Log($"Activate by... {clientID.ClientID.ToString()}");

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
