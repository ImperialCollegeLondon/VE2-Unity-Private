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
    [Category("Player and Linear Adjustable Tests")]
    internal class PlayerAndLinearAdjustableTests : PlayerServiceSetupFixture
    {
        private IV_LinearAdjustable _linearAdjustablePluginInterface => _v_linearAdjustableProviderStub;
        private IRangedGrabInteractionModuleProvider _linearAdjustableRaycastInterface => _v_linearAdjustableProviderStub;
        private V_LinearAdjustableProviderStub _v_linearAdjustableProviderStub;

        private PluginGrabbableScript _customerScript;

        [SetUp]
        public void SetUpBeforeEveryTest()
        {
            //create the handheld adjustable
            LinearAdjustableService linearAdjustable = new(
                new List<IHandheldInteractionModule>(),
                new LinearAdjustableConfig(Substitute.For<ITransformWrapper>(), Substitute.For<ITransformWrapper>()),
                new AdjustableState(),
                new GrabbableState(),
                "debug",
                Substitute.For<IWorldStateSyncableContainer>(),
                GrabInteractableContainerSetup.GrabInteractableContainer,
                InteractorContainerSetup.InteractorContainer,
                LocalClientIDWrapperSetup.LocalClientIDWrapper);

            _v_linearAdjustableProviderStub = new(linearAdjustable);

            //wire up the customer script to receive the events
            _customerScript = Substitute.For<PluginGrabbableScript>();
            _linearAdjustablePluginInterface.OnGrab.AddListener(_customerScript.HandleGrabReceived);
            _linearAdjustablePluginInterface.OnDrop.AddListener(_customerScript.HandleDropReceived);
        }

        [Test]
        public void WithHoveringLinearAdjustable_OnUserGrab_CustomerScriptReceivesGrab()
        {
            RayCastProviderSetup.StubRangedInteractionModuleForRaycast(_linearAdjustableRaycastInterface.RangedGrabInteractionModule);

            //Invoke grab, check customer received the grab, and that the interactorID is set
            PlayerInputContainerSetup.Grab2D.OnPressed += Raise.Event<Action>();
            _customerScript.Received(1).HandleGrabReceived();
            Assert.IsTrue(_linearAdjustablePluginInterface.IsGrabbed);
            Assert.AreEqual(_linearAdjustablePluginInterface.MostRecentInteractingClientID.Value, LocalClientIDWrapperSetup.LocalClientID);
            Assert.IsTrue(_linearAdjustablePluginInterface.MostRecentInteractingClientID.IsLocal);

            //Invoke drop, Check customer received the drop, and that the interactorID is set
            PlayerInputContainerSetup.Grab2D.OnPressed += Raise.Event<Action>();
            _customerScript.Received(1).HandleDropReceived();
            Assert.IsFalse(_linearAdjustablePluginInterface.IsGrabbed);
            Assert.AreEqual(_linearAdjustablePluginInterface.MostRecentInteractingClientID.Value, LocalClientIDWrapperSetup.LocalClientID);
            Assert.IsTrue(_linearAdjustablePluginInterface.MostRecentInteractingClientID.IsLocal);
        }

        [TearDown]
        public void TearDownAfterEveryTest()
        {
            _customerScript.ClearReceivedCalls();

            _linearAdjustablePluginInterface.OnGrab.RemoveAllListeners();
            _linearAdjustablePluginInterface.OnDrop.RemoveAllListeners();

            _v_linearAdjustableProviderStub.TearDown();
        }

        [OneTimeTearDown]
        public void TearDownOnce() { }
    }
}