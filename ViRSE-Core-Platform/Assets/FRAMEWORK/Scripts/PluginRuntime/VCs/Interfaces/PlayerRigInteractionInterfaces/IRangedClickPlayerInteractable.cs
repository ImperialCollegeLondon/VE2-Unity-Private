using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ViRSE.PluginRuntime.VComponents
{
    public interface IRangedClickPlayerInteractable
    {
        public void InvokeOnClickDown(InteractorID interactorID);
    }
}