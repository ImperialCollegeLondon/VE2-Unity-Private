using NSubstitute;
using NUnit.Framework;
using VE2.Core.VComponents.Internal;
using System;
using VE2.Core.VComponents.Tests;
using VE2.Core.VComponents.API;
using VE2.Core.Player.Internal;
using System.Collections.Generic;
using VE2.Common.Shared;
using VE2.Common.API;


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
                new GrabbableState(), 
                "debug",
                Substitute.For<IWorldStateSyncableContainer>(),
                GrabInteractableContainerSetup.GrabInteractableContainer,
                InteractorContainerSetup.InteractorContainer,
                Substitute.For<IRigidbodyWrapper>(), 
                new PhysicsConstants(),
                new V_FreeGrabbable(),
                LocalClientIDWrapperSetup.LocalClientIDWrapper,
                Substitute.For<IColliderWrapper>());

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
            RayCastProviderSetup.StubRangedInteractionModuleForRaycast(_grabbableRaycastInterface.RangedGrabInteractionModule);

            //Invoke grab, check customer received the grab, and that the interactorID is set
            PlayerInputContainerSetup.Grab2D.OnPressed += Raise.Event<Action>();
            _customerScript.Received(1).HandleGrabReceived();
            Assert.IsTrue(_grabbablePluginInterface.IsGrabbed);
            Assert.AreEqual(_grabbablePluginInterface.MostRecentInteractingClientID.Value, LocalClientIDWrapperSetup.LocalClientID);
            Assert.IsTrue(_grabbablePluginInterface.MostRecentInteractingClientID.IsLocal);

            //Invoke drop, Check customer received the drop, and that the interactorID is set
            PlayerInputContainerSetup.Grab2D.OnPressed += Raise.Event<Action>();
            _customerScript.Received(1).HandleDropReceived();
            Assert.IsFalse(_grabbablePluginInterface.IsGrabbed);
            Assert.AreEqual(_grabbablePluginInterface.MostRecentInteractingClientID.Value, LocalClientIDWrapperSetup.LocalClientID);
            Assert.IsTrue(_grabbablePluginInterface.MostRecentInteractingClientID.IsLocal);
        }

        [Test]
        public void OnUser_WhenNotHoveringOverGrabbable_GrabsFailsafeGrabbable()
        {
            RayCastProviderSetup.StubRangedInteractionModuleForSpherecastAll(_grabbableRaycastInterface.RangedGrabInteractionModule);

            PlayerInputContainerSetup.PlayerInputContainerStub.ChangeMode.OnPressed += Raise.Event<Action>();
            Assert.IsTrue(PlayerService.IsVRMode, "Player should be in VR mode");

            PlayerInputContainerSetup.GrabVRRight.OnPressed += Raise.Event<Action>();
            _customerScript.Received(1).HandleGrabReceived();
            Assert.IsTrue(_grabbablePluginInterface.IsGrabbed);
            Assert.AreEqual(_grabbablePluginInterface.MostRecentInteractingClientID.Value, LocalClientIDWrapperSetup.LocalClientID);
            Assert.IsTrue(_grabbablePluginInterface.MostRecentInteractingClientID.IsLocal);

            //Invoke drop, Check customer received the drop, and that the interactorID is set
            PlayerInputContainerSetup.GrabVRRight.OnPressed += Raise.Event<Action>();
            _customerScript.Received(1).HandleDropReceived();
            Assert.IsFalse(_grabbablePluginInterface.IsGrabbed);
            Assert.AreEqual(_grabbablePluginInterface.MostRecentInteractingClientID.Value, LocalClientIDWrapperSetup.LocalClientID);
            Assert.IsTrue(_grabbablePluginInterface.MostRecentInteractingClientID.IsLocal);
        }

        [TearDown]
        public void TearDownAfterEveryTest()
        {
            _customerScript.ClearReceivedCalls();  

            _grabbablePluginInterface.OnGrab.RemoveAllListeners();
            _grabbablePluginInterface.OnDrop.RemoveAllListeners();

            _v_freeGrabbableProviderStub.TearDown();
        }

        [OneTimeTearDown]
        public void TearDownOnce() { }
    }
}
