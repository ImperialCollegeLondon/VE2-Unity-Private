using UnityEngine;
using System.Collections.Generic;
using VE2.Core.VComponents.API;
using VE2.Common.API;
using VE2.Common.Shared;
using UnityEngine.Events;

namespace VE2.Core.VComponents.Internal
{
    internal partial class V_Sliding2DAdjustable : IV_Sliding2DAdjustable
    {
        #region State Module Interface
        internal IAdjustable2DStateModule _Adjustable2DStateModule => _Service.Adjustable2DStateModule;
        internal IGrabbableStateModule _GrabbableStateModule => _Service.FreeGrabbableStateModule;

        public UnityEvent<Vector2> OnValueAdjusted { get; }/* => _Adjustable2DStateModule.OnValueAdjusted*/
        public UnityEvent OnGrab => _GrabbableStateModule.OnGrab;
        public UnityEvent OnDrop => _GrabbableStateModule.OnDrop;

        public bool IsGrabbed => _GrabbableStateModule.IsGrabbed;
        public bool IsLocallyGrabbed => _GrabbableStateModule.IsLocalGrabbed;
        public Vector2 Value => _Adjustable2DStateModule.OutputValue;
        public void SetValue(Vector2 value) => _Adjustable2DStateModule.SetOutputValue(value);
        public Vector2 MinimumOutputValue { get => _Adjustable2DStateModule.MinimumOutputValue; set => _Adjustable2DStateModule.MinimumOutputValue = value; }
        public Vector2 MaximumOutputValue { get => _Adjustable2DStateModule.MaximumOutputValue; set => _Adjustable2DStateModule.MaximumOutputValue = value; }

        public Vector2 MinimumSpatialValue { get => _Service.MinimumSpatialValue; set => _Service.MinimumSpatialValue = value; }
        public Vector2 MaximumSpatialValue { get => _Service.MaximumSpatialValue; set => _Service.MaximumSpatialValue = value; }
        public Vector2 SpatialValue { get   => _Service.SpatialValue; set => _Service.SpatialValue = value; }
        public int NumberOfValues { get => _Service.NumberOfValues; set => _Service.NumberOfValues = value; }

        public void SetMinimumAndMaximumSpatialValuesRange(Vector2 min, Vector2 max)
        {
            MinimumSpatialValue = min;
            MaximumSpatialValue = max;
        }

        public void SetMinimumAndMaximumOutputValuesRange(Vector2 min, Vector2 max)
        {
            MinimumOutputValue = min;
            MaximumOutputValue = max;
        }

        public IClientIDWrapper MostRecentGrabbingClientID => _GrabbableStateModule.MostRecentInteractingClientID;
        public IClientIDWrapper MostRecentAdjustingClientID => _Adjustable2DStateModule.MostRecentInteractingClientID;
        #endregion

        #region Ranged Interaction Module Interface
        internal IRangedAdjustable2DInteractionModule _RangedAdjustableModule => _Service.RangedAdjustable2DInteractionModule;
        public float InteractRange { get => _RangedAdjustableModule.InteractRange; set => _RangedAdjustableModule.InteractRange = value; }
        #endregion

        #region General Interaction Module Interface
        //We have two General Interaction Modules here, it doesn't matter which one we point to, both share the same General Interaction Config object!
        public bool AdminOnly { get => _RangedAdjustableModule.AdminOnly; set => _RangedAdjustableModule.AdminOnly = value; }
        public bool EnableControllerVibrations { get => _RangedAdjustableModule.EnableControllerVibrations; set => _RangedAdjustableModule.EnableControllerVibrations = value; }
        public bool ShowTooltipsAndHighlight { get => _RangedAdjustableModule.ShowTooltipsAndHighlight; set => _RangedAdjustableModule.ShowTooltipsAndHighlight = value; }
        public bool IsInteractable { get => _RangedAdjustableModule.IsInteractable; set => _RangedAdjustableModule.IsInteractable = value; }
        #endregion
    }

    [DisallowMultipleComponent]
    internal partial class V_Sliding2DAdjustable : MonoBehaviour, IRangedGrabInteractionModuleProvider
    {
        [SerializeField, IgnoreParent] private Sliding2DAdjustableConfig _config = new();
        [SerializeField, HideInInspector] private Adjustable2DState _adjustable2DState = new();
        [SerializeField, HideInInspector] private GrabbableState _freeGrabbableState = new();

        #region Player Interfaces
        IRangedInteractionModule IRangedInteractionModuleProvider.RangedInteractionModule => _Service.RangedAdjustable2DInteractionModule;
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

        private Sliding2DAdjustableService _service = null;
        private Sliding2DAdjustableService _Service
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
            _config.Adjustable2DStateConfig.MinimumOutputValue = new Vector2(
               Mathf.Min(_config.Adjustable2DStateConfig.MinimumOutputValue.x, _config.Adjustable2DStateConfig.MaximumOutputValue.x),
               Mathf.Min(_config.Adjustable2DStateConfig.MinimumOutputValue.y, _config.Adjustable2DStateConfig.MaximumOutputValue.y)
            );

            _config.Adjustable2DStateConfig.StartingOutputValue = new Vector2(
               Mathf.Clamp(_config.Adjustable2DStateConfig.StartingOutputValue.x, _config.Adjustable2DStateConfig.MinimumOutputValue.x, _config.Adjustable2DStateConfig.MaximumOutputValue.x),
               Mathf.Clamp(_config.Adjustable2DStateConfig.StartingOutputValue.y, _config.Adjustable2DStateConfig.MinimumOutputValue.y, _config.Adjustable2DStateConfig.MaximumOutputValue.y)
            );

            _config.Adjustable2DStateConfig.MaximumOutputValue = new Vector2(
               Mathf.Max(_config.Adjustable2DStateConfig.MinimumOutputValue.x, _config.Adjustable2DStateConfig.MaximumOutputValue.x),
               Mathf.Max(_config.Adjustable2DStateConfig.MinimumOutputValue.y, _config.Adjustable2DStateConfig.MaximumOutputValue.y)
            );

            _config.LinearAdjustableServiceConfig.MinimumSpatialValue = new Vector2(
               Mathf.Min(_config.LinearAdjustableServiceConfig.MinimumSpatialValue.x, _config.LinearAdjustableServiceConfig.MaximumSpatialValue.x),
               Mathf.Min(_config.LinearAdjustableServiceConfig.MinimumSpatialValue.y, _config.LinearAdjustableServiceConfig.MaximumSpatialValue.y)
            );

            _config.LinearAdjustableServiceConfig.MaximumSpatialValue = new Vector2(
               Mathf.Max(_config.LinearAdjustableServiceConfig.MinimumSpatialValue.x, _config.LinearAdjustableServiceConfig.MaximumSpatialValue.x),
               Mathf.Max(_config.LinearAdjustableServiceConfig.MinimumSpatialValue.y, _config.LinearAdjustableServiceConfig.MaximumSpatialValue.y)
            );
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

            _service = new Sliding2DAdjustableService(
                handheldInteractions,
                _config,
                _adjustable2DState,
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
