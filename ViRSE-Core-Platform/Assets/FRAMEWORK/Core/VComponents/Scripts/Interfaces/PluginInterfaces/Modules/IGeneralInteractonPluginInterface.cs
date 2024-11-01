using UnityEngine;
using ViRSE.Core.VComponents;
using ViRSE.Core.VComponents.InternalInterfaces;

public interface IGeneralInteractionPluginInterface
{
    protected IGeneralInteractionModuleImplementor _GeneralModuleImplementor { get; }
    public bool AdminOnly
    {
        get
        {
            return _GeneralModuleImplementor.GeneralInteractionModule.AdminOnly;
        }
        set
        {
            _GeneralModuleImplementor.GeneralInteractionModule.AdminOnly = value;
        }
    }
}
