using UnityEngine;
using VE2.Common.API;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    internal class V_PressurePlate : MonoBehaviour, IV_PressurePlate, ICollideInteractionModuleProvider
    {
        [SerializeField, HideLabel, IgnoreParent] private PressurePlateConfig _config = new();
        [SerializeField, HideInInspector] private MultiInteractorActivatableState _state = new();

        #region Plugin Interfaces
        IMultiInteractorActivatableStateModule IV_PressurePlate._StateModule => _Service.StateModule;
        ICollideInteractionModule IV_PressurePlate._ColliderModule => _Service.ColliderInteractionModule;
        #endregion

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
            _service = new PressurePlateService(_config, _state, id, VE2API.LocalClientIdWrapper);
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
