using System;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using VE2.Common;
using VE2.Core.Player;
using VE2.Core.VComponents.InteractableFindables;
using VE2.Core.VComponents.InteractableInterfaces;
using VE2.Core.VComponents.Internal;
using VE2.Core.VComponents.PluginInterfaces;
using VE2.Core.VComponents.Tests;

namespace VE2.Core.Tests
{
    [TestFixture]
    [Category("Player and Linear Adjustable Tests")]
    public class PlayerAndLinearAdjustableTests
    {
        private IV_LinearAdjustable _linearAdjustablePluginInterface;
        private IRangedGrabPlayerInteractableIntegrator _linearAdjustableRaycastInterface;
        private V_LinearAdjustableStub _v_linearAdjustableStub;

        private PluginGrabbableScript _customerScript;
        private PlayerService _playerServiceStub;

        [OneTimeSetUp]
        public void SetUpOnce()
        {
            //Wire up the customer script to receive the events           
            _customerScript = Substitute.For<PluginGrabbableScript>();
        }

        [SetUp]
        public void SetUpBeforeEveryTest()
        {
            //create the handheld adjustable
            LinearAdjustableService linearAdjustable = LinearAdjustableServiceFactory.Create(interactorContainer: InteractorSetup.InteractorContainerStub);
            _v_linearAdjustableStub = new(linearAdjustable);

            //hook up interfaces
            _linearAdjustablePluginInterface = _v_linearAdjustableStub;
            _linearAdjustableRaycastInterface = _v_linearAdjustableStub;

            //wire up the customer script to receive the events
            _linearAdjustablePluginInterface.OnGrab.AddListener(_customerScript.HandleGrabReceived);
            _linearAdjustablePluginInterface.OnDrop.AddListener(_customerScript.HandleDropReceived);

            _playerServiceStub = new(
                new PlayerTransformData(),
                new PlayerStateConfig(),
                false,
                true,
                new PlayerStateModuleContainer(),
                InteractorSetup.InteractorContainerStub,
                PlayerSettingsProviderSetup.PlayerSettingsProviderStub,
                Substitute.For<IPlayerAppearanceOverridesProvider>(),
                MultiplayerSupportSetup.MultiplayerSupportStub,
                InputHandlerSetup.PlayerInputContainerStubWrapper.PlayerInputContainer,
                RayCastProviderSetup.RaycastProviderStub,
                Substitute.For<IXRManagerWrapper>()
            );
        }

        [Test]
        public void WithHoveringLinearAdjustable_OnUserGrab_CustomerScriptReceivesGrab()
        {
            RayCastProviderSetup.StubRangedInteractionModuleForRaycastProviderStub(_linearAdjustableRaycastInterface.RangedGrabInteractionModule);

            //Invoke grab, check customer received the grab, and that the interactorID is set
            InputHandlerSetup.PlayerInputContainerStubWrapper.Grab2D.OnPressed += Raise.Event<Action>();
            _customerScript.Received(1).HandleGrabReceived();
            Assert.IsTrue(_linearAdjustablePluginInterface.IsGrabbed);
            Assert.AreEqual(_linearAdjustablePluginInterface.MostRecentInteractingClientID, MultiplayerSupportSetup.LocalClientID);

            //Invoke drop, Check customer received the drop, and that the interactorID is set
            InputHandlerSetup.PlayerInputContainerStubWrapper.Grab2D.OnPressed += Raise.Event<Action>();
            _customerScript.Received(1).HandleDropReceived();
            Assert.IsFalse(_linearAdjustablePluginInterface.IsGrabbed);
            Assert.AreEqual(_linearAdjustablePluginInterface.MostRecentInteractingClientID, MultiplayerSupportSetup.LocalClientID);
        }

        [TearDown]
        public void TearDownAfterEveryTest()
        {
            _customerScript.ClearReceivedCalls();

            _linearAdjustablePluginInterface.OnGrab.RemoveAllListeners();
            _linearAdjustablePluginInterface.OnDrop.RemoveAllListeners();

            _v_linearAdjustableStub.TearDown();
            _linearAdjustableRaycastInterface = null;
            _linearAdjustablePluginInterface = null;

            _playerServiceStub.TearDown();
        }

        [OneTimeTearDown]
        public void TearDownOnce() { }
    }
}