using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ViRSE.Core.Shared //TODO break into different files
{
    public interface IMultiplayerSupport
    {
        public void RegisterStateModule(IStateModule stateModule, string stateType, string goName);
        public bool IsEnabled { get; }
        public string MultiplayerSupportGameObjectName { get; }
    }

    public interface IStateModule
    {
        public byte[] StateAsBytes { get; set; }
        public string GOName { get; }

        public TransmissionProtocol TransmissionProtocol { get; }
        public float TransmissionFrequency { get; }
    }

    public enum TransmissionProtocol //TODO - move
    {
        UDP,
        TCP
    }
}
