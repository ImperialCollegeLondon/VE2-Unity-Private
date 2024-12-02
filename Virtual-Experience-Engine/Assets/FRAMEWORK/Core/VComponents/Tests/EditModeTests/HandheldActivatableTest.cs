using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using VE2.Common;
using VE2.Core.VComponents.InteractableFindables;
using VE2.Core.VComponents.InteractableInterfaces;
using VE2.Core.VComponents.Internal;
using VE2.Core.VComponents.NonInteractableInterfaces;
using VE2.Core.VComponents.PluginInterfaces;


namespace VE2.Core.VComponents.Tests
{
    [TestFixture]
    [Category("Handheld Activatable Service Tests")]
    public class HandheldActivatableTest
    {
        private IV_HandheldActivatable _handheldActivatablePluginInterface;
        private PluginActivatableMock _customerScript;
        private V_HandheldActivatableStub _v_handheldActivatableStub;

        [OneTimeSetUp]
        public void SetUpOnce()
        {
            _customerScript = Substitute.For<PluginActivatableMock>();
        }

        [SetUp]
        public void SetUp()
        {
            HandheldActivatableService handheldActivatable = HandheldActivatableServiceStubFactory.Create();
            _v_handheldActivatableStub = new(handheldActivatable);

            _handheldActivatablePluginInterface = _v_handheldActivatableStub;

            _handheldActivatablePluginInterface.OnActivate.AddListener(_customerScript.HandleActivateReceived);
            _handheldActivatablePluginInterface.OnDeactivate.AddListener(_customerScript.HandleDeactivateReceived);
        }

        [Test]
        public void HandheldActivatable_WhenActivatedByPlugin_EmitsToPlugin()
        {
            //Invoke click, Check customer received the activation, and that the interactorID is set
            _handheldActivatablePluginInterface.IsActivated = true;
            _customerScript.Received(1).HandleActivateReceived();
            Assert.IsTrue(_handheldActivatablePluginInterface.IsActivated);
            Assert.AreEqual(_handheldActivatablePluginInterface.MostRecentInteractingClientID, ushort.MaxValue);

            // Invoke the click to deactivate
            _handheldActivatablePluginInterface.IsActivated = false;
            _customerScript.Received(1).HandleDeactivateReceived();
            Assert.IsFalse(_handheldActivatablePluginInterface.IsActivated);
            Assert.AreEqual(_handheldActivatablePluginInterface.MostRecentInteractingClientID, ushort.MaxValue);
        }
        
        //tear down that runs after every test method in this class
        [TearDown]
        public void TearDownAfterEveryTest()
        {
            _customerScript.ClearReceivedCalls();
            _handheldActivatablePluginInterface.OnActivate.RemoveAllListeners();
            _handheldActivatablePluginInterface.OnDeactivate.RemoveAllListeners();

            _v_handheldActivatableStub.TearDown();
            _handheldActivatablePluginInterface = null;
        }

        //tear down that runs once after all the tests in this class
        [OneTimeTearDown]
        public void TearDownOnce() { }
    }

    public class V_HandheldActivatableStub : IV_HandheldActivatable
    {
        #region Plugin Interfaces
        ISingleInteractorActivatableStateModule IV_HandheldActivatable._StateModule => _HandheldActivatableService.StateModule;
        IHandheldClickInteractionModule IV_HandheldActivatable._HandheldClickModule => _HandheldActivatableService.HandheldClickInteractionModule;
        #endregion

        protected HandheldActivatableService _HandheldActivatableService = null;

        public V_HandheldActivatableStub(HandheldActivatableService HandheldActivatable)
        {
            _HandheldActivatableService = HandheldActivatable;
        }

        public void TearDown()
        {
            _HandheldActivatableService.TearDown();
            _HandheldActivatableService = null;
        }
    }

    public class HandheldActivatableServiceStubFactory
    {
        public static HandheldActivatableService Create(
            HandheldActivatableConfig config = null,
            SingleInteractorActivatableState stateModule = null,
            string debugName = "debug",
            WorldStateModulesContainer worldStateModulesContainer = null)
        {
            config ??= new HandheldActivatableConfig();
            stateModule ??= new SingleInteractorActivatableState();
            worldStateModulesContainer ??= new WorldStateModulesContainer();

            HandheldActivatableService handheldActivatable = new HandheldActivatableService(config, stateModule, debugName, worldStateModulesContainer);

            return handheldActivatable;
        }

    }
}


