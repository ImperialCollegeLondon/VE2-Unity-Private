using System;
using UnityEngine;
using VE2.Common;
using VE2.NonCore.Instancing.VComponents.NonInteractableInterfaces;
using static VE2.Common.CommonSerializables;

namespace VE2.NonCore.Instancing.VComponents.Internal
{

    public class NetworkObjectService
    {
        #region Interfaces
        public INetworkObjectStateModule StateModule => _StateModule;
        #endregion

        #region Modules
        private readonly NetworkObjectStateModule _StateModule;
        #endregion

        public NetworkObjectService(NetworkObjectStateConfig config, VE2Serializable state, string id, WorldStateModulesContainer worldStateModulesContainer)
        {
            _StateModule = new(state, config, id, worldStateModulesContainer);
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
