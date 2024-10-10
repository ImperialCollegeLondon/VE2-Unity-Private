using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ViRSE.Core.Player
{
    public class PlayerController2D : PlayerController
    {
        [SerializeField] private Camera camera2D;
        [SerializeField] private Player2DLocomotor playerLocomotor2D;
        [SerializeField] private Interactor2D interactor2D;
        [SerializeField] private CharacterController characterController;

        public override Vector3 RootPosition { 
            get => transform.position + (Vector3.down * characterController.height / 2); 
            set {
            characterController.enabled = false;
            transform.position = value + (Vector3.up * characterController.height / 2);
            characterController.enabled = true;
            }
        }    
        public override Quaternion RootRotation { get => transform.rotation; set => transform.rotation = value; }

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
