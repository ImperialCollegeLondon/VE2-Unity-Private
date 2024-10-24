using NSubstitute;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using ViRSE;
using ViRSE.Core.Shared;
using ViRSE.Core;
using ViRSE.Core.VComponents;
using ViRSE.Core.Player;
using static ViRSE.Core.Shared.CoreCommonSerializables;


namespace ViRSE.Tests
{
    public class PushActivatableTests
    {
        [Test]
        public void OnUserClick_WithHoveringActivatable_CustomerScriptReceivesOnActivate()
        {
            //Create an activatable
            GameObject activatableGO = new GameObject();
            BoxCollider boxCol = activatableGO.AddComponent<BoxCollider>();
            V_PushActivatable pushActivatable = activatableGO.AddComponent<V_PushActivatable>();
            pushActivatable.OnEnable();

            //Wire up mock customer script
            IPushActivatable pushActivatableInterface = pushActivatable;
            CustomerScript customerScript = Substitute.For<CustomerScript>();
            pushActivatableInterface.OnActivate.AddListener(customerScript.HandleActivateReceived);
            pushActivatableInterface.OnDeactivate.AddListener(customerScript.HandleDeactivateReceived);
    
            //Stub out the raycast for the player
            RaycastResultWrapper raycastResultStub = Substitute.For<RaycastResultWrapper>();
            raycastResultStub.RangedInteractableHit.Returns(pushActivatable);
            RaycastProviderStub raycastProvider = new RaycastProviderStub(raycastResultStub);

            //Stub out the input handler    
            InputHandler2DStub inputHandler2DStub = new();
            InputHandlerStub inputHandlerStub = new(inputHandler2DStub);

            //Create the player
            GameObject playerSpawnerGO = new GameObject();
            V_PlayerSpawner playerSpawner = playerSpawnerGO.AddComponent<V_PlayerSpawner>();
            playerSpawner.OnEnable();

            /*####################PROBLEMS##########################
            To spawn the player, we also need to mock out the ViRSECoreServiceLocator.Instance.PlayerSettingsProvider
            This is probably a bit of a sign that we just shouldn't even be testing things at the MonoBehaviour level, we leave those for the e2e tests
            This means we don't need to worry about making things visible for the test, or having partial classes with test hooks or any of that nonsense 
            BUT, we do need to figure out how to properly instantiate just the service WITHOUT the monobehaviour
            The raycast wrapper will need to return the actual module, rather than the implementor 
            This means we may need to add interfaces onto the PushActivatable, rather than relying on the VC to do all this wiring - there wont BE a VC!!!

            ########################################################*/

            //Check customer received the activation, and that the interactorID is set
            inputHandler2DStub.SimulateMouseClick();
            customerScript.Received(1).HandleActivateReceived();
            //Assert.AreEqual(interactorID, stateInterface.CurrentInteractor);

            // Invoke the click to deactivate
            inputHandler2DStub.SimulateMouseClick();
            customerScript.Received(1).HandleDeactivateReceived();
            //Assert.IsNull(stateInterface.CurrentInteractor);
        }

        public class CustomerScript
        {
            public void HandleActivateReceived() { }

            public void HandleDeactivateReceived() { }
        }

        public class RaycastProviderStub : RaycastProvider
        {
            private readonly RaycastResultWrapper _raycastHitWrapperStub;

            public RaycastProviderStub(RaycastResultWrapper raycastHitWrapper)
            {
                _raycastHitWrapperStub = raycastHitWrapper;
                _instance = this;
            }

            public override bool Raycast(Vector3 rayOrigin, Vector3 raycastDirection, out RaycastResultWrapper hit, float maxRaycastDistance, LayerMask layerMask)
            {
                hit = _raycastHitWrapperStub;
                return true;
            }
        }

        public class InputHandlerStub : V_InputHandler
        {
            private readonly InputHandler2D _inputHandler2DStub;
            public override InputHandler2D InputHandler2D => _inputHandler2DStub;


            public InputHandlerStub(InputHandler2D InputHandler2DStub)
            {
                _inputHandler2DStub = InputHandler2DStub;
                _instance = this;
            }   
        }

        public class InputHandler2DStub : InputHandler2D
        {
            public void SimulateMouseClick() => InvokeOnMouseLeftClick();
        }   
    }
}
