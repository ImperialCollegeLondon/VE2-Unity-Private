using System;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
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
        private V_HoldActivatableProviderStub _v_holdActivatableProviderStub;
        private PluginActivatableScript _customerScript;

        [SetUp]
        public void SetUpBeforeEveryTest()
        {
            HoldActivatableService holdActivatableService = new(
                new HoldActivatableConfig(),
                new MultiInteractorActivatableState(),
                "debug");

            _v_holdActivatableProviderStub = new(holdActivatableService);

            _customerScript = Substitute.For<PluginActivatableScript>();
            _holdActivatablePluginInterface.OnActivate.AddListener(_customerScript.HandleActivateReceived);
            _holdActivatablePluginInterface.OnDeactivate.AddListener(_customerScript.HandleDeactivateReceived);
        }

        [Test]
        public void OnUserPressedDownAndReleased_WithHoveringActivatable_CustomerScriptTriggersOnActivateAndOnDeactivate([Random((ushort)0, ushort.MaxValue, 1)] ushort localClientID)
        {
            RayCastProviderSetup.StubRangedInteractionModuleForRaycastProviderStub(_holdActivatableRaycastInterface.RangedInteractionModule);
            LocalClientIDProviderSetup.LocalClientIDProviderStub.LocalClientID.Returns(localClientID);

            PlayerInputContainerSetup.RangedClick2D.OnPressed += Raise.Event<Action>();
            _customerScript.Received(1).HandleActivateReceived();
            Assert.IsTrue(_holdActivatablePluginInterface.IsActivated, "Activatable should be activated");
            Assert.AreEqual(_holdActivatablePluginInterface.MostRecentInteractingClientID, localClientID);

            PlayerInputContainerSetup.RangedClick2D.OnReleased += Raise.Event<Action>();
            _customerScript.Received(1).HandleDeactivateReceived();
            Assert.IsFalse(_holdActivatablePluginInterface.IsActivated, "Activatable should be deactivated");
            Assert.AreEqual(_holdActivatablePluginInterface.MostRecentInteractingClientID, localClientID);
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
