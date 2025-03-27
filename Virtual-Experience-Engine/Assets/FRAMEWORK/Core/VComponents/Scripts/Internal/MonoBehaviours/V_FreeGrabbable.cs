using System;
using UnityEngine;
using System.Collections.Generic;
using VE2.Common.TransformWrapper;
using VE2.Core.VComponents.API;
using Unity.Collections;

namespace VE2.Core.VComponents.Internal
{
    [RequireComponent(typeof(MeshFilter))]
    [ExecuteAlways]
    internal class V_FreeGrabbable : MonoBehaviour, IV_FreeGrabbable, IRangedGrabInteractionModuleProvider
    {
        // [Help("TestHelp", UnityMessageType.Error, ApplyCondition = true)]
        // [SerializeField, ShowDisabledIf(nameof(_showError), true)] private bool Test;

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
        public Collider Collider 
        {
            get 
            {
                if (_collider == null)
                    _collider = GetComponent<Collider>();
                return _collider;
            }
        }
        [SerializeField, HideInInspector] private Collider _collider = null;

        public Rigidbody Rigidbody
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
        private TransformWrapper _transformWrapper = null;

        private void Awake()
        {
            if (_config.StateConfig.AttachPoint == null)
                _config.StateConfig.AttachPoint = transform;

            if (Application.isPlaying)
                return;

            if (GetComponent<Rigidbody>() == null)
                gameObject.AddComponent<Rigidbody>();

            if (GetComponent<Collider>() == null)
            {
                Collider collider;
                if (gameObject.name.ToUpper().Contains("CUBE") || gameObject.name.ToUpper().Contains("BOX"))
                    collider = gameObject.AddComponent<BoxCollider>();
                else if (gameObject.name.ToUpper().Contains("SPHERE") || gameObject.name.ToUpper().Contains("BALL"))
                    collider =gameObject.AddComponent<SphereCollider>();
                else
                {
                    collider = gameObject.AddComponent<MeshCollider>();
                    ((MeshCollider)collider).convex = true;
                }

                collider.isTrigger = false;
            }
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
                VComponentsAPI.WorldStateSyncService,
                VComponentsAPI.InteractorContainer,
                _rigidbodyWrapper,
                Resources.Load<PhysicsConstants>("PhysicsConstants"));
        }

        private void FixedUpdate()
        {
            _service?.HandleFixedUpdate();            
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;

            _service.TearDown();
            _service = null;
        }
    }
}