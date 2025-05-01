using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VE2.Core.VComponents.API
{
    internal interface IRangedGrabInteractionModule : IRangedInteractionModule
    {
        public void RequestLocalGrab(InteractorID interactorID);
        public void RequestLocalDrop(InteractorID interactorID);
        public void SetInspectModeEnter(Transform grabberTransform);
        public void SetInspectModeExit(Transform grabberTransform);
        public List<IHandheldInteractionModule> HandheldInteractions { get; }
    }
}