using NSubstitute;
using NUnit.Framework;
using System;
using VE2.Core.VComponents.Tests;
using UnityEngine;
using VE2.Core.VComponents.Internal;
using VE2.Core.VComponents.API;

namespace VE2.Core.Tests
{
    [TestFixture]
    [Category("Player and Toggle Activatable Tests")]
    internal class PlayerAndToggleActivatableTests : PlayerServiceSetupFixture
    {
        // Using more descriptive names
        private IV_ToggleActivatable FirstActivatable => _v_activatableProviderStub;
        private IRangedClickInteractionModuleProvider FirstRaycast => _v_activatableProviderStub;
        private V_ToggleActivatableProviderStub _v_activatableProviderStub;
        private PluginActivatableScript _customerScript;

        private IV_ToggleActivatable SecondActivatable => _v_activatableProviderStub2;
        private IRangedClickInteractionModuleProvider SecondRaycast => _v_activatableProviderStub2;
        private V_ToggleActivatableProviderStub _v_activatableProviderStub2;
        private PluginActivatableScript _customerScript2;

        private ActivatableGroupsContainer _activatableGroupsContainer;

        #region Setup & Helper Methods

        private (V_ToggleActivatableProviderStub provider, PluginActivatableScript script) CreateActivatable(string debugLabel)
        {
            var config = new ToggleActivatableConfig
            {
                StateConfig = new ActivatableStateConfig
                {
                    UseActivationGroup = true,
                    ActivationGroupID = "TestGroup"
                },
                GeneralInteractionConfig = new GeneralInteractionConfig(),
                RangedInteractionConfig = new RangedInteractionConfig()
            };

            var service = new ToggleActivatableService(
                config,
                new SingleInteractorActivatableState(),
                debugLabel,
                Substitute.For<IWorldStateSyncService>(),
                _activatableGroupsContainer);

            var providerStub = new V_ToggleActivatableProviderStub(service);
            var customerScript = Substitute.For<PluginActivatableScript>();

            // Cast to IV_ToggleActivatable to access OnActivate/OnDeactivate.
            ((IV_ToggleActivatable)providerStub).OnActivate.AddListener(customerScript.HandleActivateReceived);
            ((IV_ToggleActivatable)providerStub).OnDeactivate.AddListener(customerScript.HandleDeactivateReceived);

            return (providerStub, customerScript);
        }

        private void SimulateClick()
        {
            PlayerInputContainerSetup.RangedClick2D.OnPressed += Raise.Event<Action>();
        }

        [SetUp]
        public void SetUpBeforeEveryTest()
        {
            _activatableGroupsContainer = new ActivatableGroupsContainer();

            // Initialize first activatable
            var first = CreateActivatable("debug");
            _v_activatableProviderStub = first.provider;
            _customerScript = first.script;

            // Initialize second activatable
            var second = CreateActivatable("debug2");
            _v_activatableProviderStub2 = second.provider;
            _customerScript2 = second.script;
        }
        #endregion

        #region Test Cases

        [Test]
        public void OnUserClick_WithHoveringActivatable_CustomerScriptReceivesOnActivate(
            [Random((ushort)0, ushort.MaxValue, 1)] ushort localClientID)
        {
            RayCastProviderSetup.StubRangedInteractionModuleForRaycastProviderStub(FirstRaycast.RangedClickInteractionModule);
            LocalClientIDProviderSetup.LocalClientIDProviderStub.LocalClientID.Returns(localClientID);

            // Simulate click to activate
            SimulateClick();
            _customerScript.Received(1).HandleActivateReceived();
            Assert.IsTrue(FirstActivatable.IsActivated, "Activatable should be activated");
            Assert.AreEqual(FirstActivatable.MostRecentInteractingClientID, localClientID);

            // Simulate click to deactivate
            SimulateClick();
            _customerScript.Received(1).HandleDeactivateReceived();
            Assert.IsFalse(FirstActivatable.IsActivated, "Activatable should be deactivated");
            Assert.AreEqual(FirstActivatable.MostRecentInteractingClientID, localClientID);
        }

        [Test]
        public void OnUserClick_WithHoveringActivatable_CustomerScriptReceivesOnActivate_DeactivatesOthersInGroup(
            [Random((ushort)0, ushort.MaxValue, 1)] ushort localClientID)
        {
            // Stub first activatable's module and set client ID
            RayCastProviderSetup.StubRangedInteractionModuleForRaycastProviderStub(FirstRaycast.RangedClickInteractionModule);
            LocalClientIDProviderSetup.LocalClientIDProviderStub.LocalClientID.Returns(localClientID);

            // Activate first activatable
            SimulateClick();
            _customerScript.Received(1).HandleActivateReceived();
            Assert.IsTrue(FirstActivatable.IsActivated, "First activatable should be activated");
            Assert.AreEqual(FirstActivatable.MostRecentInteractingClientID, localClientID);

            // Stub second activatable's module
            RayCastProviderSetup.StubRangedInteractionModuleForRaycastProviderStub(SecondRaycast.RangedClickInteractionModule);

            // Activate second activatable
            SimulateClick();
            _customerScript2.Received(1).HandleActivateReceived();
            Assert.IsTrue(SecondActivatable.IsActivated, "Second activatable should be activated");
            Assert.AreEqual(SecondActivatable.MostRecentInteractingClientID, localClientID);

            // Verify first activatable deactivation
            Assert.IsFalse(FirstActivatable.IsActivated, "First activatable should be deactivated");
            _customerScript.Received(1).HandleDeactivateReceived();
        }

        #endregion

        #region Teardown

        [TearDown]
        public void TearDownAfterEveryTest()
        {
            _customerScript.ClearReceivedCalls();
            _customerScript2?.ClearReceivedCalls();

            FirstActivatable.OnActivate.RemoveAllListeners();
            FirstActivatable.OnDeactivate.RemoveAllListeners();

            SecondActivatable?.OnActivate.RemoveAllListeners();
            SecondActivatable?.OnDeactivate.RemoveAllListeners();

            _v_activatableProviderStub.TearDown();
            _v_activatableProviderStub2?.TearDown();
        }
        #endregion
    }
}
