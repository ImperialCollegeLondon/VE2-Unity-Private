using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViRSE.Core.Player;

namespace ViRSE.Core.Shared //TODO break into different files
{
    public interface IMultiplayerSupport
    {
        public void RegisterStateModule(IStateModule stateModule, string stateType, string goName); //TODO, should we be passing in the state config here, rather than have the syncer pull it?
        public void RegisterLocalPlayer(ILocalPlayerRig localPlayerRig);
        public void DeregisterLocalPlayer();
        public bool IsEnabled { get; }
        public string GameObjectName { get; }
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
