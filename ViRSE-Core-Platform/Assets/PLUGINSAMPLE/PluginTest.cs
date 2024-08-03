using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using ViRSE;
using ViRSE.PluginRuntime.VComponents;

public class PluginTest : MonoBehaviour
{
    [SerializeField] private GameObject _pushButtonGO;
    private IPushActivatable _pushActivatable => _pushButtonGO.GetComponent<IPushActivatable>();

    // Start is called before the first frame update
    void Start()
    {
        _pushActivatable.OnActivate.AddListener(OnButtonActivate);
        _pushActivatable.OnDeactivate.AddListener(OnButtonDeactivate);
    }

    private void OnButtonActivate()
    {
        InteractorID interactorID = _pushActivatable.CurrentInteractor;
        Debug.Log("Button activated! ");
        Debug.Log($"Button state = {_pushActivatable.IsActivated}");

        if (interactorID != null) 
            Debug.Log($"Activate by... {interactorID.ToString()}");
    }

    private void OnButtonDeactivate()
    {
        Debug.Log("Button deactivated! ");
        Debug.Log($"Button state = {_pushActivatable.IsActivated}");
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            _pushActivatable.IsActivated = !_pushActivatable.IsActivated;
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            _pushActivatable.InteractRange = 0;
        }
    }
}
