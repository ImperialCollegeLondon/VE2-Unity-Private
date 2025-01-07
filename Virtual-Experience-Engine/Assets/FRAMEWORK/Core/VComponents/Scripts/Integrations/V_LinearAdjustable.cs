using UnityEngine;
using VE2.Common;
using VE2.Core.VComponents.NonInteractableInterfaces;
using VE2.Core.VComponents.InteractableFindables;
using VE2.Core.VComponents.PluginInterfaces;
using VE2.Core.VComponents.InteractableInterfaces;
using VE2.Core.VComponents.Internal;
using System.Collections.Generic;
using VE2.Common.TransformWrapper;

namespace VE2.Core.VComponents.Integration
{
    public class V_LinearAdjustable : MonoBehaviour, IV_LinearAdjustable, IRangedGrabPlayerInteractableIntegrator
    {
        [SerializeField, HideLabel, IgnoreParent] private LinearAdjustableConfig _config = new();
        [SerializeField, HideInInspector] private AdjustableState _adjustableState = null;
        [SerializeField, HideInInspector] private FreeGrabbableState _freeGrabbableState = new();

        #region Plugin Interfaces
        IAdjustableStateModule IV_LinearAdjustable._AdjustableStateModule => _service.AdjustableStateModule;
        IFreeGrabbableStateModule IV_LinearAdjustable._FreeGrabbableStateModule => _service.FreeGrabbableStateModule;
        IRangedAdjustableInteractionModule IV_LinearAdjustable._RangedAdjustableModule => _service.RangedAdjustableInteractionModule;
        #endregion

        #region Player Interfaces
        IRangedInteractionModule IRangedPlayerInteractableIntegrator.RangedInteractionModule => _service.RangedAdjustableInteractionModule;
        #endregion

        public float MinimumOutputValue { get => _service.MinimumOutputValue; set => _service.MinimumOutputValue = value; }
        public float MaximumOutputValue { get => _service.MaximumOutputValue; set => _service.MaximumOutputValue = value; }
        public float OutputValue { get => _service.OutputValue; set => _service.OutputValue = value; }
        public int NumberOfValues { get => _service.NumberOfValues; set => _service.NumberOfValues = value; }

        private LinearAdjustableService _service = null;

        private void OnEnable()
        {
            string id = "LinearAdjustable-" + gameObject.name;

            if(_adjustableState == null)
                _adjustableState = new AdjustableState(_config.AdjustableStateConfig.StartingValue);  
            
            List<IHandheldInteractionModule> handheldInteractions = new(); 

            //TODO: THINK ABOUT THIS
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
                VE2CoreServiceLocator.Instance.WorldStateModulesContainer,
                VE2CoreServiceLocator.Instance.InteractorContainer
                );
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
