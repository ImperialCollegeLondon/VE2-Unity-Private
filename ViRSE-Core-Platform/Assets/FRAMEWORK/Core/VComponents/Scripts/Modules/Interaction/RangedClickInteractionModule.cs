
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using ViRSE.Core.VComponents.InternalInterfaces;

namespace ViRSE.Core.VComponents.PluginInterfaces
{
    internal class RangedClickInteractionModule : RangedInteractionModule, IRangedClickInteractionModule
    {

        public void Click(ushort clientID)
        {
            //only happens if is valid click
            OnClickDown?.Invoke(clientID);
        }

        public event Action<ushort> OnClickDown;

        public RangedClickInteractionModule(RangedInteractionConfig rangedConfig, GeneralInteractionConfig generalConfig) : base(rangedConfig, generalConfig) { }  
    }
}