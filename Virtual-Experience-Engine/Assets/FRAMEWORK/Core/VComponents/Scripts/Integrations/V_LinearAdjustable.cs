using UnityEngine;
using VE2.Common;
using VE2.Core.VComponents.NonInteractableInterfaces;
using VE2.Core.VComponents.InteractableFindables;
using VE2.Core.VComponents.PluginInterfaces;
using VE2.Core.VComponents.InteractableInterfaces;
using VE2.Core.VComponents.Internal;
using System.Collections.Generic;

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

        private LinearAdjustableService _service = null;
        private TransformWrapper _transformWrapper = null;

        private void OnEnable()
        {
            string id = "LinearAdjustable-" + gameObject.name;

            if(_adjustableState == null)
                _adjustableState = new AdjustableState(_config.adjustableStateConfig.StartingValue);  
            
            List<IHandheldInteractionModule> handheldInteractions = new(); 

            //TODO: THINK ABOUT THIS
            // if(TryGetComponent(out V_HandheldActivatable handheldActivatable))
            //     handheldInteractions.Add(handheldActivatable.HandheldClickInteractionModule);
            // if (TryGetComponent(out V_HandheldAdjustable handheldAdjustable))
            //     handheldInteractions.Add(handheldAdjustable.HandheldScrollInteractionModule);

            _transformWrapper = new(GetComponent<Transform>());

            _service = new LinearAdjustableService(
                transform,
                handheldInteractions,
                _config,
                _adjustableState,
                _freeGrabbableState,
                id,
                VE2CoreServiceLocator.Instance.WorldStateModulesContainer,
                VE2CoreServiceLocator.Instance.InteractorContainer,
                _transformWrapper);
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
