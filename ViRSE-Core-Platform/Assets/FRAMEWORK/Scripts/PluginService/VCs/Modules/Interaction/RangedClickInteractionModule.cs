
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ViRSE.VComponents
{
    public class RangedClickInteractionModule : RangedInteractionModule, IRangedClickPlayerInteractable
    {
        public UnityEvent<InteractorID> OnClickDown { get; private set; } = new UnityEvent<InteractorID>();

        public void InvokeOnClickDown(InteractorID interactorID)
        {
            //only happens if is valid click
            OnClickDown.Invoke(interactorID);
        }
    }
}