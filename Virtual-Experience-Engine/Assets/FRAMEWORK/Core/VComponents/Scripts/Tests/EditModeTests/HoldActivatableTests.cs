using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Internal;

namespace VE2.Core.VComponents.Tests
{
    internal partial class V_HoldActivatableProviderStub : IV_HoldActivatable
    {
        #region State Module Interface
        internal IMultiInteractorActivatableStateModule _StateModule => _Service.StateModule;

        public UnityEvent OnActivate => _StateModule.OnActivate;
        public UnityEvent OnDeactivate => _StateModule.OnDeactivate;

        public bool IsActivated => _StateModule.IsActivated;
        public IClientIDWrapper MostRecentInteractingClientID => _StateModule.MostRecentInteractingClientID;
        public List<IClientIDWrapper> CurrentlyInteractingClientIDs => _StateModule.CurrentlyInteractingClientIDs;
        #endregion

        #region Ranged Interaction Module Interface
        internal IRangedHoldClickInteractionModule _RangedHoldClickModule => _Service.RangedClickInteractionModule;
        public float InteractRange { get => _RangedHoldClickModule.InteractRange; set => _RangedHoldClickModule.InteractRange = value; }
        #endregion

        #region General Interaction Module Interface
        //We have two General Interaction Modules here, it doesn't matter which one we point to, both share the same General Interaction Config object!
        public bool AdminOnly { get => _RangedHoldClickModule.AdminOnly; set => _RangedHoldClickModule.AdminOnly = value; }
        public bool EnableControllerVibrations { get => _RangedHoldClickModule.EnableControllerVibrations; set => _RangedHoldClickModule.EnableControllerVibrations = value; }
        public bool ShowTooltipsAndHighlight { get => _RangedHoldClickModule.ShowTooltipsAndHighlight; set => _RangedHoldClickModule.ShowTooltipsAndHighlight = value; }
        public bool IsInteractable { get => _RangedHoldClickModule.IsInteractable; set => _RangedHoldClickModule.IsInteractable = value; }
        #endregion
    }

    internal partial class V_HoldActivatableProviderStub : IRangedHoldClickInteractionModuleProvider, ICollideInteractionModuleProvider
    {
        #region Player Interfaces
        int ICollideInteractionModuleProvider.Layer => CommonUtils.DefaultLayer;
        ICollideInteractionModule ICollideInteractionModuleProvider.CollideInteractionModule => _Service.ColliderInteractionModule;
        IRangedInteractionModule IRangedInteractionModuleProvider.RangedInteractionModule => _Service.RangedClickInteractionModule;
        #endregion

        internal HoldActivatableService _Service = null;

        internal V_HoldActivatableProviderStub(HoldActivatableService service)
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
