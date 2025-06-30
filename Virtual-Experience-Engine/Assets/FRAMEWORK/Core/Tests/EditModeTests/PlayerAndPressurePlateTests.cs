using System;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using VE2.Common.Shared;
using VE2.Core.Player.Internal;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Internal;
using VE2.Core.VComponents.Tests;
namespace VE2.Core.Tests
{
    [TestFixture]
    [Category("PlayerAndPressurePlateTests")]
    internal class PlayerAndPressurePlateTests : PlayerServiceSetupFixture
    {
        private IV_PressurePlate _pressurePlatePluginInterface => _v_pressurePlateProviderStub;
        private ICollideInteractionModuleProvider _pressurePlateCollideInterface => _v_pressurePlateProviderStub;
        private V_PressurePlateStub _v_pressurePlateProviderStub;
        private PluginActivatableScript _customerScript;

        [SetUp]
        public void SetUpBeforeEveryTest()
        {
            PressurePlateService pressurePlateService = new(
                new PressurePlateConfig(),
                new MultiInteractorActivatableSyncedState(),
                "debug",
                LocalClientIDWrapperSetup.LocalClientIDWrapper,
                Substitute.For<IWorldStateSyncableContainer>());

            _v_pressurePlateProviderStub = new(pressurePlateService);

            _customerScript = Substitute.For<PluginActivatableScript>();
            _pressurePlatePluginInterface.OnActivate.AddListener(_customerScript.HandleActivateReceived);
            _pressurePlatePluginInterface.OnDeactivate.AddListener(_customerScript.HandleDeactivateReceived);
        }

        [Test]
        public void OnUserPressedDownAndReleased_OnCollidingWithPressurePlate_CustomerScriptTriggersOnActivateAndOnDeactivate([Random((ushort)0, ushort.MaxValue, 1)] ushort localClientID)
        {
            LocalClientIDWrapperSetup.LocalClientIDWrapper.Value.Returns(localClientID);
            ICollisionDetector feetCollider = CollisionDetectorFactoryStubSetup.CollisionDetectorFactoryStub.CollisionDetectorStubs[ColliderType.Feet2D];

            feetCollider.OnCollideStart += Raise.Event<Action<ICollideInteractionModule>>(_pressurePlateCollideInterface.CollideInteractionModule);
            _customerScript.Received(1).HandleActivateReceived();
            Assert.IsTrue(_pressurePlatePluginInterface.IsActivated, "Activatable should be activated");
            Assert.AreEqual(_pressurePlatePluginInterface.MostRecentInteractingClientID.Value, localClientID);
            Assert.IsTrue(_pressurePlatePluginInterface.MostRecentInteractingClientID.IsLocal);

            feetCollider.OnCollideEnd += Raise.Event<Action<ICollideInteractionModule>>(_pressurePlateCollideInterface.CollideInteractionModule);
            _customerScript.Received(1).HandleDeactivateReceived();
            Assert.IsFalse(_pressurePlatePluginInterface.IsActivated, "Activatable should be deactivated");
            Assert.AreEqual(_pressurePlatePluginInterface.MostRecentInteractingClientID.Value, localClientID);
            Assert.IsTrue(_pressurePlatePluginInterface.MostRecentInteractingClientID.IsLocal);
        }

        [TearDown]
        public void TearDownAfterEveryTest()
        {
            _customerScript.ClearReceivedCalls();

            _pressurePlatePluginInterface.OnActivate.RemoveAllListeners();
            _pressurePlatePluginInterface.OnDeactivate.RemoveAllListeners();

            _v_pressurePlateProviderStub.TearDown();
        }
    }
}
