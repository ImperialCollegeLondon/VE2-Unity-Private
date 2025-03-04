using UnityEngine;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Internal;

namespace VE2.Core.VComponents.Integration
{
    internal class V_HandheldActivatable : MonoBehaviour, IV_HandheldActivatable
    {
        [SerializeField, HideLabel, IgnoreParent] private HandheldActivatableConfig _config = new(); 
        [SerializeField, HideInInspector] private SingleInteractorActivatableState _state = new();        

        #region Plugin Interfaces
        ISingleInteractorActivatableStateModule IV_HandheldActivatable._StateModule => _service.StateModule;
        IHandheldClickInteractionModule IV_HandheldActivatable._HandheldClickModule => _service.HandheldClickInteractionModule;
        #endregion

        internal IHandheldClickInteractionModule HandheldClickInteractionModule => _service.HandheldClickInteractionModule;   
        private HandheldActivatableService _service = null;

        private void OnEnable()
        {
            string id = "HHActivatable-" + gameObject.name; 
            _service = new(_config, _state, id, VComponentsAPI.HasMultiPlayerSupport? VComponentsAPI.WorldStateSyncService : null);
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

