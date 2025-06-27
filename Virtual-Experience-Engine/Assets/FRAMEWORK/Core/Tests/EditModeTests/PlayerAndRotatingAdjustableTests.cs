using System;
using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Internal;
using VE2.Core.VComponents.Tests;

namespace VE2.Core.Tests
{
    [TestFixture]
    [Category("Player and Rotating Adjustable Tests")]
    internal class PlayerAndRotatingAdjustableTests : PlayerServiceSetupFixture
    {
        private IV_RotatingAdjustable _rotatingAdjustablePluginInterface => _v_rotatingAdjustableProviderStub;
        private IRangedGrabInteractionModuleProvider _rotatingAdjustableRaycastInterface => _v_rotatingAdjustableProviderStub;
        private V_RotatingAdjustableProviderStub _v_rotatingAdjustableProviderStub;

        private PluginGrabbableScript _customerScript;

        [SetUp]
        public void SetUpBeforeEveryTest()
        {
            //create the handheld adjustable
            RotatingAdjustableService rotatingAdjustable = new(
                new List<IHandheldInteractionModule>(),
                new RotatingAdjustableConfig(Substitute.For<ITransformWrapper>(), Substitute.For<ITransformWrapper>()),
                new AdjustableState(),
                new GrabbableState(),
                "debug",
                Substitute.For<IWorldStateSyncableContainer>(),
                GrabInteractableContainerSetup.GrabInteractableContainer,
                InteractorContainerSetup.InteractorContainer,
                LocalClientIDWrapperSetup.LocalClientIDWrapper);

            _v_rotatingAdjustableProviderStub = new(rotatingAdjustable);

            //wire up the customer script to receive the events
            _customerScript = Substitute.For<PluginGrabbableScript>();
            _rotatingAdjustablePluginInterface.OnGrab.AddListener(_customerScript.HandleGrabReceived);
            _rotatingAdjustablePluginInterface.OnDrop.AddListener(_customerScript.HandleDropReceived);
        }

        [Test]
        public void WithHoveringRotatingAdjustable_OnUserGrab_CustomerScriptReceivesGrab()
        {
            RayCastProviderSetup.StubRangedInteractionModuleForRaycast(_rotatingAdjustableRaycastInterface.RangedGrabInteractionModule);

            //Invoke grab, check customer received the grab, and that the interactorID is set
            PlayerInputContainerSetup.Grab2D.OnPressed += Raise.Event<Action>();
            _customerScript.Received(1).HandleGrabReceived();
            Assert.IsTrue(_rotatingAdjustablePluginInterface.IsGrabbed);
            Assert.AreEqual(_rotatingAdjustablePluginInterface.MostRecentInteractingClientID.Value, LocalClientIDWrapperSetup.LocalClientID);
            Assert.IsTrue(_rotatingAdjustablePluginInterface.MostRecentInteractingClientID.IsLocal);

            //Invoke drop, Check customer received the drop, and that the interactorID is set
            PlayerInputContainerSetup.Grab2D.OnPressed += Raise.Event<Action>();
            _customerScript.Received(1).HandleDropReceived();
            Assert.IsFalse(_rotatingAdjustablePluginInterface.IsGrabbed);
            Assert.AreEqual(_rotatingAdjustablePluginInterface.MostRecentInteractingClientID.Value, LocalClientIDWrapperSetup.LocalClientID);
            Assert.IsTrue(_rotatingAdjustablePluginInterface.MostRecentInteractingClientID.IsLocal);
        }

        [TearDown]
        public void TearDownAfterEveryTest()
        {
            _customerScript.ClearReceivedCalls();

            _rotatingAdjustablePluginInterface.OnGrab.RemoveAllListeners();
            _rotatingAdjustablePluginInterface.OnDrop.RemoveAllListeners();

            _v_rotatingAdjustableProviderStub.TearDown();
        }

        [OneTimeTearDown]
        public void TearDownOnce() { }
    }
}