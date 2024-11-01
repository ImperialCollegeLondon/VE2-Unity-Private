

namespace ViRSE.Core.VComponents.InternalInterfaces
{
    public interface IGeneralInteractionModuleImplementor
    {
        public IGeneralInteractionModule GeneralInteractionModule { get; }
    }

    public interface IGeneralInteractionModule
    {
        public bool AdminOnly { get; set; }
    }
}