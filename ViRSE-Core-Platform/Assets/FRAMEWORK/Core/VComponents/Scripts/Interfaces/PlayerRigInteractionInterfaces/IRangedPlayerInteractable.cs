using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ViRSE.Core.VComponents
{
    public interface IRangedPlayerInteractableImplementor : IGeneralPlayerInteractableImplementor
    {
        protected IRangedPlayerInteractable RangedPlayerInteractable { get; }

        public float InteractRange => RangedPlayerInteractable.InteractRange;
    }

    public interface IRangedPlayerInteractable : IGeneralPlayerInteractable
    {
        public float InteractRange { get; }
    }
}
