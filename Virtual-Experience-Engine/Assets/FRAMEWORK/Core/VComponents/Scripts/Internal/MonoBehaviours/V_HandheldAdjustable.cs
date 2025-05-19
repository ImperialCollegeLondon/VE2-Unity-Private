using UnityEngine;
using VE2.Common.API;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    internal class V_HandheldAdjustable : MonoBehaviour, IV_HandheldAdjustable
    {
        [SerializeField, HideLabel, IgnoreParent] private HandheldAdjustableConfig _config = new();
        [SerializeField, HideInInspector] private AdjustableState _state = null;

        #region Plugin Interfaces
        IAdjustableStateModule IV_HandheldAdjustable._StateModule => _Service.StateModule;
        IHandheldScrollInteractionModule IV_HandheldAdjustable._HandheldScrollModule => _Service.HandheldScrollInteractionModule;
        #endregion

        #region Player Interfaces
        internal IHandheldScrollInteractionModule HandheldScrollInteractionModule => _Service.HandheldScrollInteractionModule;
        #endregion

        private HandheldAdjustableService _service = null;
        private HandheldAdjustableService _Service
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

            string id = "HHAdjustable-" + gameObject.name;
            if (_state == null)
                _state = new AdjustableState(float.MaxValue);

            _service = new(_config, _state, id, VE2API.WorldStateSyncableContainer, VE2API.LocalClientIdWrapper);
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

