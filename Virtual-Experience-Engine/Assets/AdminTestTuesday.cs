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
        //Wiring up a listener programmatically
        //_linearAdjustable.Interface.OnValueAdjusted.AddListener(HandleActivate);

        //To activate the activatable programmatically
        _linearAdjustable.Interface.SetValue(1f);
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

    private void OnEnable()
    {
        VE2API.InstanceService.OnBecomeHost.AddListener(OnBecomeHost);
        VE2API.InstanceService.OnBecomeNonHost.AddListener(OnLoseHost);
        VE2API.InstanceService.OnConnectedToInstance.AddListener(OnConnectedToInstance);
        VE2API.InstanceService.OnDisconnectedFromInstance.AddListener(OnDisconnectedFromInstance);
        VE2API.InstanceService.OnRemoteClientJoinedInstance.AddListener(OnRemoteClientJoinedInstance);
        VE2API.InstanceService.OnRemoteClientLeftInstance.AddListener(OnRemoteClientLeftInstance);
    }

    [SerializeField] private InterfaceReference<IV_GeneralInteractable> _generalInteractable;
    private void ExampleToggleActivatable()
    {
        _generalInteractable.Interface.AdminOnly = true;
    }

    [SerializeField] private InterfaceReference<IV_PressurePlate> _pressurePlate;

    private void OnBecomeHost()
    {
        Debug.Log("Became host");
    }

    private void OnLoseHost()
    {
        Debug.Log("Became non host");
    }

    private void OnConnectedToInstance(ushort clientID)
    {
        Debug.Log("Connected to instance with client ID: " + clientID);
    }

    private void OnDisconnectedFromInstance(ushort clientID)
    {
        Debug.Log("Disconnected from instance with client ID: " + clientID);
    }

    private void OnRemoteClientJoinedInstance(ushort clientID)
    {
        Debug.Log("Remote client joined instance with client ID: " + clientID);
        foreach (var id in VE2API.InstanceService.ClientIDsInCurrentInstance)
        {
            Debug.Log("Client ID in instance: " + id);
        }
    }

    private void OnRemoteClientLeftInstance(ushort clientID)
    {
        Debug.Log("Remote client left instance with client ID: " + clientID);
        foreach (var id in VE2API.InstanceService.ClientIDsInCurrentInstance)
        {
            Debug.Log("Client ID in instance: " + id);
        }
    }
    
}
