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
    [Category("Player and Sliding Adjustable Tests")]
    internal class PlayerAndLinearAdjustableTests : PlayerServiceSetupFixture
    {
        private IV_SlidingAdjustable _slidingAdjustablePluginInterface => _v_slidingAdjustableProviderStub;
        private IRangedGrabInteractionModuleProvider _slidingAdjustableRaycastInterface => _v_slidingAdjustableProviderStub;
        private V_SlidingAdjustableProviderStub _v_slidingAdjustableProviderStub;

        private PluginGrabbableScript _customerScript;

        [SetUp]
        public void SetUpBeforeEveryTest()
        {
            //create the handheld adjustable
            SlidingAdjustableService slidingAdjustable = new(
                new List<IHandheldInteractionModule>(),
                new SlidingAdjustableConfig(Substitute.For<ITransformWrapper>(), Substitute.For<ITransformWrapper>()),
                new AdjustableState(),
                new GrabbableState(),
                "debug",
                Substitute.For<IWorldStateSyncableContainer>(),
                GrabInteractableContainerSetup.GrabInteractableContainer,
                InteractorContainerSetup.InteractorContainer,
                LocalClientIDWrapperSetup.LocalClientIDWrapper);

            _v_slidingAdjustableProviderStub = new(slidingAdjustable);

            //wire up the customer script to receive the events
            _customerScript = Substitute.For<PluginGrabbableScript>();
            _slidingAdjustablePluginInterface.OnGrab.AddListener(_customerScript.HandleGrabReceived);
            _slidingAdjustablePluginInterface.OnDrop.AddListener(_customerScript.HandleDropReceived);
        }

        [Test]
        public void WithHoveringSlidingAdjustable_OnUserGrab_CustomerScriptReceivesGrab()
        {
            RayCastProviderSetup.StubRangedInteractionModuleForRaycast(_slidingAdjustableRaycastInterface.RangedGrabInteractionModule);

            //Invoke grab, check customer received the grab, and that the interactorID is set
            PlayerInputContainerSetup.Grab2D.OnPressed += Raise.Event<Action>();
            _customerScript.Received(1).HandleGrabReceived();
            Assert.IsTrue(_slidingAdjustablePluginInterface.IsGrabbed);
            Assert.AreEqual(_slidingAdjustablePluginInterface.MostRecentGrabbingClientID.Value, LocalClientIDWrapperSetup.LocalClientID);
            Assert.IsTrue(_slidingAdjustablePluginInterface.MostRecentGrabbingClientID.IsLocal);

            //Invoke drop, Check customer received the drop, and that the interactorID is set
            PlayerInputContainerSetup.Grab2D.OnPressed += Raise.Event<Action>();
            _customerScript.Received(1).HandleDropReceived();
            Assert.IsFalse(_slidingAdjustablePluginInterface.IsGrabbed);
            Assert.AreEqual(_slidingAdjustablePluginInterface.MostRecentGrabbingClientID.Value, LocalClientIDWrapperSetup.LocalClientID);
            Assert.IsTrue(_slidingAdjustablePluginInterface.MostRecentGrabbingClientID.IsLocal);
        }

        [TearDown]
        public void TearDownAfterEveryTest()
        {
            _customerScript.ClearReceivedCalls();

            _slidingAdjustablePluginInterface.OnGrab.RemoveAllListeners();
            _slidingAdjustablePluginInterface.OnDrop.RemoveAllListeners();

            _v_slidingAdjustableProviderStub.TearDown();
        }

        [OneTimeTearDown]
        public void TearDownOnce() { }
    }
}