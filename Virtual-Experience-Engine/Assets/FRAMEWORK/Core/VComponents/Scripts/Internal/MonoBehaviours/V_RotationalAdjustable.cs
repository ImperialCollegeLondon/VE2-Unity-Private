using UnityEngine;
using System.Collections.Generic;
using VE2.Core.VComponents.API;
using VE2.Common.API;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.Internal
{
    internal class V_RotationalAdjustable : MonoBehaviour, IV_RotationalAdjustable, IRangedGrabInteractionModuleProvider
    {
        [SerializeField, HideLabel, IgnoreParent] private RotationalAdjustableConfig _config = new();
        [SerializeField, HideInInspector] private AdjustableState _adjustableState = null;
        [SerializeField, HideInInspector] private GrabbableState _freeGrabbableState = new();

        #region Plugin Interfaces
        IAdjustableStateModule IV_RotationalAdjustable._AdjustableStateModule => _Service.AdjustableStateModule;
        IGrabbableStateModule IV_RotationalAdjustable._GrabbableStateModule => _Service.GrabbableStateModule;
        IRangedAdjustableInteractionModule IV_RotationalAdjustable._RangedAdjustableModule => _Service.RangedAdjustableInteractionModule;
        #endregion

        #region Player Interfaces
        IRangedInteractionModule IRangedInteractionModuleProvider.RangedInteractionModule => _Service.RangedAdjustableInteractionModule;
        #endregion

        #region Inspector Utils
        internal Collider Collider 
        {
            get 
            {
                if (_config.InteractionConfig.AttachPoint == null)
                    _config.InteractionConfig.AttachPoint = transform;
                return _config.InteractionConfig.AttachPoint.GetComponent<Collider>();
            }
        }
        internal string AttachPointGOName => _config.InteractionConfig.AttachPoint.name;
        #endregion

        public float MinimumSpatialValue { get => _Service.MinimumSpatialValue; set => _Service.MinimumSpatialValue = value; }
        public float MaximumSpatialValue { get => _Service.MaximumSpatialValue; set => _Service.MaximumSpatialValue = value; }
        public float SpatialValue { get => _Service.SpatialValue; set => _Service.SpatialValue = value; }
        public int NumberOfValues { get => _Service.NumberOfValues; set => _Service.NumberOfValues = value; }

        private RotationalAdjustableService _service = null;
        private RotationalAdjustableService _Service
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

            _config.RotationalAdjustableServiceConfig.MinimumSpatialValue = Mathf.Min(_config.RotationalAdjustableServiceConfig.MinimumSpatialValue, _config.RotationalAdjustableServiceConfig.MaximumSpatialValue);
            _config.RotationalAdjustableServiceConfig.MaximumSpatialValue = Mathf.Max(_config.RotationalAdjustableServiceConfig.MinimumSpatialValue, _config.RotationalAdjustableServiceConfig.MaximumSpatialValue);
        }

        private void OnEnable()
        {
            if (!Application.isPlaying || _service != null)
                return;

            string id = "RotationalAdjustable-" + gameObject.name;

            if(_adjustableState == null)
                _adjustableState = new AdjustableState(float.MaxValue);  
            
            List<IHandheldInteractionModule> handheldInteractions = new(); 

            //TODO: THINK ABOUT THIS
            // if(TryGetComponent(out V_HandheldActivatable handheldActivatable))
            //     handheldInteractions.Add(handheldActivatable.HandheldClickInteractionModule);
            // if (TryGetComponent(out V_HandheldAdjustable handheldAdjustable))
            //     handheldInteractions.Add(handheldAdjustable.HandheldScrollInteractionModule);

            _service = new RotationalAdjustableService(
                new TransformWrapper(GetComponent<Transform>()),
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
