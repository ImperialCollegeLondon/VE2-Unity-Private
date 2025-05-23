using System;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using VE2.Core.Player.API;
using VE2.Core.Player.Internal;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Internal;
using VE2.Core.VComponents.Tests;

namespace VE2.Core.Tests
{
    [TestFixture]
    [Category("Player and Hold Activatable Tests")]
    internal class PlayerAndHoldActivatableTests : PlayerServiceSetupFixture
    {
        private IV_HoldActivatable _holdActivatablePluginInterface => _v_holdActivatableProviderStub;
        private IRangedHoldClickInteractionModuleProvider _holdActivatableRaycastInterface => _v_holdActivatableProviderStub;
        private ICollideInteractionModuleProvider _holdActivatableCollideInterface => _v_holdActivatableProviderStub;
        private V_HoldActivatableProviderStub _v_holdActivatableProviderStub;
        private PluginActivatableScript _customerScript;

        [SetUp]
        public void SetUpBeforeEveryTest()
        {
            HoldActivatableService holdActivatableService = new(
                new HoldActivatableConfig(),
                new MultiInteractorActivatableState(),
                "debug",
                LocalClientIDWrapperSetup.LocalClientIDWrapper);

            _v_holdActivatableProviderStub = new(holdActivatableService);

            _customerScript = Substitute.For<PluginActivatableScript>();
            _holdActivatablePluginInterface.OnActivate.AddListener(_customerScript.HandleActivateReceived);
            _holdActivatablePluginInterface.OnDeactivate.AddListener(_customerScript.HandleDeactivateReceived);
        }

        [Test]
        public void OnUserPressedDownAndReleased_WithHoveringActivatable_CustomerScriptTriggersOnActivateAndOnDeactivate([Random((ushort)0, ushort.MaxValue, 1)] ushort localClientID)
        {
            RayCastProviderSetup.StubRangedInteractionModuleForRaycast(_holdActivatableRaycastInterface.RangedInteractionModule);
            LocalClientIDWrapperSetup.LocalClientIDWrapper.Value.Returns(localClientID);

            PlayerInputContainerSetup.RangedClick2D.OnPressed += Raise.Event<Action>();
            _customerScript.Received(1).HandleActivateReceived();
            Assert.IsTrue(_holdActivatablePluginInterface.IsActivated, "Activatable should be activated");
            Assert.AreEqual(_holdActivatablePluginInterface.MostRecentInteractingClientID.Value, localClientID);
            Assert.IsTrue(_holdActivatablePluginInterface.MostRecentInteractingClientID.IsLocal);

            PlayerInputContainerSetup.RangedClick2D.OnReleased += Raise.Event<Action>();
            _customerScript.Received(1).HandleDeactivateReceived();
            Assert.IsFalse(_holdActivatablePluginInterface.IsActivated, "Activatable should be deactivated");
            Assert.AreEqual(_holdActivatablePluginInterface.MostRecentInteractingClientID.Value, localClientID);
            Assert.IsTrue(_holdActivatablePluginInterface.MostRecentInteractingClientID.IsLocal);
        }

        [Test]
        public void OnUserCollideEnterAndExitInVR_OnCollidingActivatable_CustomerScriptTriggersOnActivateAndOnDeactivate([Random((ushort)0, ushort.MaxValue, 1)] ushort localClientID)
        {
            LocalClientIDWrapperSetup.LocalClientIDWrapper.Value.Returns(localClientID);
            ICollisionDetector handColliderLeft = CollisionDetectorFactoryStubSetup.CollisionDetectorFactoryStub.CollisionDetectorStubs[ColliderType.HandVRLeft];
            ICollisionDetector handColliderRight = CollisionDetectorFactoryStubSetup.CollisionDetectorFactoryStub.CollisionDetectorStubs[ColliderType.HandVRRight];

            PlayerInputContainerSetup.PlayerInputContainerStub.ChangeMode.OnPressed += Raise.Event<Action>();
            Assert.IsTrue(PlayerService.IsVRMode, "Player should be in VR mode");

            handColliderLeft.OnCollideStart += Raise.Event<Action<ICollideInteractionModule>>(_holdActivatableCollideInterface.CollideInteractionModule);
            _customerScript.Received(1).HandleActivateReceived();
            Assert.IsTrue(_holdActivatablePluginInterface.IsActivated, "Activatable should be activated");
            Assert.AreEqual(_holdActivatablePluginInterface.MostRecentInteractingClientID.Value, localClientID);
            Assert.IsTrue(_holdActivatablePluginInterface.MostRecentInteractingClientID.IsLocal);
            
            handColliderRight.OnCollideStart += Raise.Event<Action<ICollideInteractionModule>>(_holdActivatableCollideInterface.CollideInteractionModule);
            _customerScript.Received(1).HandleActivateReceived();
            Assert.IsTrue(_holdActivatablePluginInterface.IsActivated, "Activatable should be activated");
            Assert.AreEqual(_holdActivatablePluginInterface.MostRecentInteractingClientID.Value, localClientID);
            Assert.IsTrue(_holdActivatablePluginInterface.MostRecentInteractingClientID.IsLocal);

            handColliderLeft.OnCollideEnd += Raise.Event<Action<ICollideInteractionModule>>(_holdActivatableCollideInterface.CollideInteractionModule);
            _customerScript.Received(1).HandleActivateReceived();
            Assert.IsTrue(_holdActivatablePluginInterface.IsActivated, "Activatable should be activated");
            Assert.AreEqual(_holdActivatablePluginInterface.MostRecentInteractingClientID.Value, localClientID);
            Assert.IsTrue(_holdActivatablePluginInterface.MostRecentInteractingClientID.IsLocal);

            handColliderRight.OnCollideEnd += Raise.Event<Action<ICollideInteractionModule>>(_holdActivatableCollideInterface.CollideInteractionModule);
            _customerScript.Received(1).HandleDeactivateReceived();
            Assert.IsFalse(_holdActivatablePluginInterface.IsActivated, "Activatable should be deactivated");
            Assert.AreEqual(_holdActivatablePluginInterface.MostRecentInteractingClientID.Value, localClientID);
            Assert.IsTrue(_holdActivatablePluginInterface.MostRecentInteractingClientID.IsLocal);
        }

        [TearDown]
        public void TearDownAfterEveryTest()
        {
            _customerScript.ClearReceivedCalls();

            _holdActivatablePluginInterface.OnActivate.RemoveAllListeners();
            _holdActivatablePluginInterface.OnDeactivate.RemoveAllListeners();

            _v_holdActivatableProviderStub.TearDown();
        }
    }
}
