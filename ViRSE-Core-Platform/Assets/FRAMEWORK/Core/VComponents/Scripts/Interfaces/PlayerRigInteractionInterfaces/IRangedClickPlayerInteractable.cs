using System.Collections;
using System.Collections.Generic;

namespace ViRSE.Core.VComponents
{
    public interface IRangedClickPlayerInteractableIntegrator : IRangedPlayerInteractableIntegrator
    {
        public IRangedClickPlayerInteractableImplementor RangedClickPlayerInteractableImplementor { get; }

        public void InvokeOnClickDown(InteractorID interactorID)
        {
            RangedClickPlayerInteractableImplementor.InvokeOnClickDown(interactorID);
        }
    }

    public interface IRangedClickPlayerInteractableImplementor : IRangedPlayerInteractableImplementor
    {
        public IRangedClickPlayerInteractable RangedClickPlayerInteractable { get; }

        public void InvokeOnClickDown(InteractorID interactorID)
        {
            RangedClickPlayerInteractable.InvokeOnClickDown(interactorID);
        }
    }

    public interface IRangedClickPlayerInteractable : IRangedPlayerInteractable
    {
        public void InvokeOnClickDown(InteractorID interactorID);
    }
}