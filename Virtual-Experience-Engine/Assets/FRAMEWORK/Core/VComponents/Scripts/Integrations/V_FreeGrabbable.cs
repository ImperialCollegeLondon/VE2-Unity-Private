using System;
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
    internal class V_FreeGrabbable : MonoBehaviour, IV_FreeGrabbable, IRangedGrabPlayerInteractableIntegrator
    {
        [SerializeField, HideLabel, IgnoreParent] private FreeGrabbableConfig _config = new();
        [SerializeField, HideInInspector] private FreeGrabbableState _state = new();

        #region Plugin Interfaces     
        IFreeGrabbableStateModule IV_FreeGrabbable._StateModule => _service.StateModule;
        IRangedGrabInteractionModule IV_FreeGrabbable._RangedGrabModule => _service.RangedGrabInteractionModule;
        #endregion

        #region Player Interfaces
        IRangedInteractionModule IRangedPlayerInteractableIntegrator.RangedInteractionModule => _service.RangedGrabInteractionModule;
        #endregion

        private FreeGrabbableService _service = null;
        private RigidbodyWrapper _rigidbodyWrapper = null;
        private void OnEnable()
        {
            string id = "FreeGrabbable-" + gameObject.name;

            List<IHandheldInteractionModule> handheldInteractions = new(); 

            if(TryGetComponent(out V_HandheldActivatable handheldActivatable))
                handheldInteractions.Add(handheldActivatable.HandheldClickInteractionModule);
            if (TryGetComponent(out V_HandheldAdjustable handheldAdjustable))
                handheldInteractions.Add(handheldAdjustable.HandheldScrollInteractionModule);

            _rigidbodyWrapper = new(GetComponent<Rigidbody>());
            _service = new FreeGrabbableService(
                handheldInteractions,
                _config, 
                _state, 
                id, 
                VE2CoreServiceLocator.Instance.WorldStateModulesContainer,
                VE2CoreServiceLocator.Instance.InteractorContainer,
                _rigidbodyWrapper,
                Resources.Load<PhysicsConstants>("PhysicsConstants"),
                transform);
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