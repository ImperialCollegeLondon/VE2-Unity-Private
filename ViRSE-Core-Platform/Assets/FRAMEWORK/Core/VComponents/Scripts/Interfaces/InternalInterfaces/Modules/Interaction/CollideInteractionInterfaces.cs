using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViRSE.Core.VComponents.InternalInterfaces;

namespace ViRSE.Core.VComponents.InternalInterfaces
{
    // public interface IRangedInteractionModuleIntegrator
    // {
    //     public IRangedInteractionModuleImplementor Implementor { get; } 
    // }

    public interface ICollideInteractionModuleImplementor : IGeneralInteractionModuleImplementor
    {
        public ICollideInteractionModule CollideInteractionModule { get; } 
    }

    public interface ICollideInteractionModule : IGeneralInteractionModule
    {
        public void InvokeOnCollideEnter(ushort clientID);
    }
}