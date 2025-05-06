using System;
using UnityEngine;
using System.Collections.Generic;
using VE2.Core.VComponents.API;
using Unity.Collections;
using VE2.Common.API;

namespace VE2.Core.VComponents.Internal
{
    [RequireComponent(typeof(MeshFilter))]
    [ExecuteAlways]
    internal class V_FreeGrabbable : MonoBehaviour, IV_FreeGrabbable, IRangedGrabInteractionModuleProvider, IGrabbableRigidbody
    {
        [SerializeField, HideLabel, IgnoreParent] private FreeGrabbableConfig _config = new();
        [SerializeField, HideInInspector] private GrabbableState _state = new();

        #region Plugin Interfaces     
        IGrabbableStateModule IV_FreeGrabbable._StateModule => _service.StateModule;
        IRangedGrabInteractionModule IV_FreeGrabbable._RangedGrabModule => _service.RangedGrabInteractionModule;
        #endregion

        #region Player Interfaces
        IRangedInteractionModule IRangedInteractionModuleProvider.RangedInteractionModule => _service.RangedGrabInteractionModule;
        #endregion

        #region Inspector Utils
        internal Collider Collider 
        {
            get 
            {
                if (_collider == null)
                    _collider = GetComponent<Collider>();
                return _collider;
            }
        }
        [SerializeField, HideInInspector] private Collider _collider = null;

        internal Rigidbody Rigidbody
        {
            get
            {
                if (_rigidbody == null)
                    _rigidbody = GetComponent<Rigidbody>();
                return _rigidbody;
            }
        }
        [SerializeField, HideInInspector] private Rigidbody _rigidbody = null;
        #endregion

        private FreeGrabbableService _service = null;
        private RigidbodyWrapper _rigidbodyWrapper = null;

        private Action<ushort> _internalOnGrab;
        event Action<ushort> IGrabbableRigidbody.InternalOnGrab
        {
            add { _internalOnGrab += value; }
            remove {  _internalOnGrab -= value; }
        }

        public event Action<ushort> InternalOnDrop;

        private bool _freeGrabbableHandlesKinematics = true;
        public bool FreeGrabbableHandlesKinematics { get => _freeGrabbableHandlesKinematics; set => _freeGrabbableHandlesKinematics = value; }

        private void Awake()
        {
            if (_config.InteractionConfig.AttachPoint == null)
                _config.InteractionConfig.AttachPoint = transform;

            if (Application.isPlaying)
                return;

            if (GetComponent<Rigidbody>() == null)
                gameObject.AddComponent<Rigidbody>();

            if (GetComponent<Collider>() == null)
                VComponentUtils.CreateCollider(gameObject);
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
                return;

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
                VE2API.WorldStateSyncableContainer,
                VE2API.GrabInteractablesContainer,
                VE2API.InteractorContainer,
                _rigidbodyWrapper,
                Resources.Load<PhysicsConstants>("PhysicsConstants"),
                (IGrabbableRigidbody)this,
                VE2API.LocalClientIdWrapper);

            _service.OnGrabConfirmed += HandleGrabConfirmed;
            _service.OnDropConfirmed += HandleDropConfirmed;
        }

        private void FixedUpdate()
        {
            _service?.HandleFixedUpdate();            
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;
                
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
            InternalOnDrop?.Invoke(grabberID);
        }

    }
}