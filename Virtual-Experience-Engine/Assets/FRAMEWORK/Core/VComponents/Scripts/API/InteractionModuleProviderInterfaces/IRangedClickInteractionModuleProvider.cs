

namespace VE2.Core.VComponents.API
{
    public interface IRangedClickInteractionModuleProvider : IRangedInteractionModuleProvider
    {
        public IRangedClickInteractionModule RangedClickInteractionModule => (IRangedClickInteractionModule)RangedInteractionModule;
    }
}