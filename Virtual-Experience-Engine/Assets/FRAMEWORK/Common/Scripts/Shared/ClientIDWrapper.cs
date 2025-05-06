using System;
using UnityEngine;

namespace VE2.Common.Shared
{
    public interface IClientIDWrapperInternal : IClientIDWrapper
    {
        new ushort Value { get; set; } 
    }

    public interface IClientIDWrapper
    {
        ushort Value { get; }
        public bool IsClientIDReady { get; }
        event Action<ushort> OnClientIDReady;
        public bool IsLocal { get; }
        public bool IsRemote { get; }
    }

    [Serializable] 
    public class ClientIDWrapper : IClientIDWrapperInternal
    { 
        [SerializeField] private ushort _clientID = ushort.MaxValue;

        public ushort Value 
        {
            get => _clientID;
            set 
            {
                _clientID = value;
                OnClientIDReady?.Invoke(value);
            }
        } 
        
        public event Action<ushort> OnClientIDReady;
        public bool IsClientIDReady => _clientID != ushort.MaxValue;

        [SerializeField] public bool IsLocal {get; set;}
        public bool IsRemote => !IsLocal;

        public ClientIDWrapper(ushort clientID, bool isLocal)
        {
            _clientID = clientID;
            IsLocal = isLocal;
        }
    }
}
