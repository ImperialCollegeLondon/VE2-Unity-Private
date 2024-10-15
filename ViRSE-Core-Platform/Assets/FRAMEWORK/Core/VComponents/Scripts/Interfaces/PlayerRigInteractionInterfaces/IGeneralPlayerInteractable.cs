using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ViRSE.Core.VComponents
{
    public interface IGeneralPlayerInteractableImplementor
    {
        public IGeneralPlayerInteractable GeneralPlayerInteractable { get; }

        public bool AdminOnly => GeneralPlayerInteractable.AdminOnly;  
        public bool VibrateControllers => GeneralPlayerInteractable.VibrateControllers;
        public bool ShowTooltips => GeneralPlayerInteractable.ShowTooltips;
    }   

    public interface IGeneralPlayerInteractable
    {
        public bool AdminOnly { get; }
        public bool VibrateControllers { get; }
        public bool ShowTooltips { get; }
    }
}
