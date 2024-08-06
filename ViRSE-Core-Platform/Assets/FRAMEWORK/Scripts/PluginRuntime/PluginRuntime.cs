using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class PluginConfig
{
    public UnityEvent<int> OnRemoteUserJoinInstance;
    public UnityEvent<int> OnRemoteUserLeavenInstance;
}

public class PluginRuntime : MonoBehaviour
{
    private PluginConfig _pluginConfig;

    public void Initialize(PluginConfig pluginConfig)
    {
        _pluginConfig = pluginConfig;
    }
}
