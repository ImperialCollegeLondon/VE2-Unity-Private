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
        IMultiInteractorActivatableStateModule IV_PressurePlate._StateModule => _service.StateModule;
        ICollideInteractionModule IV_PressurePlate._ColliderModule => _service.ColliderInteractionModule;
        #endregion

        #region Player Interfaces
        ICollideInteractionModule ICollideInteractionModuleProvider.CollideInteractionModule => _service.ColliderInteractionModule;
        #endregion

        private PressurePlateService _service = null;

        private void OnEnable()
        {
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
