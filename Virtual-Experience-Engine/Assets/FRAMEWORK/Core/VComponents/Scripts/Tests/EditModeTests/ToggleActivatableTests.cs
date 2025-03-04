using NSubstitute;
using NUnit.Framework;
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

        //Setup Once for every single test in this test class
        [OneTimeSetUp]
        public void SetUpOnce()
        {
            //Create the activatable
            ToggleActivatableService toggleActivatable = new(new ToggleActivatableConfig(), new SingleInteractorActivatableState(), "debug", Substitute.For<IWorldStateSyncService>());

            //Stub out the VC (provider layer) with the activatable
            _v_toggleActivatableProviderStub = new(toggleActivatable);

            //Wire up the customer script to receive the events           
            _customerScript = Substitute.For<PluginActivatableScript>();
            _activatablePluginInterface.OnActivate.AddListener(_customerScript.HandleActivateReceived);
            _activatablePluginInterface.OnDeactivate.AddListener(_customerScript.HandleDeactivateReceived);
        }

        //test method to confirm that the activatable emits the correct events when Activated/Deactivated
        [Test]
        public void PushActivatable_WhenClicked_EmitsToPlugin()
        {
            //Invoke click, Check customer received the activation, and that the interactorID is set
            _activatablePluginInterface.IsActivated = true;
            _customerScript.Received(1).HandleActivateReceived();
            Assert.IsTrue(_activatablePluginInterface.IsActivated, "Activatable should be activated");
            Assert.AreEqual(_activatablePluginInterface.MostRecentInteractingClientID, ushort.MaxValue);

            // Invoke the click to deactivate
            _activatablePluginInterface.IsActivated = false;
            _customerScript.Received(1).HandleDeactivateReceived();
            Assert.IsFalse(_activatablePluginInterface.IsActivated, "Activatable should be deactivated");
            Assert.AreEqual(_activatablePluginInterface.MostRecentInteractingClientID, ushort.MaxValue);
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
    }

    internal class PluginActivatableScript
    {
        public virtual void HandleActivateReceived() { }
        public virtual void HandleDeactivateReceived() { }
    }

    internal class V_ToggleActivatableProviderStub : IV_ToggleActivatable, IRangedClickInteractionModuleProvider, ICollideInteractionModuleProvider
    {
        #region Plugin Interfaces
        ISingleInteractorActivatableStateModule IV_ToggleActivatable._StateModule => _ToggleActivatable.StateModule;
        IRangedClickInteractionModule IV_ToggleActivatable._RangedClickModule => _ToggleActivatable.RangedClickInteractionModule;
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
