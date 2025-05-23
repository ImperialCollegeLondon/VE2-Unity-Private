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
        [SerializeField] public DropBehaviour dropBehaviour = new();

        [SerializeField] public bool PreserveInspectModeOrientation = false;

        [Space(5)]
        [SerializeField] public bool AlignOrientationOnGrab = false;

        [Space(5)]
        public UnityEvent OnInspectModeEnter;

        [Space(5)]
        [EndGroup]
        public UnityEvent OnInspectModeExit;
    }
    
    internal class RangedFreeGrabInteractionModule : RangedGrabInteractionModule, IRangedFreeGrabInteractionModule
    {
        internal event Action<Vector3, Quaternion> OnGrabDeltaApplied;

        internal UnityEvent OnInspectModeEnter => _rangedFreeGrabInteractionConfig.OnInspectModeEnter;
        internal UnityEvent OnInspectModeExit => _rangedFreeGrabInteractionConfig.OnInspectModeExit;

        public bool PreserveInspectModeOrientation { get => _rangedFreeGrabInteractionConfig.PreserveInspectModeOrientation; set => _rangedFreeGrabInteractionConfig.PreserveInspectModeOrientation = value; }
        public bool AlignOrientationOnGrab { get => _rangedFreeGrabInteractionConfig.AlignOrientationOnGrab; set => _rangedFreeGrabInteractionConfig.AlignOrientationOnGrab = value; }
        public DropBehaviour DropBehaviour { get => _rangedFreeGrabInteractionConfig.dropBehaviour; set => _rangedFreeGrabInteractionConfig.dropBehaviour = value; }

        private readonly RangedFreeGrabInteractionConfig _rangedFreeGrabInteractionConfig;

        public void NotifyInspectModeEnter()
        {
            OnInspectModeEnter?.Invoke();
        }

        public void SetInspectModeExit()
        {
            OnInspectModeExit?.Invoke();
        }

        public RangedFreeGrabInteractionModule(string id, IGrabInteractablesContainer grabInteractablesContainer, ITransformWrapper transform, List<IHandheldInteractionModule> handheldInteractions,
            RangedFreeGrabInteractionConfig rangedFreeGrabInteractionConfig, GeneralInteractionConfig generalInteractionConfig)
            : base(id, grabInteractablesContainer, transform, handheldInteractions, rangedFreeGrabInteractionConfig, generalInteractionConfig)
        {
            _rangedFreeGrabInteractionConfig = rangedFreeGrabInteractionConfig;
        }

        public void ApplyDeltaWhenGrabbed(Vector3 deltaPostion, Quaternion deltaRotation)
        {
            OnGrabDeltaApplied?.Invoke(deltaPostion, deltaRotation);
        }
    }
}
