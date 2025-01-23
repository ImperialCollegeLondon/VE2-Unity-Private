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
    [Category("Player and Rotational Adjustable Tests")]
    public class PlayerAndRotationalAdjustableTests
    {
        private IV_RotationalAdjustable _rotationalAdjustablePluginInterface;
        private IRangedGrabPlayerInteractableIntegrator _rotationalAdjustableRaycastInterface;
        private V_RotationalAdjustableStub _v_rotationalAdjustableStub;

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
            RotationalAdjustableService rotationalAdjustable = RotationalAdjustableServiceFactory.Create(interactorContainer: InteractorSetup.InteractorContainerStub);
            _v_rotationalAdjustableStub = new(rotationalAdjustable);

            //hook up interfaces
            _rotationalAdjustablePluginInterface = _v_rotationalAdjustableStub;
            _rotationalAdjustableRaycastInterface = _v_rotationalAdjustableStub;

            //wire up the customer script to receive the events
            _rotationalAdjustablePluginInterface.OnGrab.AddListener(_customerScript.HandleGrabReceived);
            _rotationalAdjustablePluginInterface.OnDrop.AddListener(_customerScript.HandleDropReceived);

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
            RayCastProviderSetup.StubRangedInteractionModuleForRaycastProviderStub(_rotationalAdjustableRaycastInterface.RangedGrabInteractionModule);

            //Invoke grab, check customer received the grab, and that the interactorID is set
            InputHandlerSetup.PlayerInputContainerStubWrapper.Grab2D.OnPressed += Raise.Event<Action>();
            _customerScript.Received(1).HandleGrabReceived();
            Assert.IsTrue(_rotationalAdjustablePluginInterface.IsGrabbed);
            Assert.AreEqual(_rotationalAdjustablePluginInterface.MostRecentInteractingClientID, MultiplayerSupportSetup.LocalClientID);

            //Invoke drop, Check customer received the drop, and that the interactorID is set
            InputHandlerSetup.PlayerInputContainerStubWrapper.Grab2D.OnPressed += Raise.Event<Action>();
            _customerScript.Received(1).HandleDropReceived();
            Assert.IsFalse(_rotationalAdjustablePluginInterface.IsGrabbed);
            Assert.AreEqual(_rotationalAdjustablePluginInterface.MostRecentInteractingClientID, MultiplayerSupportSetup.LocalClientID);
        }

        [TearDown]
        public void TearDownAfterEveryTest()
        {
            _customerScript.ClearReceivedCalls();

            _rotationalAdjustablePluginInterface.OnGrab.RemoveAllListeners();
            _rotationalAdjustablePluginInterface.OnDrop.RemoveAllListeners();

            _v_rotationalAdjustableStub.TearDown();
            _rotationalAdjustableRaycastInterface = null;
            _rotationalAdjustablePluginInterface = null;

            _playerServiceStub.TearDown();
        }

        [OneTimeTearDown]
        public void TearDownOnce() { }
    }
}