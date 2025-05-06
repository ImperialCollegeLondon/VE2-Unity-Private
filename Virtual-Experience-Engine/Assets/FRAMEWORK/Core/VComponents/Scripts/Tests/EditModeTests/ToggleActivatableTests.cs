using NSubstitute;
using NUnit.Framework;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Internal;

namespace VE2.Core.VComponents.Tests
{
    [TestFixture]
    [Category("Activatable Service Tests")]
    internal class ToggleActivatableTests
    {
        private IV_ToggleActivatable _activatablePluginInterface => _v_toggleActivatableProviderStub;
        private V_ToggleActivatableProviderStub _v_toggleActivatableProviderStub;
        private PluginActivatableScript _customerScript;
        private ActivatableGroupsContainer _activatableGroupsContainer = new();
        private IV_ToggleActivatable _activatablePluginInterface2 => _v_toggleActivatableProviderStub2;
        private V_ToggleActivatableProviderStub _v_toggleActivatableProviderStub2;
        //Setup Once for every single test in this test class
        [OneTimeSetUp]
        public void SetUpOnce() { }

        [SetUp]
        public void SetUpBeforeEveryTest()
        {
            _activatableGroupsContainer.Reset();

            // Create the activatable with an activation group
            ToggleActivatableStateConfig stateConfig = new();
            stateConfig.ActivationGroupID = "testGroup";
            stateConfig.UseActivationGroup = true;
            var config = new ToggleActivatableConfig
            {
                StateConfig = stateConfig,
                GeneralInteractionConfig = new(),
                ActivatableRangedInteractionConfig = new()
            };
            ToggleActivatableService toggleActivatable = new(
                config, 
                new SingleInteractorActivatableState(), 
                "debug", 
                Substitute.For<IWorldStateSyncableContainer>(), 
                _activatableGroupsContainer,
                Substitute.For<IClientIDWrapper>());

            // Stub out the VC (provider layer) with the activatable
            _v_toggleActivatableProviderStub = new(toggleActivatable);

            // Wire up the customer script to receive the events
            _customerScript = Substitute.For<PluginActivatableScript>();
            _activatablePluginInterface.OnActivate.AddListener(_customerScript.HandleActivateReceived);
            _activatablePluginInterface.OnDeactivate.AddListener(_customerScript.HandleDeactivateReceived);
        }

        //test method to confirm that the activatable emits the correct events when Activated/Deactivated
        [Test]
        public void PushActivatable_WhenClicked_EmitsToPlugin()
        {
            //Activate, Check customer received the activation, and that the interactorID reflects programmatic activation (ie, null!)
            _activatablePluginInterface.Activate();
            _customerScript.Received(1).HandleActivateReceived();
            Assert.IsTrue(_activatablePluginInterface.IsActivated, "Activatable should be activated");
            Assert.AreEqual(_activatablePluginInterface.MostRecentInteractingClientID, null);

            //Deactivate and check
            _activatablePluginInterface.Deactivate();
            _customerScript.Received(1).HandleDeactivateReceived();
            Assert.IsFalse(_activatablePluginInterface.IsActivated, "Activatable should be deactivated");
            Assert.AreEqual(_activatablePluginInterface.MostRecentInteractingClientID, null);
        }

        //tear down that runs after every test method in this class
        [TearDown]
        public void TearDownAfterEveryTest()
        {
            _customerScript.ClearReceivedCalls();

            _activatablePluginInterface.OnActivate.RemoveAllListeners();
            _activatablePluginInterface.OnDeactivate.RemoveAllListeners();

            _v_toggleActivatableProviderStub.TearDown();
        }

        //tear down that runs once after all the tests in this class
        [OneTimeTearDown]
        public void TearDownOnce() { }

        // New test method
        [Test]
        public void PushActivatableInActivationGroup_WhenActivated_DeactivatesOthersInGroup()
        {
            // Setup a second ToggleActivatableService with the same activation group
            ToggleActivatableStateConfig stateConfig = new();
            stateConfig.ActivationGroupID = "testGroup";
            stateConfig.UseActivationGroup = true;
            var config = new ToggleActivatableConfig
            {
                StateConfig = stateConfig,
                GeneralInteractionConfig = new(),
                ActivatableRangedInteractionConfig = new()
            };
            ToggleActivatableService toggleActivatable2 = new(
                config, 
                new SingleInteractorActivatableState(), 
                "debug2", 
                Substitute.For<IWorldStateSyncableContainer>(), 
                _activatableGroupsContainer,
                Substitute.For<IClientIDWrapper>());

            // Stub out the VC (provider layer) with the activatable
            _v_toggleActivatableProviderStub2 = new(toggleActivatable2);

            // Wire up the second customer script to receive the events
            var customerScript2 = Substitute.For<PluginActivatableScript>();
            _activatablePluginInterface2.OnActivate.AddListener(customerScript2.HandleActivateReceived);
            _activatablePluginInterface2.OnDeactivate.AddListener(customerScript2.HandleDeactivateReceived);

            // Activate the first activatable
            _activatablePluginInterface.Activate();
            _customerScript.Received(1).HandleActivateReceived();
            Assert.IsTrue(_activatablePluginInterface.IsActivated, "First activatable should be activated");

            // Activate the second activatable
            _activatablePluginInterface2.Activate();
            customerScript2.Received(1).HandleActivateReceived();
            Assert.IsTrue(_activatablePluginInterface2.IsActivated, "Second activatable should be activated");

            // Check that the first activatable is deactivated
            Assert.IsFalse(_activatablePluginInterface.IsActivated, "First activatable should be deactivated");
            _customerScript.Received(1).HandleDeactivateReceived();
        }
    }

    internal class PluginActivatableScript
    {
        public virtual void HandleActivateReceived() { }
        public virtual void HandleDeactivateReceived() { }
    }

    internal class V_ToggleActivatableProviderStub : IV_ToggleActivatable, IRangedToggleClickInteractionModuleProvider, ICollideInteractionModuleProvider
    {
        #region Plugin Interfaces

        ISingleInteractorActivatableStateModule IV_ToggleActivatable._StateModule => _ToggleActivatable.StateModule;
        IRangedToggleClickInteractionModule IV_ToggleActivatable._RangedToggleClickModule => _ToggleActivatable.RangedClickInteractionModule;
        #endregion

        #region Player Interfaces
        ICollideInteractionModule ICollideInteractionModuleProvider.CollideInteractionModule => _ToggleActivatable.ColliderInteractionModule;
        IRangedInteractionModule IRangedInteractionModuleProvider.RangedInteractionModule => _ToggleActivatable.RangedClickInteractionModule;
        #endregion

        internal ToggleActivatableService _ToggleActivatable = null;

        internal V_ToggleActivatableProviderStub(ToggleActivatableService ToggleActivatable)
        {
            _ToggleActivatable = ToggleActivatable;
        }

        public void TearDown()
        {
            _ToggleActivatable.TearDown();
            _ToggleActivatable = null;
        }
    }
}
