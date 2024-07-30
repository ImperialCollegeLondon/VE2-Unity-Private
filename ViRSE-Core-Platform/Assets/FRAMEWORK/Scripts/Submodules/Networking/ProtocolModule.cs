using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ProtocolModule
{
    // [PropertyOrder(1001)]
    //[PropertySpace(SpaceBefore = 5)]
    //[FoldoutGroup("ProtocolModuleVGroup")]
    [SerializeField, ShowInInspector] public TransmissionProtocol transmissionType;
    [HideInInspector] public TransmissionProtocol TransmissionType => transmissionType;

    public ProtocolModule()
    {
        transmissionType = TransmissionProtocol.UDP;
    }

}

public interface IProtocolModule
{
    TransmissionProtocol TransmissionType { get; }
}
