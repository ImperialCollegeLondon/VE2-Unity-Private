using System;
using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using VE2.Common.TransformWrapper;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Internal;
using VE2.Core.VComponents.Tests;

namespace VE2.Core.Tests
{
    [TestFixture]
    [Category("Player and Rotational Adjustable Tests")]
    internal class PlayerAndRotationalAdjustableTests : PlayerServiceSetupFixture
    {
        private IV_RotationalAdjustable _rotationalAdjustablePluginInterface => _v_rotationalAdjustableProviderStub;
        private IRangedGrabInteractionModuleProvider _rotationalAdjustableRaycastInterface => _v_rotationalAdjustableProviderStub;
        private V_RotationalAdjustableProviderStub _v_rotationalAdjustableProviderStub;

        private PluginGrabbableScript _customerScript;

        [SetUp]
        public void SetUpBeforeEveryTest()
        {
            //create the handheld adjustable
            RotationalAdjustableService rotationalAdjustable = new(
                Substitute.For<ITransformWrapper>(),
                new List<IHandheldInteractionModule>(),
                new RotationalAdjustableConfig(),
                new AdjustableState(),
                new GrabbableState(),
                "debug",
                Substitute.For<IWorldStateSyncService>(),
                InteractorContainerSetup.InteractorContainer);

            _v_rotationalAdjustableProviderStub = new(rotationalAdjustable);

            //wire up the customer script to receive the events
            _customerScript = Substitute.For<PluginGrabbableScript>();
            _rotationalAdjustablePluginInterface.OnGrab.AddListener(_customerScript.HandleGrabReceived);
            _rotationalAdjustablePluginInterface.OnDrop.AddListener(_customerScript.HandleDropReceived);
        }

        [Test]
        public void WithHoveringLinearAdjustable_OnUserGrab_CustomerScriptReceivesGrab()
        {
            RayCastProviderSetup.StubRangedInteractionModuleForRaycastProviderStub(_rotationalAdjustableRaycastInterface.RangedGrabInteractionModule);

            //Invoke grab, check customer received the grab, and that the interactorID is set
            PlayerInputContainerSetup.Grab2D.OnPressed += Raise.Event<Action>();
            _customerScript.Received(1).HandleGrabReceived();
            Assert.IsTrue(_rotationalAdjustablePluginInterface.IsGrabbed);
            Assert.AreEqual(_rotationalAdjustablePluginInterface.MostRecentInteractingClientID, LocalClientIDProviderSetup.LocalClientID);

            //Invoke drop, Check customer received the drop, and that the interactorID is set
            PlayerInputContainerSetup.Grab2D.OnPressed += Raise.Event<Action>();
            _customerScript.Received(1).HandleDropReceived();
            Assert.IsFalse(_rotationalAdjustablePluginInterface.IsGrabbed);
            Assert.AreEqual(_rotationalAdjustablePluginInterface.MostRecentInteractingClientID, LocalClientIDProviderSetup.LocalClientID);
        }

        [TearDown]
        public void TearDownAfterEveryTest()
        {
            _customerScript.ClearReceivedCalls();

            _rotationalAdjustablePluginInterface.OnGrab.RemoveAllListeners();
            _rotationalAdjustablePluginInterface.OnDrop.RemoveAllListeners();

            _v_rotationalAdjustableProviderStub.TearDown();
        }

        [OneTimeTearDown]
        public void TearDownOnce() { }
    }
}