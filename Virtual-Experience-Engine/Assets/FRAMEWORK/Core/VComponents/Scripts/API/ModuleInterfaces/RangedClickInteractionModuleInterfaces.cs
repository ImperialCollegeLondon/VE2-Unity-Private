using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VE2.Core.VComponents.API
{
    internal interface IRangedClickInteractionModule : IRangedInteractionModule
    {
        public void ClickDown(InteractorID interactorID);
        public string ID { get; }
        public bool ActivateAtRangeInVR { get; }
    }

    internal interface IRangedToggleClickInteractionModule : IRangedClickInteractionModule 
    {

    }

    internal interface IRangedHoldClickInteractionModule : IRangedClickInteractionModule
    {
        public bool IsNetworked { get; }

        public void ClickUp(InteractorID interactorID);
    }
}