using UnityEngine;
using VE2.Common.API;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    internal class V_HandheldActivatable : MonoBehaviour, IV_HandheldActivatable
    {
        [SerializeField, HideLabel, IgnoreParent] private HandheldActivatableConfig _config = new(); 
        [SerializeField, HideInInspector] private SingleInteractorActivatableState _state = new();        

        #region Plugin Interfaces
        ISingleInteractorActivatableStateModule IV_HandheldActivatable._StateModule => _Service.StateModule;
        IHandheldClickInteractionModule IV_HandheldActivatable._HandheldClickModule => _Service.HandheldClickInteractionModule;
        #endregion

        #region Player Interfaces
        internal IHandheldClickInteractionModule HandheldClickInteractionModule => _Service.HandheldClickInteractionModule;   
        #endregion

        private HandheldActivatableService _service = null;
        private HandheldActivatableService _Service
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

            string id = "HHActivatable-" + gameObject.name;
            _service = new(_config, _state, id, VE2API.WorldStateSyncableContainer, VComponentsAPI.ActivatableGroupsContainer, VE2API.LocalClientIdWrapper);
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

