using UnityEngine;
using System.Collections.Generic;
using VE2.Common.TransformWrapper;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    public class V_RotationalAdjustable : MonoBehaviour, IV_RotationalAdjustable, IRangedGrabInteractionModuleProvider
    {
        [SerializeField, HideLabel, IgnoreParent] private RotationalAdjustableConfig _config = new();
        [SerializeField, HideInInspector] private AdjustableState _adjustableState = null;
        [SerializeField, HideInInspector] private GrabbableState _freeGrabbableState = new();

        #region Plugin Interfaces
        IAdjustableStateModule IV_RotationalAdjustable._AdjustableStateModule => _service.AdjustableStateModule;
        IGrabbableStateModule IV_RotationalAdjustable._GrabbableStateModule => _service.GrabbableStateModule;
        IRangedAdjustableInteractionModule IV_RotationalAdjustable._RangedAdjustableModule => _service.RangedAdjustableInteractionModule;
        #endregion

        #region Player Interfaces
        IRangedInteractionModule IRangedInteractionModuleProvider.RangedInteractionModule => _service.RangedAdjustableInteractionModule;
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

        public float MinimumSpatialValue { get => _service.MinimumSpatialValue; set => _service.MinimumSpatialValue = value; }
        public float MaximumSpatialValue { get => _service.MaximumSpatialValue; set => _service.MaximumSpatialValue = value; }
        public float SpatialValue { get => _service.SpatialValue; set => _service.SpatialValue = value; }
        public int NumberOfValues { get => _service.NumberOfValues; set => _service.NumberOfValues = value; }

        private RotationalAdjustableService _service = null;

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
                VComponentsAPI.WorldStateSyncService,
                VComponentsAPI.InteractorContainer);
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
