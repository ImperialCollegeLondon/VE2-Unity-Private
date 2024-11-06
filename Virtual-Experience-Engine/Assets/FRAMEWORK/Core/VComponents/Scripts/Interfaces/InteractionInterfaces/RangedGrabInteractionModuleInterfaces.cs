using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VE2.Core.VComponents.InteractableInterfaces
{
    public interface IRangedGrabInteractionModule : IRangedInteractionModule
    {
        public void LocalInteractorGrab(InteractorID interactorID);
        public void LocalInteractorDrop(InteractorID interactorID);
    }
}