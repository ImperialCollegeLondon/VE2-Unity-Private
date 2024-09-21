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
    public string IP;
    public ushort Port;
    public string InstanceCode;

    public InstanceNetworkSettings(string iP, ushort port, string instanceCode)
    {
        IP = iP;
        Port = port;
        InstanceCode = instanceCode;
    }
}
