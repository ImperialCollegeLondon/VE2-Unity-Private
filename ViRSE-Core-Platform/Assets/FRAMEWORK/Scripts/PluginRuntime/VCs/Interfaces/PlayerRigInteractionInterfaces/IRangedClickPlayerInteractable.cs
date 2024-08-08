using System.Collections;
using System.Collections.Generic;

namespace ViRSE.PluginRuntime.VComponents
{
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