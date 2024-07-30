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
    //[SerializeField] public TransmissionProtocol transmissionType { get; private set; } = TransmissionProtocol.UDP;
    [SerializeField] public TransmissionProtocol transmissionType = TransmissionProtocol.UDP;

}

public interface IProtocolModule
{
    TransmissionProtocol TransmissionType { get; }
}
