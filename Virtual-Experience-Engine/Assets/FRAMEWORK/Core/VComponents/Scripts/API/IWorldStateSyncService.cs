using UnityEngine;

namespace VE2.Core.VComponents.API
{
    internal interface IWorldStateSyncService
    {
        public void RegisterWorldStateModule(IWorldStateModule module);
        public void DeregisterWorldStateModule(IWorldStateModule module);
    }
}
