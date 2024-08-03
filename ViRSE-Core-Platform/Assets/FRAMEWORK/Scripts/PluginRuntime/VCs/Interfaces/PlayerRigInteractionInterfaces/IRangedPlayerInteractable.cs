using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ViRSE.PluginRuntime.VComponents
{
    public interface IRangedPlayerInteractable
    {
        public bool IsPositionWithinInteractRange(Vector3 position);
    }
}
