using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViRSE.Core.Player;

namespace ViRSE.Core.Shared //TODO break into different files
{
    public interface IMultiplayerSupport
    {
        public bool IsEnabled { get; }
        public string GameObjectName { get; }
    }

    public interface IStateModule //TODO, rename to 
    {
        public string ID {get; }
        public byte[] StateAsBytes { get; set; }

        public TransmissionProtocol TransmissionProtocol { get; }
        public float TransmissionFrequency { get; }
    }

    public enum TransmissionProtocol //TODO - move
    {
        UDP,
        TCP
    }
}
