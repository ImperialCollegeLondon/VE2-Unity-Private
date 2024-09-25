using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ViRSE.Core.Player 
{
    public abstract class PlayerController : MonoBehaviour
    {
        public abstract Vector3 RootPosition { get; set; }
        public abstract Quaternion RootRotation { get; set; }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

    }
}

/*
How do we want 2d/vr player transform to actually work?
Most performant to keep both rigs in the scene and toggle active, rather than instantiate/destroy
Is there actually any point at all in having this master player gameobject? 
Let's just have separate prefabs, and inject the controllers for these into the 

*/
