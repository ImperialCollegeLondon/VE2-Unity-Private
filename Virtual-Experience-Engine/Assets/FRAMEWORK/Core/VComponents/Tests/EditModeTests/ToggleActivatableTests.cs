using NSubstitute;
using NUnit.Framework;
using VE2.Core.VComponents.PluginInterfaces;
using UnityEngine;
using VE2.Common;
using VE2.Core.VComponents.InteractableInterfaces;
using VE2.Core.VComponents.NonInteractableInterfaces;
using VE2.Core.VComponents.InteractableFindables;
using VE2.Core.VComponents.Internal;

namespace VE2.Core.VComponents.Tests
{
    [TestFixture]
    [Category("Activatable Service Tests")]
    public class ToggleActivatableTests
    {
        //Toggle activatable
        private IV_ToggleActivatable _activatablePluginInterface;
        private V_ToggleActivatableStub _v_activatableStub;

        private PluginActivatableMock _customerScript;

        //Setup Once for every single test in this test class
        [OneTimeSetUp]
        public void SetUpOnce()
        {
            //Substitute for the customer script           
            _customerScript = Substitute.For<PluginActivatableMock>();
        }

        //setup that runs before every test method in this class
        [SetUp]
        public void SetUpBeforeEveryTest() 
        { 
            //Create the activatable
            ToggleActivatableService toggleActivatable = ToggleActivatableServiceStubFactory.Create();
            _v_activatableStub = new(toggleActivatable);

            //Get interfaces
            _activatablePluginInterface = _v_activatableStub;

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

            _v_activatableStub.TearDown();
            _activatablePluginInterface = null;
        }

        //tear down that runs once after all the tests in this class
        [OneTimeTearDown]
        public void TearDownOnce() { }
    }

    public class PluginActivatableMock
    {
        public virtual void HandleActivateReceived() { }
        public virtual void HandleDeactivateReceived() { }
    }

    public class V_ToggleActivatableStub : IV_ToggleActivatable, IRangedClickPlayerInteractableIntegrator, ICollidePlayerInteractableIntegrator
    {
        #region Plugin Interfaces
        ISingleInteractorActivatableStateModule IV_ToggleActivatable._StateModule => _ToggleActivatableService.StateModule;
        IRangedClickInteractionModule IV_ToggleActivatable._RangedClickModule => _ToggleActivatableService.RangedClickInteractionModule;
        #endregion

        #region Player Interfaces
        ICollideInteractionModule ICollidePlayerInteractableIntegrator.CollideInteractionModule => _ToggleActivatableService.ColliderInteractionModule;
        IRangedInteractionModule IRangedPlayerInteractableIntegrator.RangedInteractionModule => _ToggleActivatableService.RangedClickInteractionModule;
        #endregion

        protected ToggleActivatableService _ToggleActivatableService = null;

        public V_ToggleActivatableStub(ToggleActivatableService ToggleActivatable)
        {
            _ToggleActivatableService = ToggleActivatable;
        }

        public void TearDown()
        {
            _ToggleActivatableService.TearDown();
            _ToggleActivatableService = null;
        }
    }

    public static class ToggleActivatableServiceStubFactory
    {
        //factory method to create the activatable stub
        public static ToggleActivatableService Create(
            ToggleActivatableConfig config = null,
            SingleInteractorActivatableState interactorState = null,
            string debugName = "debug",
            WorldStateModulesContainer worldStateModules = null
        )
        {
            // Use defaults if parameters are not provided
            config ??= new ToggleActivatableConfig();
            interactorState ??= new SingleInteractorActivatableState();
            worldStateModules ??= Substitute.For<WorldStateModulesContainer>();

            //Create the activatable with default values
            ToggleActivatableService toggleActivatable = new(config, interactorState, debugName, worldStateModules);

            return toggleActivatable;
        }
    }
}
