
namespace VE2.Core.VComponents.API
{
    internal interface ICollideInteractionModuleProvider 
    {
        public int Layer { get; }
        public ICollideInteractionModule CollideInteractionModule { get; }
    }
}
