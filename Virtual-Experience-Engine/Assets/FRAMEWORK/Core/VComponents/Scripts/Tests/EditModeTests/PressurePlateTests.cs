using UnityEngine;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Internal;

namespace VE2.Core.VComponents.Tests
{
    internal class V_PressurePlateStub : IV_PressurePlate, ICollideInteractionModuleProvider
    {
        #region Plugin Interfaces
        IMultiInteractorActivatableStateModule IV_PressurePlate._StateModule => _pressurePlate.StateModule;
        ICollideInteractionModule IV_PressurePlate._ColliderModule => _pressurePlate.ColliderInteractionModule;
        #endregion

        #region Player Interfaces
        ICollideInteractionModule ICollideInteractionModuleProvider.CollideInteractionModule => _pressurePlate.ColliderInteractionModule;
        #endregion

        internal PressurePlateService _pressurePlate = null;

        internal V_PressurePlateStub(PressurePlateService pressurePlateService)
        {
            _pressurePlate = pressurePlateService;
        }

        public void TearDown()
        {
            _pressurePlate.TearDown();
            _pressurePlate = null;
        }
    }
}
