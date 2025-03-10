using System;
using UnityEngine;
using UnityEngine.Events;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class HoldActivatableConfig
    {
        [SerializeField, IgnoreParent] public HoldActivatableStateConfig StateConfig = new();
        [SpaceArea(spaceAfter: 10), SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();
        [SerializeField, IgnoreParent] public RangedInteractionConfig RangedInteractionConfig = new();
    }

    internal class HoldActivatableService
    {
        #region Interfaces
        public IMultiInteractorActivatableStateModule StateModule => _StateModule;
        public IRangedClickInteractionModule RangedClickInteractionModule => _RangedClickInteractionModule;
        public ICollideInteractionModule ColliderInteractionModule => _ColliderInteractionModule;
        #endregion

        #region Modules
        private readonly MultiInteractorActivatableStateModule _StateModule;
        private readonly RangedClickInteractionModule _RangedClickInteractionModule;
        private readonly ColliderInteractionModule _ColliderInteractionModule;
        #endregion

        public HoldActivatableService(HoldActivatableConfig config, MultiInteractorActivatableState state, string id)
        {
            _StateModule = new(state, config.StateConfig, id);
            _RangedClickInteractionModule = new(config.RangedInteractionConfig, config.GeneralInteractionConfig);
            _ColliderInteractionModule = new(config.GeneralInteractionConfig);

            _RangedClickInteractionModule.OnClickDown += SetStateToActive;
            _RangedClickInteractionModule.OnClickUp += SetStateToInactive;
            
            _ColliderInteractionModule.OnCollideEnter += SetStateToActive;
            _ColliderInteractionModule.OnCollideExit += SetStateToInactive;
        }

        public void HandleFixedUpdate()
        {
            _StateModule.HandleFixedUpdate();
        }

        private void SetStateToActive(ushort clientID)
        {
            _StateModule.SetState(clientID, true);
        }

        private void SetStateToInactive(ushort clientID)
        {
            _StateModule.SetState(clientID, false);
        }

        public void TearDown() 
        {
            _StateModule.TearDown();
        }
    }
}