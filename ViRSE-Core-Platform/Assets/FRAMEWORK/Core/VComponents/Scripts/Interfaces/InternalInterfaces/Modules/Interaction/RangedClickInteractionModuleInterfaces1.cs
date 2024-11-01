using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ViRSE.Core.VComponents.InternalInterfaces
{
    // public interface IRangedClickPlayerInteractable : IRangedInteractionModuleIntegrator
    // {
    //     public void Click(ushort clientID) => ((IRangedClickInteractionModule)Implementor.Module).Click(clientID);
    // }

    public interface IRangedClickInteractionModuleImplementor : IRangedInteractionModuleImplementor
    {
        
    }

    public interface IRangedClickInteractionModule : IRangedInteractionModule
    {
        public void Click(ushort clientID);
    }
}