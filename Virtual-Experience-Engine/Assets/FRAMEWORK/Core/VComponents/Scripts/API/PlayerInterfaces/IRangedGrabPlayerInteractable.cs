
using VE2.Core.VComponents.InteractableInterfaces;

namespace VE2.Core.VComponents.InteractableFindables
{
    public interface IRangedGrabPlayerInteractableIntegrator : IRangedPlayerInteractableIntegrator
    {
        public IRangedGrabInteractionModule RangedGrabInteractionModule => (IRangedGrabInteractionModule)RangedInteractionModule;
    }
}