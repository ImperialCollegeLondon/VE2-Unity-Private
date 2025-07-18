using UnityEngine;
using System.Collections.Generic;
using VE2.Core.VComponents.API;
using VE2.Common.API;
using VE2.Common.Shared;
using UnityEngine.Events;

namespace VE2.Core.VComponents.Internal
{
    internal partial class V_SlidingAdjustable : IV_SlidingAdjustable
    {
        #region State Module Interface
        internal IAdjustableStateModule _AdjustableStateModule => _Service.AdjustableStateModule;
        internal IGrabbableStateModule _GrabbableStateModule => _Service.FreeGrabbableStateModule;

        public UnityEvent<float> OnValueAdjusted => _AdjustableStateModule.OnValueAdjusted;
        public UnityEvent OnGrab => _GrabbableStateModule.OnGrab;
        public UnityEvent OnDrop => _GrabbableStateModule.OnDrop;

        public bool IsGrabbed => _GrabbableStateModule.IsGrabbed;
        public bool IsLocallyGrabbed => _GrabbableStateModule.IsLocalGrabbed;
        public float Value => _AdjustableStateModule.OutputValue;
        public void SetValue(float value) => _AdjustableStateModule.SetOutputValue(value);
        public float MinimumOutputValue { get => _AdjustableStateModule.MinimumOutputValue; set => _AdjustableStateModule.MinimumOutputValue = value; }
        public float MaximumOutputValue { get => _AdjustableStateModule.MaximumOutputValue; set => _AdjustableStateModule.MaximumOutputValue = value; }

        public float MinimumSpatialValue { get => _Service.MinimumSpatialValue; set => _Service.MinimumSpatialValue = value; }
        public float MaximumSpatialValue { get => _Service.MaximumSpatialValue; set => _Service.MaximumSpatialValue = value; }
        public float SpatialValue { get => _Service.SpatialValue; set => _Service.SpatialValue = value; }
        public int NumberOfValues { get => _Service.NumberOfValues; set => _Service.NumberOfValues = value; }

        public void SetMinimumAndMaximumSpatialValuesRange(float min, float max)
        {
            MinimumSpatialValue = min;
            MaximumSpatialValue = max;
        }

        public void SetMinimumAndMaximumOutputValuesRange(float min, float max)
        {
            MinimumOutputValue = min;
            MaximumOutputValue = max;
        }

        public IClientIDWrapper MostRecentGrabbingClientID => _GrabbableStateModule.MostRecentInteractingClientID;
        public IClientIDWrapper MostRecentAdjustingClientID => _AdjustableStateModule.MostRecentInteractingClientID;
        #endregion

        #region Ranged Interaction Module Interface
        internal IRangedAdjustableInteractionModule _RangedAdjustableModule => _Service.RangedAdjustableInteractionModule;
        public float InteractRange { get => _RangedAdjustableModule.InteractRange; set => _RangedAdjustableModule.InteractRange = value; }
        #endregion

        #region General Interaction Module Interface
        //We have two General Interaction Modules here, it doesn't matter which one we point to, both share the same General Interaction Config object!
        public bool AdminOnly {get => _RangedAdjustableModule.AdminOnly; set => _RangedAdjustableModule.AdminOnly = value; }
        public bool EnableControllerVibrations { get => _RangedAdjustableModule.EnableControllerVibrations; set => _RangedAdjustableModule.EnableControllerVibrations = value; }
        public bool ShowTooltipsAndHighlight { get => _RangedAdjustableModule.ShowTooltipsAndHighlight; set => _RangedAdjustableModule.ShowTooltipsAndHighlight = value; }
        public bool IsInteractable { get => _RangedAdjustableModule.IsInteractable; set => _RangedAdjustableModule.IsInteractable = value; }
        #endregion
    }
    
    [DisallowMultipleComponent]
    internal partial class V_SlidingAdjustable : MonoBehaviour, IRangedGrabInteractionModuleProvider
    {
        [SerializeField, IgnoreParent] private SlidingAdjustableConfig _config = new();
        [SerializeField, HideInInspector] private AdjustableState _adjustableState = new();
        [SerializeField, HideInInspector] private GrabbableState _freeGrabbableState = new();

        #region Player Interfaces
        IRangedInteractionModule IRangedInteractionModuleProvider.RangedInteractionModule => _Service.RangedAdjustableInteractionModule;
        #endregion

        #region Inspector Utils
        internal Collider Collider
        {
            get
            {
                if (_config.RangedAdjustableInteractionConfig.AttachPointWrapper == null)
                    _config.RangedAdjustableInteractionConfig.AttachPointWrapper = new TransformWrapper(transform);
                return ((TransformWrapper)_config.RangedAdjustableInteractionConfig.AttachPointWrapper).Transform.GetComponent<Collider>();
            }
        }
        internal string AttachPointGOName => ((TransformWrapper)_config.RangedAdjustableInteractionConfig.AttachPointWrapper).GameObject.name;
        #endregion

        private SlidingAdjustableService _service = null;
        private SlidingAdjustableService _Service
        {
            get
            {
                if (_service == null)
                    OnEnable();
                return _service;
            }
        }

        private void OnValidate()
        {
            _config.AdjustableStateConfig.MinimumOutputValue = Mathf.Min(_config.AdjustableStateConfig.MinimumOutputValue, _config.AdjustableStateConfig.MaximumOutputValue);
            _config.AdjustableStateConfig.StartingOutputValue = Mathf.Clamp(_config.AdjustableStateConfig.StartingOutputValue, _config.AdjustableStateConfig.MinimumOutputValue, _config.AdjustableStateConfig.MaximumOutputValue);
            _config.AdjustableStateConfig.MaximumOutputValue = Mathf.Max(_config.AdjustableStateConfig.MinimumOutputValue, _config.AdjustableStateConfig.MaximumOutputValue);

            _config.LinearAdjustableServiceConfig.MinimumSpatialValue = Mathf.Min(_config.LinearAdjustableServiceConfig.MinimumSpatialValue, _config.LinearAdjustableServiceConfig.MaximumSpatialValue);
            _config.LinearAdjustableServiceConfig.MaximumSpatialValue = Mathf.Max(_config.LinearAdjustableServiceConfig.MinimumSpatialValue, _config.LinearAdjustableServiceConfig.MaximumSpatialValue);
        }

        private void OnEnable()
        {
            if (!Application.isPlaying || _service != null)
                return;

            string id = "LinearAdjustable-" + gameObject.name;

            if (_config.RangedAdjustableInteractionConfig.TransformToAdjust == null || ((TransformWrapper)_config.RangedAdjustableInteractionConfig.TransformToAdjust).Transform == null)
            {
                _config.RangedAdjustableInteractionConfig.TransformToAdjust = new TransformWrapper(transform);
                Debug.LogWarning($"The adjustable on {gameObject.name} does not have an assigned TransformToAdjust, and so may not behave as intended");
            }

            if (_config.RangedAdjustableInteractionConfig.AttachPointWrapper == null || ((TransformWrapper)_config.RangedAdjustableInteractionConfig.AttachPointWrapper).Transform == null)
            {
                _config.RangedAdjustableInteractionConfig.AttachPointWrapper = new TransformWrapper(transform);
                Debug.LogWarning($"The adjustable on {gameObject.name} does not have an assigned AttachPoint, and so may not behave as intended");
            }

            List<IHandheldInteractionModule> handheldInteractions = new();

            //TODO: THINK ABOUT THIS - do we want to allow adjustables to also have activatables on them?
            // if(TryGetComponent(out V_HandheldActivatable handheldActivatable))
            //     handheldInteractions.Add(handheldActivatable.HandheldClickInteractionModule);
            // if (TryGetComponent(out V_HandheldAdjustable handheldAdjustable))
            //     handheldInteractions.Add(handheldAdjustable.HandheldScrollInteractionModule);

            _service = new SlidingAdjustableService(
                handheldInteractions,
                _config,
                _adjustableState,
                _freeGrabbableState,
                id,
                VE2API.WorldStateSyncableContainer,
                VE2API.GrabInteractablesContainer,
                VE2API.InteractorContainer,
                VE2API.LocalClientIdWrapper);
        }

        private void Start() => _service.HandleStart();
        private void FixedUpdate() => _service.HandleFixedUpdate();

        private void OnDisable()
        {
            _service.TearDown();
            _service = null;
        }
    }
}
