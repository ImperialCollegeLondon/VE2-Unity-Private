using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ViRSE.Core.VComponents.InternalInterfaces;

namespace ViRSE.Core.VComponents.PlayerInterfaces
{
    public interface IGeneralPlayerInteractable
    {
        protected IGeneralInteractionModuleImplementor _GeneralModuleImplementor { get; }

        public bool AdminOnly => _GeneralModuleImplementor.GeneralInteractionModule.AdminOnly;
    }
}
