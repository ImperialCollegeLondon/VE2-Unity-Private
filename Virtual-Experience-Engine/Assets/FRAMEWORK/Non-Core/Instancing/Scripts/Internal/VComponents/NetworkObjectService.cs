using VE2.Common.Shared;
using VE2.NonCore.Instancing.API;
using static VE2.Common.Shared.CommonSerializables;

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

        public NetworkObjectService(NetworkObjectStateConfig config, VE2Serializable state, string id, IWorldStateSyncableContainer worldStateSyncableContainer)
        {
            _StateModule = new(state, config, id, worldStateSyncableContainer);
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
