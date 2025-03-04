using System;
using UnityEngine;
using VE2.Core.VComponents.Internal;
using System.Collections.Generic;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Integration
{
    internal class V_FreeGrabbable : MonoBehaviour, IV_FreeGrabbable, IRangedGrabInteractionModuleProvider
    {
        [SerializeField, HideLabel, IgnoreParent] private FreeGrabbableConfig _config = new();
        [SerializeField, HideInInspector] private FreeGrabbableState _state = new();

        #region Plugin Interfaces     
        IFreeGrabbableStateModule IV_FreeGrabbable._StateModule => _service.StateModule;
        IRangedGrabInteractionModule IV_FreeGrabbable._RangedGrabModule => _service.RangedGrabInteractionModule;
        #endregion

        #region Player Interfaces
        IRangedInteractionModule IRangedInteractionModuleProvider.RangedInteractionModule => _service.RangedGrabInteractionModule;
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
                VComponentsAPI.WorldStateSyncService,
                VComponentsAPI.InteractorContainer,
                _rigidbodyWrapper,
                Resources.Load<PhysicsConstants>("PhysicsConstants"));
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