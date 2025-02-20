using System;
using UnityEngine;

namespace VE2.NonCore.Instancing.Internal
{
    [Serializable] 
    public class LocalClientIdWrapper 
    { 
        private ushort _localClientID = ushort.MaxValue; 
        public ushort LocalClientID 
        {
            get => _localClientID;
            set 
            {
                _localClientID = value;
                OnLocalClientIDSet?.Invoke(value);
            }
        } 
        
        public event Action<ushort> OnLocalClientIDSet;
    }
}
