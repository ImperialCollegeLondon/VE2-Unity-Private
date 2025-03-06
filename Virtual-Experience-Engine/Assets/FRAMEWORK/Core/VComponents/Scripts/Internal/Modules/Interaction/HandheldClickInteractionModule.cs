using System;
using UnityEngine;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    internal class HandheldClickInteractionModule : GeneralInteractionModule, IHandheldClickInteractionModule
    {
        public void Click(ushort clientID)
        {
            OnClickDown?.Invoke(clientID);
        }

        public event Action<ushort> OnClickDown;

        public HandheldClickInteractionModule(GeneralInteractionConfig config) : base(config) { }
    }

}

