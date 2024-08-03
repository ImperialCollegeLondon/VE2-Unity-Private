using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ViRSE.VComponents
{
    public class GrabbableInteractionModule : MonoBehaviour
    {
        [VerticalGroup("GrabbableInteractionModule_VGroup")]
        [SerializeField] protected Transform attachPoint = null;

        [VerticalGroup("GrabbableInteractionModule_VGroup")]
        [HideIf("@CheckIfProgrammaticAdjustable()")]
        public UnityEvent OnGrabbed;

        [VerticalGroup("GrabbableInteractionModule_VGroup")]
        public UnityEvent OnDropped;

        [FoldoutGroup("Grabbable Settings V Group/Grabbable Settings")]
        [Tooltip("Allows users to grab the grabbable without their ray actually pointing at it  - helps inexperienced users! " +
    "On grab, if the ray isn't currently pointing at the grabbable, perform a proxity check, with a multiplied snap range. " +
    "If the grabbable is within this larger range, the ray will snap to it, and user will grab the grabbable" +
    " Set to 1 to turn this feature off")]
        [Range(1f, 2f)]
        [SerializeField] private float failsafeGrabMultiplier = 1.5f;
    }
}