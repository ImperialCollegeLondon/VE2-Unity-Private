using System;
using UnityEngine;
using VE2.Core.VComponents.Internal;
using System.Collections.Generic;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    internal class V_FreeGrabbable : MonoBehaviour, IV_FreeGrabbable, IRangedGrabInteractionModuleProvider, IGrabbableRigidbody
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

        private Action<ushort> _internalOnGrab;
        event Action<ushort> IGrabbableRigidbody.InternalOnGrab
        {
            add { _internalOnGrab += value; }
            remove {  _internalOnGrab -= value; }
        }

        private Action<ushort> _internalOnDrop;
        event Action<ushort> IGrabbableRigidbody.InternalOnDrop
        {
            add { _internalOnDrop += value; }
            remove { _internalOnDrop -= value; }
        }

        private bool _freeGrabbableHandlesKinematics = true;
        public bool FreeGrabbableHandlesKinematics { get => _freeGrabbableHandlesKinematics; set => _freeGrabbableHandlesKinematics = value; }

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
                Resources.Load<PhysicsConstants>("PhysicsConstants"),
                (IGrabbableRigidbody)this);

            _service.OnGrabConfirmed += HandleGrabConfirmed;
            _service.OnDropConfirmed += HandleDropConfirmed;
        }

        private void FixedUpdate()
        {
            _service.HandleFixedUpdate();            
        }

        private void OnDisable()
        {
            _service.OnGrabConfirmed -= HandleGrabConfirmed;
            _service.OnDropConfirmed -= HandleDropConfirmed;

            _service.TearDown();
            _service = null;
        }

        private void HandleGrabConfirmed(ushort grabberID)
        {
            _internalOnGrab?.Invoke(grabberID);
        }

        private void HandleDropConfirmed(ushort grabberID)
        {
            _internalOnDrop?.Invoke(grabberID);
        }

    }
}