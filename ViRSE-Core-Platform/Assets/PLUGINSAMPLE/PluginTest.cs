using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ViRSE;
using ViRSE.PluginRuntime.VComponents;

public class PluginTest : MonoBehaviour
{
    [SerializeField] private Light _light;
    [SerializeField] private GameObject _pushButtonGO;
    private IPushActivatable _pushActivatable => _pushButtonGO.GetComponent<IPushActivatable>();

    // Start is called before the first frame update
    void Start()
    {
        //_pushActivatable.OnActivate.AddListener(OnButtonActivate);
        //_pushActivatable.OnDeactivate.AddListener(OnButtonDeactivate);
    }

    public void OnButtonActivate()
    {
        InteractorID interactorID = _pushActivatable.CurrentInteractor;
        Debug.Log("Button activated! ");
        Debug.Log($"Button state = {_pushActivatable.IsActivated}");

        if (interactorID != null) 
            Debug.Log($"Activate by... {interactorID.ToString()}");

        _light.enabled = true;
        HandleNewColor();
    }

    public void OnButtonDeactivate()
    {
        Debug.Log("Button deactivated! ");
        Debug.Log($"Button state = {_pushActivatable.IsActivated}");

        _light.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
            _pushActivatable.IsActivated = !_pushActivatable.IsActivated;
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            _pushActivatable.InteractRange = 0;
    }

    private void HandleNewColor()
    {
        if (lightColor == LightColours.Bue)
            lightColor = LightColours.White;
        else
            lightColor++;

        _light.color = lightColor switch
        {
            LightColours.Red => Color.red,
            LightColours.Green => Color.green,
            LightColours.Bue => Color.blue,
            _ => Color.white,
        };
    }

    private LightColours lightColor = LightColours.White;
    private enum LightColours
    {
        White, 
        Red, 
        Green,
        Bue,
    }
}
