using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


//No config for collider interactions

namespace ViRSE.PluginRuntime.VComponents
{
    public class ColliderInteractionModule : MonoBehaviour //Needs to implement player rig interface
    {
        public UnityEvent<InteractorID> OnCollideEnter { get; private set; } = new UnityEvent<InteractorID>(); //This is totally fine, how is it any different?

        public void InvokeOnCollideEnter(InteractorID id)
        {
            OnCollideEnter.Invoke(id);
        }
    }
}
