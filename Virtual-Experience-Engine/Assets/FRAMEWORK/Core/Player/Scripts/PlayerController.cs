using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VE2.Common;

namespace VE2.Core.Player 
{
    public abstract class PlayerController : MonoBehaviour
    {
        public abstract PlayerTransformData PlayerTransformData { get; }
        public abstract void ActivatePlayer(PlayerTransformData initialTransformData);
        public abstract void DeactivatePlayer();
    }
}

/*
How do we want 2d/vr player transform to actually work?
Most performant to keep both rigs in the scene and toggle active, rather than instantiate/destroy
Is there actually any point at all in having this master player gameobject? 
Let's just have separate prefabs, and inject the controllers for these into the 

*/
