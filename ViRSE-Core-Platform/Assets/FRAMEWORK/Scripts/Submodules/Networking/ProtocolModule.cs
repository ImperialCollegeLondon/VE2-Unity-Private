using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ProtocolConfig
{
    [SerializeField] public TransmissionProtocol transmissionType;
}

public interface IProtocolModule
{
    TransmissionProtocol TransmissionType { get; }
}
