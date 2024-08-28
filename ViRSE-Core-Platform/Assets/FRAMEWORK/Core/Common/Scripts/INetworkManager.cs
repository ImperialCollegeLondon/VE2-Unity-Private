using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ViRSE.Core.Shared
{
    public abstract class ViRSESerializable
    {
        public byte[] Bytes { get => ConvertToBytes(); set => PopulateFromBytes(value); }

        public ViRSESerializable() { }

        public ViRSESerializable(byte[] bytes)
        {
            PopulateFromBytes(bytes);
        }

        protected abstract byte[] ConvertToBytes();

        protected abstract void PopulateFromBytes(byte[] bytes);
    }

    public interface INetworkManager
    {
        public void RegisterStateModule(IStateModule stateModule, string stateType, string goName);
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

    /*
     * Where does this live? 
     * 
     * It needs to be assessible to the VCs, but also the player rig... and maybe even the core platform integration stuff??
     * So surely then it needs to go into some shared interface? Maybe just like "Networker Interface"?
     */

}