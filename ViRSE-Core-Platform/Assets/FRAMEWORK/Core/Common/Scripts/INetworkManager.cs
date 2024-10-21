using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViRSE.Core.Player;
using static ViRSE.Core.Shared.CoreCommonSerializables;

namespace ViRSE.Core.Shared //TODO break into different files
{
    public interface IMultiplayerSupport
    {
        public bool IsEnabled { get; }
        public string GameObjectName { get; }
    }

    public interface IBaseStateModule 
    {
        public ViRSESerializable State { get; }
        public TransmissionProtocol TransmissionProtocol { get; }
        public float TransmissionFrequency { get; }

        //TODO - could put config and wiring in here
    }

    public interface IWorldStateModule : IBaseStateModule
    {
        public string ID { get; }
        public byte[] StateAsBytes { get; set; }
    }

    public interface IPlayerStateModule : IBaseStateModule
    {
        public ViRSEAvatarAppearance AvatarAppearance {get;}
        public event Action<ViRSEAvatarAppearance> OnAvatarAppearanceChanged;
    }

    public enum TransmissionProtocol //TODO - move
    {
        UDP,
        TCP
    }
}
