using System;

namespace VE2.Core.Player.API
{
    public interface ILocalClientIDProvider
    {
        public bool IsClientIDReady => LocalClientID != ushort.MaxValue;
        public event Action<ushort> OnClientIDReady;
        public ushort LocalClientID { get; }
        public string GameObjectName { get; }
        public bool IsEnabled { get; }
    }
}

