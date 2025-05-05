using System;
using UnityEngine;

namespace VE2.NonCore.Instancing.Internal //TODO - remove, moved to API
{
    // public interface ILocalClientIdWrapperInternal : ILocalClientIdWrapper
    // {
    //     new ushort LocalClientID { get; set; } 
    // }

    // public interface ILocalClientIdWrapper
    // {
    //     ushort LocalClientID { get; }
    //     event Action<ushort> OnLocalClientIDSet;
    //     public bool IsLocal { get; }
    //     public bool IsRemote { get; }
    // }

    // [Serializable] 
    // public class LocalClientIdWrapper 
    // { 
    //     private ushort _localClientID = ushort.MaxValue; 
    //     public ushort LocalClientID 
    //     {
    //         get => _localClientID;
    //         set 
    //         {
    //             _localClientID = value;
    //             OnLocalClientIDSet?.Invoke(value);
    //         }
    //     } 
        
    //     public event Action<ushort> OnLocalClientIDSet;

    //     public bool IsLocal;
    //     public bool IsRemote;
    // }
}
