using UnityEngine;

namespace VE2.Core.VComponents.API
{
    internal interface IWorldStateSyncProvider 
    {
        public IWorldStateSyncService WorldStateSyncService { get; }
        public string GameObjectName { get; }
        public bool IsEnabled { get; }
    }
}
