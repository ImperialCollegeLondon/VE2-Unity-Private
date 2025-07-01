using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Internal;

namespace VE2.Core.VComponents.Tests
{
    internal partial class V_PressurePlateActivatableStub : IV_PressurePlateActivatable
    {
        #region State Module Interface
        internal IMultiInteractorActivatableStateModule _StateModule => _Service.StateModule;

        public UnityEvent OnActivate => _StateModule.OnActivate;
        public UnityEvent OnDeactivate => _StateModule.OnDeactivate;

        public bool IsActivated { get { return _StateModule.IsActivated; } }
        public void ToggleAlwaysActivated(bool toggle) => _StateModule.ToggleAlwaysActivated(toggle);
        public IClientIDWrapper MostRecentInteractingClientID => _StateModule.MostRecentInteractingClientID;
        public List<IClientIDWrapper> CurrentlyInteractingClientIDs => _StateModule.CurrentlyInteractingClientIDs;
        #endregion

        #region General Interaction Module Interface
        //We have two General Interaction Modules here, it doesn't matter which one we point to, both share the same General Interaction Config object!
        internal ICollideInteractionModule _ColliderModule => _Service.ColliderInteractionModule;
        public bool AdminOnly { get => _ColliderModule.AdminOnly; set => _ColliderModule.AdminOnly = value; }
        public bool EnableControllerVibrations { get => _ColliderModule.EnableControllerVibrations; set => _ColliderModule.EnableControllerVibrations = value; }
        public bool ShowTooltipsAndHighlight { get => _ColliderModule.ShowTooltipsAndHighlight; set => _ColliderModule.ShowTooltipsAndHighlight = value; }
        public bool IsInteractable { get => _ColliderModule.IsInteractable; set => _ColliderModule.IsInteractable = value; }
        #endregion
    }

    internal partial class V_PressurePlateActivatableStub : ICollideInteractionModuleProvider
    {
        #region Player Interfaces
        ICollideInteractionModule ICollideInteractionModuleProvider.CollideInteractionModule => _Service.ColliderInteractionModule;
        #endregion

        internal PressurePlateActivatableService _Service = null;

        internal V_PressurePlateActivatableStub(PressurePlateActivatableService service)
        {
            _Service = service;
        }

        public void TearDown()
        {
            _Service.TearDown();
            _Service = null;
        }
    }
}
