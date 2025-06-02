using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    internal class HandheldClickInteractionModule : GeneralInteractionModule, IHandheldClickInteractionModule
    {
        public bool IsHoldMode { get; private set; } //Detects if it is Toggle or Hold mode
        public bool DeactivateOnDrop { get; private set; } //If true, the activatable will deactivate when the handheld is dropped

        public void ClickDown(ushort clientID)
        {
            OnClickDown?.Invoke(clientID);
        }

        public void ClickUp(ushort clientID)
        {
            OnClickUp?.Invoke(clientID);
        }

        public event Action<ushort> OnClickDown;
        public event Action<ushort> OnClickUp;

        public HandheldClickInteractionModule(IV_FreeGrabbable grabbable, HandHeldClickInteractionConfig handheldClickInteractionConfig, GeneralInteractionConfig config) : base(config)
        {
            IsHoldMode = handheldClickInteractionConfig.IsHoldMode;
            DeactivateOnDrop = handheldClickInteractionConfig.DeactivateOnDrop;
        } 
    }

}

