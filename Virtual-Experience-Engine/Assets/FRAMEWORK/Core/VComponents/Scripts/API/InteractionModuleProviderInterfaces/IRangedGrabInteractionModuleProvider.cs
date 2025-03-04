

namespace VE2.Core.VComponents.API
{
    internal interface IRangedGrabInteractionModuleProvider : IRangedInteractionModuleProvider
    {
        public IRangedGrabInteractionModule RangedGrabInteractionModule => (IRangedGrabInteractionModule)RangedInteractionModule;
    }
}
