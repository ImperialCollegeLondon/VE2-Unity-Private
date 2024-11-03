
using VIRSE.Core.VComponents.InteractableInterfaces;

namespace ViRSE.Core.VComponents.RaycastInterfaces
{
    public interface IRangedClickPlayerInteractableIntegrator : IRangedPlayerInteractableIntegrator
    {
        public IRangedClickInteractionModule RangedClickInteractionModule => (IRangedClickInteractionModule)RangedInteractionModule;
    }
}