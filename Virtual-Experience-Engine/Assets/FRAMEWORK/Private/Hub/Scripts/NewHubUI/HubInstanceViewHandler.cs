using System;
using UnityEngine;
using UnityEngine.UI;
using VE2.Common.Shared;
using static VE2.NonCore.Platform.API.PlatformPublicSerializables;

internal class HubInstanceDisplayHandler
{
    public event Action<InstanceCode> OnInstanceButtonClicked;

    private readonly HubInstanceView _view;

    public HubInstanceDisplayHandler(PlatformInstanceInfo instanceInfo, bool isSelected, GameObject prefab, VerticalLayoutGroup instancesVerticalGroup)
    {
        GameObject instanceButton = GameObject.Instantiate(prefab, instancesVerticalGroup.transform); 
        _view = instanceButton.GetComponent<HubInstanceView>();

        _view.SetupView(instanceInfo, isSelected);
        _view.OnSelectInstance += () => OnInstanceButtonClicked?.Invoke(instanceInfo.InstanceCode);
    }

    public void UpdateDisplay(PlatformInstanceInfo instanceInfo, bool isSelected) => _view.UpdateInstanceInfo(instanceInfo, isSelected);

    public void Destroy()
    {
        GameObject.Destroy(_view.gameObject);
    }
}
