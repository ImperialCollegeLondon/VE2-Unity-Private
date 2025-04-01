using UnityEngine;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Internal;

namespace VE2.Core.VComponents.Tests
{
    internal class V_HoldActivatableProviderStub : IV_HoldActivatable, IRangedHoldClickInteractionModuleProvider, ICollideInteractionModuleProvider
    {
        #region Plugin Interfaces
        IMultiInteractorActivatableStateModule IV_HoldActivatable._StateModule => _HoldActivatable.StateModule;
        IRangedHoldClickInteractionModule IV_HoldActivatable._RangedHoldClickModule => _HoldActivatable.RangedClickInteractionModule;
        #endregion

        #region Player Interfaces
        ICollideInteractionModule ICollideInteractionModuleProvider.CollideInteractionModule => _HoldActivatable.ColliderInteractionModule;
        IRangedInteractionModule IRangedInteractionModuleProvider.RangedInteractionModule => _HoldActivatable.RangedClickInteractionModule;
        #endregion

        internal HoldActivatableService _HoldActivatable = null;

        internal V_HoldActivatableProviderStub(HoldActivatableService HoldActivatable)
        {
            _HoldActivatable = HoldActivatable;
        }

        public void TearDown()
        {
            _HoldActivatable.TearDown();
            _HoldActivatable = null;
        }
    }
}
