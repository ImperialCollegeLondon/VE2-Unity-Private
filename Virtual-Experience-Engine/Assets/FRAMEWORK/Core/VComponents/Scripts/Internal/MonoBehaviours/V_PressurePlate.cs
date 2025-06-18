using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    internal partial class V_PressurePlate : IV_PressurePlate
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

        #endregion
    }

    internal partial class V_PressurePlate : MonoBehaviour, IV_PressurePlate, ICollideInteractionModuleProvider
    {
        [SerializeField, IgnoreParent] private PressurePlateConfig _config = new();
        [SerializeField, HideInInspector] private MultiInteractorActivatableSyncedState _state = new();

        #region Player Interfaces
        ICollideInteractionModule ICollideInteractionModuleProvider.CollideInteractionModule => _Service.ColliderInteractionModule;
        #endregion

        private PressurePlateService _service = null;
        private PressurePlateService _Service
        {
            get
            {
                if (_service == null)
                    OnEnable();
                return _service;
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying || _service != null)
                return;

            string id = "PressurePlate-" + gameObject.name;
            _service = new PressurePlateService(_config, _state, id, VE2API.LocalClientIdWrapper, VE2API.WorldStateSyncableContainer);
        }

        private void FixedUpdate()
        {
            _service.HandleFixedUpdate();
        }

        private void OnDisable()
        {
            _service.TearDown();
            _service = null;
        }
    }
}
