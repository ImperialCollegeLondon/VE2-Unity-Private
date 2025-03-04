

namespace VE2.Core.VComponents.API
{
    internal interface IRangedClickInteractionModuleProvider : IRangedInteractionModuleProvider
    {
        public IRangedClickInteractionModule RangedClickInteractionModule => (IRangedClickInteractionModule)RangedInteractionModule;
    }
}
