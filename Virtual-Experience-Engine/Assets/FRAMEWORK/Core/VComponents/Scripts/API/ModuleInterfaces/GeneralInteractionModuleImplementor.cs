

namespace VE2.Core.VComponents.API
{
    public interface IGeneralInteractionModule
    {
        public bool AdminOnly { get; set; }
        public bool EnableControllerVibrations { get; set; }
        public bool ShowTooltipsAndHighlight { get; set; }
    }
}