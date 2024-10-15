
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ViRSE.Core.VComponents
{
    public class RangedClickInteractionModule : RangedInteractionModule, IRangedClickPlayerInteractable
    {
        #region Player Rig Interfaces
        public void InvokeOnClickDown(InteractorID interactorID)
        {
            //only happens if is valid click
            OnClickDown?.Invoke(interactorID);
        }
        #endregion

        public event Action<InteractorID> OnClickDown;

        public RangedClickInteractionModule(RangedInteractionConfig config) : base(config) { }  
    }
}