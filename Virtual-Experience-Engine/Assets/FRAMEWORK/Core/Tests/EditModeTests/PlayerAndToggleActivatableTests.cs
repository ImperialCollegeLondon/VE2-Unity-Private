using NSubstitute;
using NUnit.Framework;
using System;
using UnityEngine;
using VE2.Core.VComponents.Internal;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Tests;
using VE2.Core.Player.Internal;
using VE2.Common.Shared;


namespace VE2.Core.Tests
{
    [TestFixture]
    [Category("Player and Toggle Activatable Tests")]
    internal class PlayerAndToggleActivatableTests : PlayerServiceSetupFixture
    {
        // Using more descriptive names
        private IV_ToggleActivatable _firstActivatablePluginInterface => _v_activatableProviderStub;
        private IRangedToggleClickInteractionModuleProvider _firstActivatableRaycastInterface => _v_activatableProviderStub;
        private ICollideInteractionModuleProvider _firstActivatableCollideInterface => _v_activatableProviderStub;

        private V_ToggleActivatableProviderStub _v_activatableProviderStub;
        private PluginActivatableScript _customerScript;

        private IV_ToggleActivatable _secondActivatablePluginInterface => _v_activatableProviderStub2;
        private IRangedToggleClickInteractionModuleProvider _secondActivatableRaycastInterface => _v_activatableProviderStub2;
        private V_ToggleActivatableProviderStub _v_activatableProviderStub2;
        private PluginActivatableScript _customerScript2;

        private ActivatableGroupsContainer _activatableGroupsContainer;

        #region Setup & Helper Methods

        private (V_ToggleActivatableProviderStub provider, PluginActivatableScript script) CreateActivatable(string debugLabel)
        {
            var config = new ToggleActivatableConfig
            {
                StateConfig = new ToggleActivatableStateConfig
                {
                    UseActivationGroup = true,
                    ActivationGroupID = "TestGroup"
                },
                GeneralInteractionConfig = new GeneralInteractionConfig(),
                RangedClickInteractionConfig = new RangedClickInteractionConfig()
            };

            var service = new ToggleActivatableService(
                config,
                new SingleInteractorActivatableState(),
                debugLabel,
                Substitute.For<IWorldStateSyncableContainer>(),
                _activatableGroupsContainer,
                LocalClientIDWrapperSetup.LocalClientIDWrapper);

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
        public void OnUserClick_WithHoveringActivatable_CustomerScriptReceivesOnActivate([Random((ushort)0, ushort.MaxValue, 1)] ushort localClientID)
        {
            RayCastProviderSetup.StubRangedInteractionModuleForRaycast(_firstActivatableRaycastInterface.RangedToggleClickInteractionModule);
            LocalClientIDWrapperSetup.LocalClientIDWrapper.Value.Returns(localClientID);

            // Simulate click to activate
            SimulateClick();
            _customerScript.Received(1).HandleActivateReceived();

            Assert.IsTrue(_firstActivatablePluginInterface.IsActivated, "Activatable should be activated");
            Assert.AreEqual(_firstActivatablePluginInterface.MostRecentInteractingClientID.Value, localClientID);
            Assert.IsTrue(_firstActivatablePluginInterface.MostRecentInteractingClientID.IsLocal);

            // Simulate click to deactivate
            SimulateClick();
            _customerScript.Received(1).HandleDeactivateReceived();

            Assert.IsFalse(_firstActivatablePluginInterface.IsActivated, "Activatable should be deactivated");
            Assert.AreEqual(_firstActivatablePluginInterface.MostRecentInteractingClientID.Value, localClientID);
            Assert.IsTrue(_firstActivatablePluginInterface.MostRecentInteractingClientID.IsLocal);
        }

        [Test]
        public void OnUserClick_WithHoveringActivatable_CustomerScriptReceivesOnActivate_DeactivatesOthersInGroup([Random((ushort)0, ushort.MaxValue, 1)] ushort localClientID)
        {
            // Stub first activatable's module and set client ID
            RayCastProviderSetup.StubRangedInteractionModuleForRaycast(_firstActivatableRaycastInterface.RangedToggleClickInteractionModule);
            LocalClientIDWrapperSetup.LocalClientIDWrapper.Value.Returns(localClientID);

            // Activate first activatable
            SimulateClick();
            _customerScript.Received(1).HandleActivateReceived();
            Assert.IsTrue(_firstActivatablePluginInterface.IsActivated, "First activatable should be activated");
            Assert.AreEqual(_firstActivatablePluginInterface.MostRecentInteractingClientID.Value, localClientID);
            Assert.IsTrue(_firstActivatablePluginInterface.MostRecentInteractingClientID.IsLocal);

            // Stub second activatable's module
            RayCastProviderSetup.StubRangedInteractionModuleForRaycast(_secondActivatableRaycastInterface.RangedToggleClickInteractionModule);

            // Activate second activatable
            SimulateClick();
            _customerScript2.Received(1).HandleActivateReceived();
            Assert.IsTrue(_secondActivatablePluginInterface.IsActivated, "Second activatable should be activated");
            Assert.AreEqual(_secondActivatablePluginInterface.MostRecentInteractingClientID.Value, localClientID);
            Assert.IsTrue(_firstActivatablePluginInterface.MostRecentInteractingClientID.IsLocal);

            // Verify first activatable deactivation
            Assert.IsFalse(_firstActivatablePluginInterface.IsActivated, "First activatable should be deactivated");
            _customerScript.Received(1).HandleDeactivateReceived();
        }

        [Test]
        public void OnUserCollideEnterInVR_CollidingWithActivatable_CustomerReceivesOnActivate([Random((ushort)0, ushort.MaxValue, 1)] ushort localClientID)
        {
            LocalClientIDWrapperSetup.LocalClientIDWrapper.Value.Returns(localClientID);
            ICollisionDetector handCollider = CollisionDetectorFactoryStubSetup.CollisionDetectorFactoryStub.CollisionDetectorStubs[ColliderType.HandVRLeft];

            PlayerInputContainerSetup.PlayerInputContainerStub.ChangeMode.OnPressed += Raise.Event<Action>();
            Assert.IsTrue(PlayerService.IsVRMode, "Player should be in VR mode");

            handCollider.OnCollideStart += Raise.Event<Action<ICollideInteractionModule>>(_firstActivatableCollideInterface.CollideInteractionModule);
            _customerScript.Received(1).HandleActivateReceived();
            Assert.IsTrue(_firstActivatablePluginInterface.IsActivated, "Activatable should be activated");
            Assert.AreEqual(_firstActivatablePluginInterface.MostRecentInteractingClientID.Value, localClientID);
            Assert.IsTrue(_firstActivatablePluginInterface.MostRecentInteractingClientID.IsLocal);

            handCollider.OnCollideEnd += Raise.Event<Action<ICollideInteractionModule>>(_firstActivatableCollideInterface.CollideInteractionModule);
            handCollider.OnCollideStart += Raise.Event<Action<ICollideInteractionModule>>(_firstActivatableCollideInterface.CollideInteractionModule);
            _customerScript.Received(1).HandleDeactivateReceived();
            Assert.IsFalse(_firstActivatablePluginInterface.IsActivated, "Activatable should be deactivated");
            Assert.AreEqual(_firstActivatablePluginInterface.MostRecentInteractingClientID.Value, localClientID);
            Assert.IsTrue(_firstActivatablePluginInterface.MostRecentInteractingClientID.IsLocal);
        }

        #endregion

        #region Teardown

        [TearDown]
        public void TearDownAfterEveryTest()
        {
            _customerScript.ClearReceivedCalls();
            _customerScript2?.ClearReceivedCalls();

            _firstActivatablePluginInterface.OnActivate.RemoveAllListeners();
            _firstActivatablePluginInterface.OnDeactivate.RemoveAllListeners();

            _secondActivatablePluginInterface?.OnActivate.RemoveAllListeners();
            _secondActivatablePluginInterface?.OnDeactivate.RemoveAllListeners();

            _v_activatableProviderStub.TearDown();
            _v_activatableProviderStub2?.TearDown();
        }
        #endregion
    }
}
