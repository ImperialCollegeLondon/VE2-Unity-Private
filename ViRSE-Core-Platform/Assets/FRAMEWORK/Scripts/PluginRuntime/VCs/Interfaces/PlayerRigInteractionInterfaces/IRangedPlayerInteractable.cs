using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ViRSE.PluginRuntime.VComponents
{
    public interface IRangedPlayerInteractableImplementor : IGeneralPlayerInteractableImplementor
    {
        protected IRangedPlayerInteractable RangedPlayerInteractable { get; }

        public float InteractRange => RangedPlayerInteractable.InteractRange;
    }

    public interface IRangedPlayerInteractable
    {
        public float InteractRange { get; }
    }
}
