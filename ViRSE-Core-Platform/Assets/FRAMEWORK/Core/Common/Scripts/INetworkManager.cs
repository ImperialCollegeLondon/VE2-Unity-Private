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
        public bool IsNetworked { get; }
        public event Action<bool> OnIsNetworkedChanged;
        public TransmissionProtocol TransmissionProtocol { get; }
        public float TransmissionFrequency { get; }
    }

    public interface IWorldStateModule : IBaseStateModule
    {
        public string ID { get; }
        public byte[] StateAsBytes { get; set; }
    }

    public interface IPlayerStateModule : IBaseStateModule
    {
        public ViRSESerializable PlayerTransform {get;}
        public ViRSEAvatarAppearance AvatarAppearance {get;}
        public event Action<ViRSEAvatarAppearance> OnAvatarAppearanceChanged;
    }

    public enum TransmissionProtocol //TODO - move
    {
        UDP,
        TCP
    }
}
