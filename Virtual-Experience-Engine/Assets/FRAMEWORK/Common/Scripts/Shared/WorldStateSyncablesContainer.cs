using System;
using System.Collections.Generic;
using UnityEngine;

namespace VE2.Common.Shared
{
    internal interface IWorldStateSyncableContainer
    {
        Dictionary<string, IWorldStateModule> WorldStateSyncables { get; }
        event Action<IWorldStateModule> OnWorldStateSyncableRegistered;
        event Action<IWorldStateModule> OnWorldStateSyncableDeregistered;
        void RegisterWorldStateSyncable(IWorldStateModule worldStateSyncable);
        void DeregisterWorldStateSyncable(IWorldStateModule worldStateSyncable);
    }

    internal class WorldStateSyncableContainer : IWorldStateSyncableContainer
    {
        public Dictionary<string, IWorldStateModule> WorldStateSyncables { get; } = new();
        public event Action<IWorldStateModule> OnWorldStateSyncableRegistered;
        public event Action<IWorldStateModule> OnWorldStateSyncableDeregistered;

        public void RegisterWorldStateSyncable(IWorldStateModule worldStateSyncable)
        {
            WorldStateSyncables.Add(worldStateSyncable.ID, worldStateSyncable);
            OnWorldStateSyncableRegistered?.Invoke(worldStateSyncable);
        }

        public void DeregisterWorldStateSyncable(IWorldStateModule worldStateSyncable)
        {
            WorldStateSyncables.Remove(worldStateSyncable.ID);
            OnWorldStateSyncableDeregistered?.Invoke(null);
        }
    }
}
