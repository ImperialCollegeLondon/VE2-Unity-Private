using UnityEngine;
using UnityEngine.InputSystem;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;

public class AdminTestTuesday : MonoBehaviour
{
    [SerializeField] private InterfaceReference<IV_LinearAdjustable> _linearAdjustable;

    private void Example()
    {
        // //Wiring up a listener programmatically
        // _linearAdjustable.Interface.OnActivate.AddListener(HandleActivate);

        // //To activate the activatable programmatically 
        // _linearAdjustable.Interface.Activate();
    }

    private void HandleActivate()
    {
        Debug.Log("Activatable was activated!");
    }

    // Update is called once per frame
    void Update()
    {
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            if (!VE2API.PlatformService.IsLocalPlayerAdmin)
                VE2API.PlatformService.GrantLocalPlayerAdmin();
            else
                VE2API.PlatformService.RevokeLocalPlayerAdmin();
        }

        if (Keyboard.current.bKey.wasPressedThisFrame)
        {
            _pressurePlate.Interface.ToggleAlwaysActivated(true);
        }
        else if (Keyboard.current.nKey.wasPressedThisFrame)
        {
            _pressurePlate.Interface.ToggleAlwaysActivated(false);
        }
    }

    [SerializeField] private InterfaceReference<IV_GeneralInteractable> _generalInteractable;
    private void ExampleToggleActivatable()
    {
        _generalInteractable.Interface.AdminOnly = true;
    }

    [SerializeField] private InterfaceReference<IV_PressurePlate> _pressurePlate;
}
