using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VE2.Core.VComponents.API
{
    internal interface IRangedGrabInteractionModule : IRangedInteractionModule
    {
        public void RequestLocalGrab(InteractorID interactorID);
        public void RequestLocalDrop(InteractorID interactorID);
        public List<IHandheldInteractionModule> HandheldInteractions { get; }
    }
}