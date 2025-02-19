using System;
using static VE2.Common.CommonSerializables;
using VE2.Core.VComponents.InteractableInterfaces;

namespace VE2.Common 
{
    public interface ILocalClientIDProvider
    {
        public bool IsClientIDReady => LocalClientID != ushort.MaxValue;
        public event Action<ushort> OnClientIDReady;
        public ushort LocalClientID { get; }
        public string GameObjectName { get; }
        public bool IsEnabled { get; }
    }

    // internal interface ILocalClientIDProviderProvider
    // {
    //     public ILocalClientIDProvider LocalClientIDProvider { get; }
    //     public string GameObjectName { get; }
    //     public bool IsEnabled { get; }
    // }
}

