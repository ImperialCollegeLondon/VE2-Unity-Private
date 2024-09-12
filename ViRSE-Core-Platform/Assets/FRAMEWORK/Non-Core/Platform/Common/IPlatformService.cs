using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlatformService //TODO, maybe not all of these should live in the same interface?
{
    public bool IsConnectedToServer { get; }
    public event Action OnConnectedToServer;

    public UserSettings UserSettings { get; }
    public InstanceNetworkSettings InstanceNetworkSettings { get; }

    public void RequestInstanceAllocation(string worldName, string instanceSuffix);

    public void TearDown();
}
