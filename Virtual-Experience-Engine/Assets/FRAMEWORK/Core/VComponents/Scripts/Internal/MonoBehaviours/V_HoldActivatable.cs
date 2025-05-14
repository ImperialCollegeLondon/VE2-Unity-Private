using UnityEngine;
using VE2.Common.API;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    internal class V_HoldActivatable : MonoBehaviour, IV_HoldActivatable, IRangedInteractionModuleProvider, ICollideInteractionModuleProvider
    {
        [SerializeField, HideLabel, IgnoreParent] private HoldActivatableConfig _config = new();
        [SerializeField, HideInInspector] private MultiInteractorActivatableState _state = new();

        #region Plugin Interfaces
        IMultiInteractorActivatableStateModule IV_HoldActivatable._StateModule => _service.StateModule;
        IRangedHoldClickInteractionModule IV_HoldActivatable._RangedHoldClickModule => _service.RangedClickInteractionModule;
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

        private HoldActivatableService _service = null;

        private void OnEnable()
        {
            string id = "HoldActivatable-" + gameObject.name;
            _service = new HoldActivatableService(_config, _state, id, VE2API.LocalClientIdWrapper);
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
