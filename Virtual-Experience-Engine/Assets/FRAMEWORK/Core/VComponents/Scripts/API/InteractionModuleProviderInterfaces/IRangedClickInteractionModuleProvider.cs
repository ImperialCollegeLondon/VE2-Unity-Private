

namespace VE2.Core.VComponents.API
{
    internal interface IRangedToggleClickInteractionModuleProvider : IRangedInteractionModuleProvider
    {
        public IRangedToggleClickInteractionModule RangedToggleClickInteractionModule => (IRangedToggleClickInteractionModule)RangedInteractionModule;
    }

    internal interface IRangedHoldClickInteractionModuleProvider : IRangedInteractionModuleProvider
    {
        public IRangedHoldClickInteractionModule RangedHoldClickInteractionModule => (IRangedHoldClickInteractionModule)RangedInteractionModule;
    }
}
