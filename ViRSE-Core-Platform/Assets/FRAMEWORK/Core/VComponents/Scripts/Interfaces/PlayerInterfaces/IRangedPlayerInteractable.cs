using ViRSE.Core.VComponents.InternalInterfaces;

namespace ViRSE.Core.VComponents.PlayerInterfaces
{
    public interface IRangedPlayerInteractable : IGeneralPlayerInteractable
    {
        public IRangedInteractionModuleImplementor RangedModuleImplementor { get; }
        public float InteractRange { get => RangedModuleImplementor.RangedInteractionModule.InteractRange;}
    }
}
