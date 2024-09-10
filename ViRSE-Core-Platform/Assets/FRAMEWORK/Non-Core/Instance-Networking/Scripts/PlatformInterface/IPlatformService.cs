using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlatformService
{
    public bool IsConnectedToServer { get; }
    public event Action OnConnectedToServer;
    public InstanceConnectionDetails GetInstanceConnectionDetails();
}

[Serializable]
public class InstanceConnectionDetails
{
    public string IP;
    public ushort Port;
    public string InstanceCode;
}
