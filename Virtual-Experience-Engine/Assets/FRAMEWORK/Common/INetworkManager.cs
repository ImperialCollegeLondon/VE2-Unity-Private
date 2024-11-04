using System;
using static VE2.Common.CommonSerializables;

namespace VE2.Common //TODO break into different files
{
    public interface IMultiplayerSupport
    {
        public bool IsEnabled { get; }
        public string GameObjectName { get; }
        public ushort LocalClientID { get; }
        public bool IsHost { get; }
        public bool IsConnectedToServer { get; }
    }

    public interface IBaseStateModule 
    {
        public VE2Serializable State { get; }
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
        public AvatarAppearance AvatarAppearance {get;}
        public event Action<AvatarAppearance> OnAvatarAppearanceChanged;
    }

    public enum TransmissionProtocol //TODO - move
    {
        UDP,
        TCP
    }
}
