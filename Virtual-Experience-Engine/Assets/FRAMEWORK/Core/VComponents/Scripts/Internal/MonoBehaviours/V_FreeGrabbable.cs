using System;
using UnityEngine;
using System.Collections.Generic;
using VE2.Core.VComponents.API;
using Unity.Collections;
using VE2.Common.API;
using UnityEngine.Events;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.Internal
{
    internal partial class V_FreeGrabbable : IV_FreeGrabbable
    {
        #region State Module Interface
        internal IGrabbableStateModule _StateModule => _Service.StateModule;

        public UnityEvent OnGrab => _StateModule.OnGrab;
        public UnityEvent OnDrop => _StateModule.OnDrop;

        public bool IsGrabbed { get { return _StateModule.IsGrabbed; } }
        public IClientIDWrapper MostRecentInteractingClientID => _StateModule.MostRecentInteractingClientID;
        #endregion

        #region Ranged Interaction Module Interface
        internal IRangedGrabInteractionModule _RangedGrabModule => _Service.RangedGrabInteractionModule;
        public float InteractRange { get => _RangedGrabModule.InteractRange; set => _RangedGrabModule.InteractRange = value; }
        #endregion

        #region General Interaction Module Interface
        //We have two General Interaction Modules here, it doesn't matter which one we point to, both share the same General Interaction Config object!
        public bool AdminOnly {get => _RangedGrabModule.AdminOnly; set => _RangedGrabModule.AdminOnly = value; }
        public bool EnableControllerVibrations { get => _RangedGrabModule.EnableControllerVibrations; set => _RangedGrabModule.EnableControllerVibrations = value; }
        public bool ShowTooltipsAndHighlight { get => _RangedGrabModule.ShowTooltipsAndHighlight; set => _RangedGrabModule.ShowTooltipsAndHighlight = value; }
        public bool IsInteractable { get => _RangedGrabModule.IsInteractable; set => _RangedGrabModule.IsInteractable = value; }
        #endregion
    }

    [ExecuteAlways]
    internal partial class V_FreeGrabbable : MonoBehaviour, IRangedGrabInteractionModuleProvider, IGrabbableRigidbody
    {
        [SerializeField, IgnoreParent] private FreeGrabbableConfig _config = new();
        [SerializeField, HideInInspector] private GrabbableState _state = new();

        #region Player Interfaces
        IRangedInteractionModule IRangedInteractionModuleProvider.RangedInteractionModule => _Service.RangedGrabInteractionModule;
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
        private FreeGrabbableService _Service
        {
            get
            {
                if (_service == null)
                    OnEnable();
                return _service;
            }
        }

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

        //Bit of a bodge to allow FreeGrabbables to add RigidBodySyncables without tying the 
        private const string RigidBodySyncableFullName = "VE2.NonCore.Instancing.Internal.V_RigidbodySyncable"; 
        private const string RigidBodySyncableAssemblyName = "VE2.NonCore.Instancing.Internal"; 

        void Reset()
        {
            TryAddRigidBodySyncable();
        }

        private void TryAddRigidBodySyncable()
        {
            Type syncableType = Type.GetType($"{RigidBodySyncableFullName}, {RigidBodySyncableAssemblyName}");

            if (syncableType == null)
            {
                Debug.LogWarning($"Could not automatically add {RigidBodySyncableFullName} to {gameObject.name}. If you want this gameobject's rigidbody to be synced, please add a {RigidBodySyncableFullName} component manually.");
                return;
            }

            if (GetComponent(syncableType) == null)
                gameObject.AddComponent(syncableType);
        }

        private void Awake()
        {
            if (_config.RangedFreeGrabInteractionConfig.AttachPointWrapper == null)
                _config.RangedFreeGrabInteractionConfig.AttachPointWrapper = new TransformWrapper(transform);

            if (Application.isPlaying)
                return;

            if (GetComponent<Rigidbody>() == null)
                gameObject.AddComponent<Rigidbody>();

            if (GetComponent<Collider>() == null)
                VComponentUtils.CreateCollider(gameObject);
        }

        private void OnEnable()
        {
            if (!Application.isPlaying || _service != null)
                return;

            string id = "FreeGrabbable-" + gameObject.name;

            if (_config.RangedFreeGrabInteractionConfig.AttachPointWrapper == null || ((TransformWrapper)_config.RangedFreeGrabInteractionConfig.AttachPointWrapper).Transform == null)
            {
                _config.RangedFreeGrabInteractionConfig.AttachPointWrapper = new TransformWrapper(transform);
                Debug.LogWarning($"The adjustable on {gameObject.name} does not have an assigned AttachPoint, and so may not behave as intended");
            }

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