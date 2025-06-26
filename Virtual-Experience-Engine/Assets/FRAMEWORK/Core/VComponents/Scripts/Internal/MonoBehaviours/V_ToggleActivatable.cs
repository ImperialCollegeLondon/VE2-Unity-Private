using System;
using UnityEngine;
using UnityEngine.Events;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    internal partial class V_ToggleActivatable : IV_ToggleActivatable
    {
        #region State Module Interface
        internal ISingleInteractorActivatableStateModule _StateModule => _Service.StateModule;

        public UnityEvent OnActivate => _StateModule.OnActivate;
        public UnityEvent OnDeactivate => _StateModule.OnDeactivate;

        public bool IsActivated => _StateModule.IsActivated;
        public void Activate() => _StateModule.Activate();
        public void Deactivate() => _StateModule.Deactivate();
        public void SetActivated(bool isActivated) => _StateModule.SetActivated(isActivated);
        public IClientIDWrapper MostRecentInteractingClientID => _StateModule.MostRecentInteractingClientID;

        public void SetNetworked(bool isNetworked) => _StateModule.SetNetworked(isNetworked);
        #endregion

        #region Ranged Interaction Module Interface
        internal IRangedToggleClickInteractionModule _RangedToggleClickModule => _Service.RangedClickInteractionModule;
        public float InteractRange { get => _RangedToggleClickModule.InteractRange; set => _RangedToggleClickModule.InteractRange = value; }
        #endregion

        #region General Interaction Module Interface
        //We have two General Interaction Modules here, it doesn't matter which one we point to, both share the same General Interaction Config object!
        public bool AdminOnly {get => _RangedToggleClickModule.AdminOnly; set => _RangedToggleClickModule.AdminOnly = value; }
        public bool EnableControllerVibrations { get => _RangedToggleClickModule.EnableControllerVibrations; set => _RangedToggleClickModule.EnableControllerVibrations = value; }
        public bool ShowTooltipsAndHighlight { get => _RangedToggleClickModule.ShowTooltipsAndHighlight; set => _RangedToggleClickModule.ShowTooltipsAndHighlight = value; }
        public bool IsInteractable { get => _RangedToggleClickModule.IsInteractable; set => _RangedToggleClickModule.IsInteractable = value; }
        #endregion
    }

    [ExecuteAlways]
    internal partial class V_ToggleActivatable : MonoBehaviour, IRangedInteractionModuleProvider, ICollideInteractionModuleProvider
    {
        internal ToggleActivatableConfig Config { get => _config; set { _config = value; } }
        [SerializeField, IgnoreParent] private ToggleActivatableConfig _config = new();
        [SerializeField, HideInInspector] private SingleInteractorActivatableState _state = new();

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
        
        private ToggleActivatableService _service = null;
        private ToggleActivatableService _Service
        {
            get
            {
                if (_service == null)
                    OnEnable();
                return _service;
            }
        }

        private void Awake()
        {
            if (Application.isPlaying)
                return;

            if (GetComponent<Collider>() == null)
                VComponentUtils.CreateCollider(gameObject);
        }

        private void OnEnable()
        {
            if (!Application.isPlaying || _service != null)
                return;

            string id = "Activatable-" + gameObject.name;
            _service = new ToggleActivatableService(_config, _state, id, VE2API.WorldStateSyncableContainer, VE2API.ActivatableGroupsContainer, VE2API.LocalClientIdWrapper);
        }

        private void FixedUpdate()
        {
            _service?.HandleFixedUpdate();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;

            _service?.TearDown();
            _service = null;
        }
    }
}