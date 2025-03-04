using UnityEngine;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    internal class V_HandheldAdjustable : MonoBehaviour, IV_HandheldAdjustable
    {
        [SerializeField, HideLabel, IgnoreParent] private HandheldAdjustableConfig _config = new();
        [SerializeField, HideInInspector] private AdjustableState _state = null;

        #region Plugin Interfaces
        IAdjustableStateModule IV_HandheldAdjustable._StateModule => _service.StateModule;
        IHandheldScrollInteractionModule IV_HandheldAdjustable._HandheldScrollModule => _service.HandheldScrollInteractionModule;
        #endregion

        internal IHandheldScrollInteractionModule HandheldScrollInteractionModule => _service.HandheldScrollInteractionModule;
        private HandheldAdjustableService _service = null;

        private void OnEnable()
        {
            string id = "HHAdjustable-" + gameObject.name;
            if (_state == null)
                _state = new AdjustableState(_config.StateConfig.StartingValue);

            _service = new(_config, _state, id, VComponentsAPI.WorldStateSyncService);
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

