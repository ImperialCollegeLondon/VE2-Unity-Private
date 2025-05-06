using System;
using UnityEngine;
using VE2.Common.API;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    [ExecuteAlways]
    internal class V_ToggleActivatable : MonoBehaviour, IV_ToggleActivatable, IRangedInteractionModuleProvider, ICollideInteractionModuleProvider
    {
        internal ToggleActivatableConfig Config { get => _config; set { _config = value; }}
        [SerializeField, HideLabel, IgnoreParent] private ToggleActivatableConfig _config = new();
        [SerializeField, HideInInspector] private SingleInteractorActivatableState _state = new();

        #region Plugin Interfaces
        ISingleInteractorActivatableStateModule IV_ToggleActivatable._StateModule => _service.StateModule;
        IRangedToggleClickInteractionModule IV_ToggleActivatable._RangedToggleClickModule => _service.RangedClickInteractionModule;
        #endregion

        #region Player Interfaces
        ICollideInteractionModule ICollideInteractionModuleProvider.CollideInteractionModule => _service.ColliderInteractionModule;
        IRangedInteractionModule IRangedInteractionModuleProvider.RangedInteractionModule => _service.RangedClickInteractionModule;
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

        private void Awake()
        {
            if (Application.isPlaying)
                return;

            if (GetComponent<Collider>() == null)
                VComponentUtils.CreateCollider(gameObject);
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
                return;

            string id = "Activatable-" + gameObject.name;
            _service = new ToggleActivatableService(_config, _state, id, VE2API.WorldStateSyncableContainer, VComponentsAPI.ActivatableGroupsContainer, VE2API.LocalClientIdWrapper);
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