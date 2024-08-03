using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ViRSE.FrameworkRuntime.LocalPlayerRig
{
    public class PlayerController2D : PlayerController
    {
        [SerializeField] private Camera camera2D;
        [SerializeField] private Player2DLocomotor playerLocomotor2D;
        [SerializeField] private Interactor2D interactor2D;

        // Start is called before the first frame update
        void Start()
        {
            interactor2D.Setup(camera2D);
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
