using System;
using VE2.Core.VComponents.API;
using VE2.NonCore.Instancing.API;
using static VE2.Core.Common.CommonSerializables;

namespace VE2.NonCore.Instancing.Internal
{
    internal class NetworkObjectService
    {
        #region Interfaces
        public INetworkObjectStateModule StateModule => _StateModule;
        #endregion

        #region Modules
        private readonly NetworkObjectStateModule _StateModule;
        #endregion

        public NetworkObjectService(NetworkObjectStateConfig config, VE2Serializable state, string id, IWorldStateSyncService worldStateSyncService)
        {
            _StateModule = new(state, config, id, worldStateSyncService);
        }

        public void HandleFixedUpdate()
        {
            _StateModule.HandleFixedUpdate();
        }

        public void TearDown()
        {
            _StateModule.TearDown();
        }
    }
}
