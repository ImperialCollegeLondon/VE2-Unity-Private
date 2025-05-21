using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using VE2.Core.VComponents.API;
using UnityEngine.Events;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    public class RangedFreeGrabInteractionConfig
    {
        [BeginGroup(Style = GroupStyle.Round)]
        [Space(5)]
        [Title("Ranged Free Grab Interaction Settings")]
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

        private readonly RangedFreeGrabInteractionConfig _rangedFreeGrabInteractionConfig;
        public RangedFreeGrabInteractionModule(List<IHandheldInteractionModule> handheldInteractions, RangedInteractionConfig config, GeneralInteractionConfig generalInteractionConfig, RangedFreeGrabInteractionConfig rangedFreeGrabInteractionConfig) : base(handheldInteractions, config, generalInteractionConfig)
        {
            _rangedFreeGrabInteractionConfig = rangedFreeGrabInteractionConfig;
        }

        public void SetInspectModeEnter()
        {
            OnInspectModeEnter?.Invoke();
        }

        public void SetInspectModeExit()
        {
            OnInspectModeExit?.Invoke();
        }

        public void ApplyDeltaWhenGrabbed(Vector3 deltaPostion, Quaternion deltaRotation)
        {
            OnGrabDeltaApplied?.Invoke(deltaPostion, deltaRotation);
        }
    }
}
