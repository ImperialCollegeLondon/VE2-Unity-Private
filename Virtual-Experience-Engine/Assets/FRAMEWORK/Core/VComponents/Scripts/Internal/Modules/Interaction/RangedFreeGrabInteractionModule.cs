using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using VE2.Core.VComponents.API;
using UnityEngine.Events;
using VE2.Common.API;
using VE2.Common.Shared;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class RangedFreeGrabInteractionConfig : RangedGrabInteractionConfig
    {
        [BeginGroup(Style = GroupStyle.Round)]
        [Space(5)]
        [Title("Ranged Free Grab Interaction Settings")]
        [SerializeField, PropertyOrder(-100)] public DropBehaviour DropBehaviour = DropBehaviour.KeepMomentum;
        [SerializeField, PropertyOrder(-99)] public bool AlignOrientationOnGrab = true;

        [SerializeField, PropertyOrder(-98)] public bool PreserveInspectModeOrientation = false;

        [Space(5)]
        [SerializeField, PropertyOrder(-97)] public UnityEvent OnLocalInspectModeEnter;

        [Space(5)]
        [EndGroup]
        [SerializeField, PropertyOrder(-96)] public UnityEvent OnLocalInspectModeExit;
    }
    
    internal class RangedFreeGrabInteractionModule : RangedGrabInteractionModule, IRangedFreeGrabInteractionModule
    {
        internal event Action<Vector3, Quaternion> OnGrabDeltaApplied;

        internal UnityEvent OnInspectModeEnter => _rangedFreeGrabInteractionConfig.OnLocalInspectModeEnter;
        internal UnityEvent OnInspectModeExit => _rangedFreeGrabInteractionConfig.OnLocalInspectModeExit;

        public bool PreserveInspectModeOrientation { get => _rangedFreeGrabInteractionConfig.PreserveInspectModeOrientation; set => _rangedFreeGrabInteractionConfig.PreserveInspectModeOrientation = value; }
        public bool AlignOrientationOnGrab { get => _rangedFreeGrabInteractionConfig.AlignOrientationOnGrab; set => _rangedFreeGrabInteractionConfig.AlignOrientationOnGrab = value; }
        public DropBehaviour DropBehaviour { get => _rangedFreeGrabInteractionConfig.DropBehaviour; set => _rangedFreeGrabInteractionConfig.DropBehaviour = value; }

        IColliderWrapper IRangedFreeGrabInteractionModule.ColliderWrapper => ColliderWrapper;

        public readonly IColliderWrapper ColliderWrapper; 
        private readonly RangedFreeGrabInteractionConfig _rangedFreeGrabInteractionConfig;

        public void NotifyInspectModeEnter()
        {
            OnInspectModeEnter?.Invoke();
        }

        public void SetInspectModeExit()
        {
            OnInspectModeExit?.Invoke();
        }

        public RangedFreeGrabInteractionModule(string id, IGrabInteractablesContainer grabInteractablesContainer, List<IHandheldInteractionModule> handheldInteractions,
            RangedFreeGrabInteractionConfig rangedFreeGrabInteractionConfig, GeneralInteractionConfig generalInteractionConfig, IColliderWrapper colliderWrapper)
            : base(id, grabInteractablesContainer, handheldInteractions, rangedFreeGrabInteractionConfig, generalInteractionConfig)
        {
            _rangedFreeGrabInteractionConfig = rangedFreeGrabInteractionConfig;
            ColliderWrapper = colliderWrapper;
        }

        public void ApplyDeltaWhenGrabbed(Vector3 deltaPostion, Quaternion deltaRotation)
        {
            OnGrabDeltaApplied?.Invoke(deltaPostion, deltaRotation);
        }
    }
}
