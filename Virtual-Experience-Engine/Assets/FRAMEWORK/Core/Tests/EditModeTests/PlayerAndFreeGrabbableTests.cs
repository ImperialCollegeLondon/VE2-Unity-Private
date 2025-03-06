using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using VE2.Core.VComponents.Internal;
using System;
using VE2.Core.VComponents.Tests;
using VE2.Core.VComponents.API;
using VE2.Core.Player.Internal;

namespace VE2.Core.Tests
{
    [TestFixture]
        [Category("Player and Free Grabbable Tests")]
    internal class PlayerAndFreeGrabbableTests  : PlayerServiceSetupFixture
    {
        private IV_FreeGrabbable _grabbablePluginInterface => _v_freeGrabbableProviderStub;
        private IRangedGrabInteractionModuleProvider _grabbableRaycastInterface => _v_freeGrabbableProviderStub;
        private V_FreeGrabbableProviderStub _v_freeGrabbableProviderStub;
        private PluginGrabbableScript _customerScript;

        [SetUp]
        public void SetUpBeforeEveryTest()
        {           
            FreeGrabbableService freeGrabbable = new( 
                new List<IHandheldInteractionModule>() {},
                new FreeGrabbableConfig(),
                new FreeGrabbableState(), 
                "debug",
                Substitute.For<IWorldStateSyncService>(),
                InteractorContainerSetup.InteractorContainer,
                Substitute.For<IRigidbodyWrapper>(), 
                new PhysicsConstants());

            //Stub out provider layer
            _v_freeGrabbableProviderStub = new(freeGrabbable);

            //create customer script and hook up the listeners to the IV_grabbable
            _customerScript = Substitute.For<PluginGrabbableScript>();
            _grabbablePluginInterface.OnGrab.AddListener(_customerScript.HandleGrabReceived);
            _grabbablePluginInterface.OnDrop.AddListener(_customerScript.HandleDropReceived);
        }

        [Test]
        public void OnUserGrab_WithHoveringGrabbable_CustomerScriptReceivesOnGrab()
        {          
            //Stub out the raycast provider to hit the activatable GO with 0 range
            RayCastProviderSetup.RaycastProviderStub
                .Raycast(default, default, default, default)
                .ReturnsForAnyArgs(new RaycastResultWrapper(_grabbableRaycastInterface.RangedGrabInteractionModule, null, 0));

            //Invoke grab, check customer received the grab, and that the interactorID is set
            PlayerInputContainerSetup.Grab2D.OnPressed += Raise.Event<Action>();
            _customerScript.Received(1).HandleGrabReceived();
            Assert.IsTrue(_grabbablePluginInterface.IsGrabbed);
            Assert.AreEqual(_grabbablePluginInterface.MostRecentInteractingClientID, LocalClientIDProviderSetup.LocalClientIDProviderStub.LocalClientID);

            //Invoke drop, Check customer received the drop, and that the interactorID is set
            PlayerInputContainerSetup.Grab2D.OnPressed += Raise.Event<Action>();
            _customerScript.Received(1).HandleDropReceived();
            Assert.IsFalse(_grabbablePluginInterface.IsGrabbed);
            Assert.AreEqual(_grabbablePluginInterface.MostRecentInteractingClientID, LocalClientIDProviderSetup.LocalClientIDProviderStub.LocalClientID);
        }

        [TearDown]
        public void TearDownAfterEveryTest()
        {
            _customerScript.ClearReceivedCalls();  

            _grabbablePluginInterface.OnGrab.RemoveAllListeners();
            _grabbablePluginInterface.OnDrop.RemoveAllListeners();

            _v_freeGrabbableProviderStub.TearDown();
        }
    }
}
