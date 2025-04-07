using UnityEngine;
using System.Collections.Generic;
using VE2.Common.TransformWrapper;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    public class V_LinearAdjustable : MonoBehaviour, IV_LinearAdjustable, IRangedGrabInteractionModuleProvider
    {
        [SerializeField, HideLabel, IgnoreParent] private LinearAdjustableConfig _config = new();
        [SerializeField, HideInInspector] private AdjustableState _adjustableState = null;
        [SerializeField, HideInInspector] private GrabbableState _freeGrabbableState = new();

        #region Plugin Interfaces
        IAdjustableStateModule IV_LinearAdjustable._AdjustableStateModule => _service.AdjustableStateModule;
        IGrabbableStateModule IV_LinearAdjustable._GrabbableStateModule => _service.FreeGrabbableStateModule;
        IRangedAdjustableInteractionModule IV_LinearAdjustable._RangedAdjustableModule => _service.RangedAdjustableInteractionModule;
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

        private LinearAdjustableService _service = null;

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
            string id = "LinearAdjustable-" + gameObject.name;

            if(_adjustableState == null)
                _adjustableState = new AdjustableState(float.MaxValue);  
            
            List<IHandheldInteractionModule> handheldInteractions = new(); 

            //TODO: THINK ABOUT THIS - do we want to allow adjustables to also have activatables on them?
            // if(TryGetComponent(out V_HandheldActivatable handheldActivatable))
            //     handheldInteractions.Add(handheldActivatable.HandheldClickInteractionModule);
            // if (TryGetComponent(out V_HandheldAdjustable handheldAdjustable))
            //     handheldInteractions.Add(handheldAdjustable.HandheldScrollInteractionModule);

            _service = new LinearAdjustableService(
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
