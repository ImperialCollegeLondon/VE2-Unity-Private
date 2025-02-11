
using VE2.Core.VComponents.InteractableInterfaces;

namespace VE2.Core.VComponents.InteractableFindables
{
    public interface IRangedClickPlayerInteractableIntegrator : IRangedPlayerInteractableIntegrator
    {
        public IRangedClickInteractionModule RangedClickInteractionModule => (IRangedClickInteractionModule)RangedInteractionModule;
    }
}