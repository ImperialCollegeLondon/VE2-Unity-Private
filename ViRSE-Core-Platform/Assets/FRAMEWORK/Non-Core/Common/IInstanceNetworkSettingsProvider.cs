using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IInstanceNetworkSettingsProvider //TODO - should also be exposed to the customer??
{
    public bool AreInstanceNetworkingSettingsReady { get; }
    public event Action OnInstanceNetworkSettingsReady;
    public InstanceNetworkSettings InstanceNetworkSettings { get; }
    public string GameObjectName { get; }
    public bool IsEnabled { get; }
}

[Serializable]
public class InstanceNetworkSettings
{
    [Title("Connection Settings")]
    [BeginGroup(Style = GroupStyle.Round)]
    [SerializeField] public string IP;
    [SerializeField] public ushort Port;
    [EndGroup]
    [SerializeField] public string InstanceCode;

    public InstanceNetworkSettings(string iP, ushort port, string instanceCode)
    {
        IP = iP;
        Port = port;
        InstanceCode = instanceCode;
    }
}
