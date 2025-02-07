using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBaseNetworkSettingsProvider //TODO - should also be exposed to the customer??
{
    public bool AreNetworkingSettingsReady { get; }
    public event Action OnNetworkSettingsReady;
    public string GameObjectName { get; }
    public bool IsEnabled { get; }
}

public interface IInstanceNetworkSettingsProvider : IBaseNetworkSettingsProvider
{
   public InstanceNetworkSettings InstanceNetworkSettings { get; }
}

public interface IFTPNetworkSettingsProvider : IBaseNetworkSettingsProvider
{
    public NetworkSettings NetworkSettings { get; }
}

[Serializable]
public class NetworkSettings
{
    [SerializeField] public string IP;
    [SerializeField] public ushort Port;

    public NetworkSettings(string iP, ushort port)
    {
        IP = iP;
        Port = port;
    }
}

[Serializable]
public class InstanceNetworkSettings : NetworkSettings
{
    [SerializeField] public string InstanceCode;

    public InstanceNetworkSettings(string iP, ushort port, string instanceCode) : base(iP, port)
    {
        InstanceCode = instanceCode;
    }
}
