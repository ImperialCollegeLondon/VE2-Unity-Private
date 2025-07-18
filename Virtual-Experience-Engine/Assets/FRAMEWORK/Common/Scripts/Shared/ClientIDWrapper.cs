using System;
using UnityEngine;

namespace VE2.Common.Shared
{
    internal interface ILocalClientIDWrapperWritable : ILocalClientIDWrapper
    {
        public void SetValue(ushort clientID);
    }

    public interface ILocalClientIDWrapper : IClientIDWrapper
    {
        public bool IsClientIDReady { get; }
        event Action<ushort> OnClientIDReady;
    }

    public interface IClientIDWrapper
    {
        ushort Value { get; }
        public bool IsLocal { get; }
        public bool IsRemote { get; }
    }

    [Serializable] 
    internal class LocalClientIDWrapper : ClientIDWrapper, ILocalClientIDWrapperWritable
    {         
        public event Action<ushort> OnClientIDReady;
        public bool IsClientIDReady => _ClientID != ushort.MaxValue;

        public LocalClientIDWrapper(ushort clientID) : base(clientID, true) { }

        public void SetValue(ushort clientID)
        {
            _ClientID = clientID;
            OnClientIDReady?.Invoke(clientID);
        }
    }

    [Serializable] 
    internal class ClientIDWrapper : IClientIDWrapper
    { 
        [SerializeField] protected ushort _ClientID = ushort.MaxValue;
        public ushort Value => _ClientID;

        [SerializeField] public bool IsLocal {get; set;}
        public bool IsRemote => !IsLocal;

        public ClientIDWrapper(ushort clientID, bool isLocal)
        {
            _ClientID = clientID;
            IsLocal = isLocal;
        }
    }
}
