using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    internal partial class V_HoldActivatable : MonoBehaviour, IV_HoldActivatable
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

    internal partial class V_HoldActivatable : MonoBehaviour, IRangedInteractionModuleProvider, ICollideInteractionModuleProvider
    {
        [SerializeField, IgnoreParent] private HoldActivatableConfig _config = new();
        [SerializeField, HideInInspector] private MultiInteractorActivatableSyncedState _state = new();

        #region Player Interfaces
        ICollideInteractionModule ICollideInteractionModuleProvider.CollideInteractionModule => _Service.ColliderInteractionModule;
        IRangedInteractionModule IRangedInteractionModuleProvider.RangedInteractionModule => _Service.RangedClickInteractionModule;
        #endregion

        #region Inspector Utils
        internal Collider Collider
        {
            get
            {
                if (_collider == null)
                    _collider = GetComponent<Collider>();
                return _collider;
            }
        }
        [SerializeField, HideInInspector] private Collider _collider = null;
        #endregion

        private HoldActivatableService _service = null;
        private HoldActivatableService _Service
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

            string id = "HoldActivatable-" + gameObject.name;
            _service = new HoldActivatableService(_config, _state, id, VE2API.LocalClientIdWrapper, VE2API.WorldStateSyncableContainer);
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
