using VE2.Core.VComponents.InteractableInterfaces;

namespace VE2.Core.VComponents.RaycastInterfaces
{
    public interface IRangedPlayerInteractableIntegrator //: IGeneralPlayerInteractable
    {
        public IRangedInteractionModule RangedInteractionModule { get; }
    }
}
