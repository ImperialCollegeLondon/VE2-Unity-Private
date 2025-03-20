using System;
using UnityEngine;
using VE2.Core.VComponents.API;
using static VE2.Core.Common.CommonSerializables;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class ToggleActivatableConfig
    {
        [SerializeField, IgnoreParent] public ActivatableStateConfig StateConfig = new();
        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();
        [SerializeField, IgnoreParent] public RangedInteractionConfig RangedInteractionConfig = new();
    }

    internal class ToggleActivatableService
    {
        #region Interfaces
        public ISingleInteractorActivatableStateModule StateModule => _StateModule;
        public IRangedClickInteractionModule RangedClickInteractionModule => _RangedClickInteractionModule;
        public ICollideInteractionModule ColliderInteractionModule => _ColliderInteractionModule;
        #endregion

        #region Modules
        private readonly SingleInteractorActivatableStateModule _StateModule;
        private readonly RangedClickInteractionModule _RangedClickInteractionModule;
        private readonly ColliderInteractionModule _ColliderInteractionModule;
        #endregion

        private readonly string _activationGroupID = "None";
        private readonly bool _isInActivationGroup = false;     
        internal bool test = false;

        public ToggleActivatableService(ToggleActivatableConfig config, VE2Serializable state, string id, IWorldStateSyncService worldStateSyncService, ActivatableGroupsContainer activatableGroupsContainer)
        {
            _StateModule = new(state, config.StateConfig, id, worldStateSyncService,activatableGroupsContainer);
            _RangedClickInteractionModule = new(config.RangedInteractionConfig, config.GeneralInteractionConfig);
            _ColliderInteractionModule = new(config.GeneralInteractionConfig);
            _activationGroupID = config.StateConfig.ActivationGroupID;   

            if (_activationGroupID != "None")
            {
                VComponentsAPI.ActivatableGroupsContainer.RegisterActivatable(_activationGroupID, _StateModule);
                _isInActivationGroup = true;
            }
            else
            {
                _isInActivationGroup = false;
            }

            _RangedClickInteractionModule.OnClickDown += HandleInteract;
            _ColliderInteractionModule.OnCollideEnter += HandleInteract;
        }

        public void HandleFixedUpdate()
        {
            _StateModule.HandleFixedUpdate();
        }

        private void HandleInteract(ushort clientID)
        {
            Debug.Log($"HandleInteract {_isInActivationGroup} with {_activationGroupID}");
            if (_isInActivationGroup)
                VComponentsAPI.ActivatableGroupsContainer.ActivateGroup(_activationGroupID, _StateModule);

            _StateModule.InvertState(clientID);
        }

        public void TearDown() 
        {
            VComponentsAPI.ActivatableGroupsContainer.DeregisterActivatable(_activationGroupID, _StateModule); 
            _StateModule.TearDown();
        }
    }
}
