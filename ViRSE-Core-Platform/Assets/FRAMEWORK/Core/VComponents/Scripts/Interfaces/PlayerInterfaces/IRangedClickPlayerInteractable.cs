using ViRSE.Core.VComponents.InternalInterfaces;

namespace ViRSE.Core.VComponents.PlayerInterfaces
{
    public interface IRangedClickPlayerInteractable : IRangedPlayerInteractable
    {
        public void Click(ushort clientID) => ((IRangedClickInteractionModule)RangedModuleImplementor.RangedInteractionModule).Click(clientID);
    }
}