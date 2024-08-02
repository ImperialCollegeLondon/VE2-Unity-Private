using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ProtocolConfig
{
    [SerializeField] public TransmissionProtocol TransmissionType;
}

public class ProtocolModule : IProtocolModule
{
    #region Plugin Interfaces
    public TransmissionProtocol TransmissionProtocol { get => _config.TransmissionType; set => _config.TransmissionType = value; }
    #endregion

    private ProtocolConfig _config;

    public ProtocolModule(ProtocolConfig config)
    {
        _config = config;
    }
}
