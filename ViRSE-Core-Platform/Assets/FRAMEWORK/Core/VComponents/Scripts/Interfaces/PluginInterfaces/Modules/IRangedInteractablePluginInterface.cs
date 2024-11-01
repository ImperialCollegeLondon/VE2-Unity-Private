using ViRSE.Core.VComponents.InternalInterfaces;

public interface IRangedInteractablePluginInterface : IGeneralInteractionPluginInterface
{
    protected IRangedInteractionModuleImplementor _RangedModuleImplementor { get; }  
    public float InteractRange { get => _RangedModuleImplementor.RangedInteractionModule.InteractRange; set => _RangedModuleImplementor.RangedInteractionModule.InteractRange = value; }
}
