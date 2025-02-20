

namespace VE2.Core.VComponents.API
{
    public interface IRangedGrabInteractionModuleProvider : IRangedInteractionModuleProvider
    {
        public IRangedGrabInteractionModule RangedGrabInteractionModule => (IRangedGrabInteractionModule)RangedInteractionModule;
    }
}