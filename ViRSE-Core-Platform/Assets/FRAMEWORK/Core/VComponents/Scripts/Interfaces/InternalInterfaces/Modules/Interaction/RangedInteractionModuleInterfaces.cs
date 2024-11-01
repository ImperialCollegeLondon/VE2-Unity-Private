using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViRSE.Core.VComponents.InternalInterfaces;

namespace ViRSE.Core.VComponents.InternalInterfaces
{
    public interface IRangedInteractionModuleImplementor : IGeneralInteractionModuleImplementor
    {
        public IRangedInteractionModule RangedInteractionModule { get; } 
    }

    public interface IRangedInteractionModule : IGeneralInteractionModule
    {
        public float InteractRange { get; set; }
    }
}