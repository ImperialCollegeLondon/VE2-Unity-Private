using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ViRSE.PluginRuntime.VComponents
{
    public interface IRangedInteractionModuleImplementor
    {
        protected IRangedInteractionModule _module { get; } //Not visible to customer 

        public float InteractRange { get => _module.InteractRange; set => _module.InteractRange = value; }
    }

    public interface IRangedInteractionModule
    {
        public float InteractRange { get; set; }
    }
}