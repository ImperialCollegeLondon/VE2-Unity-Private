using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProtocolModule : MonoBehaviour
{
    [PropertyOrder(1001)]
    [PropertySpace(SpaceBefore = 5)]
    [FoldoutGroup("ProtocolModuleVGroup")]
    [SerializeField] public TransmissionProtocol transmissionType { get; private set; } = TransmissionProtocol.UDP;

    public void TearDown()
    {
        DestroyImmediate(this);
    }

}

public interface IProtocolModule
{
    TransmissionProtocol TransmissionType { get; }
}
