using UnityEngine;
using System.Collections.Generic;
using VE2.Core.VComponents.API;
using VE2.Common.API;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.Internal
{
    internal class V_LinearAdjustable : MonoBehaviour, IV_LinearAdjustable, IRangedGrabInteractionModuleProvider
    {
        [SerializeField, HideLabel, IgnoreParent] private LinearAdjustableConfig _config = new();
        [SerializeField, HideInInspector] private AdjustableState _adjustableState = null;
        [SerializeField, HideInInspector] private GrabbableState _freeGrabbableState = new();

        #region Plugin Interfaces
        IAdjustableStateModule IV_LinearAdjustable._AdjustableStateModule => _Service.AdjustableStateModule;
        IGrabbableStateModule IV_LinearAdjustable._GrabbableStateModule => _Service.FreeGrabbableStateModule;
        IRangedAdjustableInteractionModule IV_LinearAdjustable._RangedAdjustableModule => _Service.RangedAdjustableInteractionModule;
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

        private LinearAdjustableService _service = null;
        private LinearAdjustableService _Service
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
