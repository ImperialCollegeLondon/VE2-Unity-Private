using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PluginTest : MonoBehaviour
{
    [SerializeField] private GameObject pushButtonGO;
    private IPushActivatable pushActivatable => pushButtonGO.GetComponent<IPushActivatable>();

    // Start is called before the first frame update
    void Start()
    {
        pushActivatable.OnActivate.AddListener(OnButtonActivate);
        pushActivatable.OnDeactivate.AddListener(OnButtonDeactivate);
    }

    private void OnButtonActivate()
    {
        InteractorID interactorID = pushActivatable.CurrentInteractor;
        Debug.Log("Button activated! ");
        Debug.Log($"Button state = {pushActivatable.IsActivated}");

        if (interactorID != null) 
            Debug.Log($"Activate by... {interactorID.ToString()}");
    }

    private void OnButtonDeactivate()
    {
        Debug.Log("Button deactivated! ");
        Debug.Log($"Button state = {pushActivatable.IsActivated}");
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            pushActivatable.IsActivated = !pushActivatable.IsActivated;
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            pushActivatable.InteractRange = 0;
        }
    }
}
