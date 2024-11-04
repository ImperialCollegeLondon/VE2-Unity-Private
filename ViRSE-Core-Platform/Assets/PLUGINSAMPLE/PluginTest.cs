using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using VE2;
using VE2.Core.VComponents;
using VE2.Core.VComponents.PluginInterfaces;

public class PluginTest : MonoBehaviour
{
    [SerializeField] private GameObject _lightOn;
    [SerializeField] private GameObject _lightOff;
    [SerializeField] private GameObject _pushButtonGO;
    private IV_ToggleActivatable _pushActivatable => _pushButtonGO.GetComponent<IV_ToggleActivatable>();

    // Start is called before the first frame update
    void Start()
    {
        //_pushActivatable.OnActivate.AddListener(OnButtonActivate);
        //_pushActivatable.OnDeactivate.AddListener(OnButtonDeactivate);
    }

    public void OnButtonActivate()
    {
        ushort clientID = _pushActivatable.MostRecentInteractingClientID;
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

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            _pushActivatable.IsActivated = !_pushActivatable.IsActivated;
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            _pushActivatable.InteractRange = 0;
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
