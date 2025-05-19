using UnityEngine;
using UnityEngine.Events;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    internal partial class V_HandheldAdjustable : IV_HandheldAdjustable
    {
        #region State Module Interface
        internal IAdjustableStateModule _StateModule => _Service.StateModule;

        public UnityEvent<float> OnValueAdjusted => _StateModule.OnValueAdjusted;
        public float Value => _StateModule.OutputValue;
        public void SetValue(float value) => _StateModule.SetOutputValue(value);
        public float MinimumValue { get => _StateModule.MinimumOutputValue; set => _StateModule.MinimumOutputValue = value; }
        public float MaximumValue { get => _StateModule.MaximumOutputValue; set => _StateModule.MaximumOutputValue = value; }
        public IClientIDWrapper MostRecentInteractingClientID => _StateModule.MostRecentInteractingClientID;
        #endregion

        #region Handheld Interaction Module Interface
        internal IHandheldScrollInteractionModule _HandheldScrollModule => _Service.HandheldScrollInteractionModule;
        #endregion

        #region General Interaction Module Interface
        public bool AdminOnly { get => _HandheldScrollModule.AdminOnly; set => _HandheldScrollModule.AdminOnly = value; }
        public bool EnableControllerVibrations { get => _HandheldScrollModule.EnableControllerVibrations; set => _HandheldScrollModule.EnableControllerVibrations = value; }
        public bool ShowTooltipsAndHighlight { get => _HandheldScrollModule.ShowTooltipsAndHighlight; set => _HandheldScrollModule.ShowTooltipsAndHighlight = value; }
        #endregion
    }

    internal partial class V_HandheldAdjustable : MonoBehaviour
    {
        [SerializeField, HideLabel, IgnoreParent] private HandheldAdjustableConfig _config = new();
        [SerializeField, HideInInspector] private AdjustableState _state = null;

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

