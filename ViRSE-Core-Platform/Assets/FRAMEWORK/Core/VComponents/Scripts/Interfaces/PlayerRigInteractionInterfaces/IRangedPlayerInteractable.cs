using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ViRSE.Core.VComponents
{
    public interface IRangedPlayerInteractableIntegrator : IGeneralPlayerInteractableIntegrator
    {
        public IRangedPlayerInteractable RangedPlayerInteractable => RangedPlayerInteractableImplementor.RangedPlayerInteractable;
        protected IRangedPlayerInteractableImplementor RangedPlayerInteractableImplementor { get; }
    }

    public interface IRangedPlayerInteractableImplementor : IGeneralPlayerInteractableImplementor
    {
        public IRangedPlayerInteractable RangedPlayerInteractable { get; } 
    }

    public interface IRangedPlayerInteractable : IGeneralPlayerInteractable
    {
        public float InteractRange { get; }
    }
}
