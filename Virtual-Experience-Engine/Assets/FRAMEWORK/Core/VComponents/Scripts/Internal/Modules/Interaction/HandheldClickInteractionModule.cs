using System;
using UnityEngine;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    internal class HandheldClickInteractionModule : GeneralInteractionModule, IHandheldClickInteractionModule
    {
        public bool IsHoldMode { get; set; } //Detects if it is Toggle or Hold mode

        public void Click(ushort clientID)
        {
            OnClickDown?.Invoke(clientID);
        }

        public void ClickUp(ushort clientID)
        {
            if (IsHoldMode)
                OnClickUp?.Invoke(clientID);
        }

        public event Action<ushort> OnClickDown;
        public event Action<ushort> OnClickUp;

        public HandheldClickInteractionModule(GeneralInteractionConfig config) : base(config) { }
    }

}

