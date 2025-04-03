
namespace VE2.Core.VComponents.API
{
    internal interface IRangedInteractionModule : IGeneralInteractionModule
    {
        public float InteractRange { get; set; }
        public void EnterHover();
        public void ExitHover();
    }
}
