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
}

/*
 *  One interface that faces the platform integration package that gets imported by customers 
 *  Another interface that faces the private platform stuff, the same package that the PlatformService lives in, is meant to provide available worlds, and global info 
 * 
 */