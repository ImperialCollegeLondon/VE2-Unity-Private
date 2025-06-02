using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Internal;


namespace VE2.Core.VComponents.Tests
{
    [TestFixture]
    [Category("Handheld Activatable Tests")]
    internal class HandheldActivatableTests
    {
        private IV_HandheldActivatable _activatablePluginInterface => _v_handheldActivatableProviderStub;
        private V_HandheldActivatableProviderStub _v_handheldActivatableProviderStub;

        private IV_HandheldActivatable _activatableHoldPluginInterface => _v_handheldHoldActivatableProviderStub;
        private V_HandheldActivatableProviderStub _v_handheldHoldActivatableProviderStub;
        private PluginActivatableScript _customerScript;

        //Setup Once for every single test in this test class
        [OneTimeSetUp]
        public void SetUpOnce()
        {
            //Create the activatable
            HandheldActivatableService handheldActivatable = new(
                Substitute.For<IV_FreeGrabbable>(),
                new HandheldActivatableConfig(), 
                new SingleInteractorActivatableState(), 
                "debug", 
                Substitute.For<IWorldStateSyncableContainer>(),
                new ActivatableGroupsContainer(),
                Substitute.For<IClientIDWrapper>());

            //Stub out the VC (provider layer) with the activatable
            _v_handheldActivatableProviderStub = new(handheldActivatable);

            //Create the activatable hold
            HandheldActivatableConfig handheldActivatableConfig = new()
            {
                StateConfig = new ToggleActivatableStateConfig(),
                HandheldClickInteractionConfig = new()
                {
                    IsHoldMode = true
                },
                GeneralInteractionConfig = new GeneralInteractionConfig()
            };

            HandheldActivatableService handheldActivatableHold = new(
                Substitute.For<IV_FreeGrabbable>(),
                handheldActivatableConfig,
                new SingleInteractorActivatableState(),
                "debug",
                Substitute.For<IWorldStateSyncableContainer>(),
                new ActivatableGroupsContainer(),
                Substitute.For<IClientIDWrapper>());

            //Stub out the VC (provider layer) with the activatable hold
            _v_handheldHoldActivatableProviderStub = new(handheldActivatableHold);

            //Wire up the customer script to receive the events           
            _customerScript = Substitute.For<PluginActivatableScript>();
            _activatablePluginInterface.OnActivate.AddListener(_customerScript.HandleActivateReceived);
            _activatablePluginInterface.OnDeactivate.AddListener(_customerScript.HandleDeactivateReceived);
        }

        [Test]
        public void HandheldActivatable_WhenActivatedByPlugin_EmitsToPlugin()
        {
            //Wire up the customer script to receive the events
            PluginActivatableScript customerScript = Substitute.For<PluginActivatableScript>();
            _activatablePluginInterface.OnActivate.AddListener(customerScript.HandleActivateReceived);
            _activatablePluginInterface.OnDeactivate.AddListener(customerScript.HandleDeactivateReceived);

            //Activate, Check customer received the activation, and that the interactorID reflects programmatic activation (ie, null!)
            _activatablePluginInterface.Activate();
            customerScript.Received(1).HandleActivateReceived();
            Assert.IsTrue(_activatablePluginInterface.IsActivated);
            Assert.AreEqual(_activatablePluginInterface.MostRecentInteractingClientID, null);

            //Deactivate and do checks
            _activatablePluginInterface.Deactivate();
            customerScript.Received(1).HandleDeactivateReceived();
            Assert.IsFalse(_activatablePluginInterface.IsActivated);
            Assert.AreEqual(_activatablePluginInterface.MostRecentInteractingClientID, null);
        }

        [Test]
        public void HandheldActivatableHoldType_WhenActivatedByPlugin_EmitsToPlugin()
        {
            //Wire up the customer script to receive the events
            PluginActivatableScript customerScript = Substitute.For<PluginActivatableScript>();
            _activatableHoldPluginInterface.OnActivate.AddListener(customerScript.HandleActivateReceived);
            _activatableHoldPluginInterface.OnDeactivate.AddListener(customerScript.HandleDeactivateReceived);

            //Activate, Check customer received the activation
            _activatableHoldPluginInterface.Activate();

            customerScript.Received(1).HandleActivateReceived();
            Assert.IsTrue(_activatableHoldPluginInterface.IsActivated);
            Assert.AreEqual(_activatableHoldPluginInterface.MostRecentInteractingClientID, null);

            //Deactivate: Check that the click up event was received
            _activatableHoldPluginInterface.Deactivate();

            customerScript.Received(1).HandleDeactivateReceived();
            Assert.IsFalse(_activatableHoldPluginInterface.IsActivated);
            Assert.AreEqual(_activatableHoldPluginInterface.MostRecentInteractingClientID, null);
        }
    }
    
    internal partial class V_HandheldActivatableProviderStub : IV_HandheldActivatable
    {
        #region State Module Interface
        internal ISingleInteractorActivatableStateModule _StateModule => Service.StateModule;

        public UnityEvent OnActivate => _StateModule.OnActivate;
        public UnityEvent OnDeactivate => _StateModule.OnDeactivate;

        public bool IsActivated  => _StateModule.IsActivated;
        public void Activate() => _StateModule.Activate();
        public void Deactivate() => _StateModule.Deactivate();
        public void SetActivated(bool isActivated) => _StateModule.SetActivated(isActivated);
        public IClientIDWrapper MostRecentInteractingClientID => _StateModule.MostRecentInteractingClientID;
        #endregion

        #region Handheld Interaction Module Interface
        internal IHandheldClickInteractionModule _HandheldClickModule => Service.HandheldClickInteractionModule;
        #endregion

        #region General Interaction Module Interface
        public bool AdminOnly {get => _HandheldClickModule.AdminOnly; set => _HandheldClickModule.AdminOnly = value; }
        public bool EnableControllerVibrations { get => _HandheldClickModule.EnableControllerVibrations; set => _HandheldClickModule.EnableControllerVibrations = value; }
        public bool ShowTooltipsAndHighlight { get => _HandheldClickModule.ShowTooltipsAndHighlight; set => _HandheldClickModule.ShowTooltipsAndHighlight = value; }
        #endregion
    }

    internal partial class V_HandheldActivatableProviderStub
    {
        internal IHandheldClickInteractionModule HandheldClickInteractionModule => Service.HandheldClickInteractionModule;
        public HandheldActivatableService Service { private get; set; }

        public V_HandheldActivatableProviderStub(HandheldActivatableService service)
        {
            Service = service;
        }

        public V_HandheldActivatableProviderStub()
        {
            // Default constructor for NSubstitute
        }

        public void TearDown()
        {
            Service.TearDown();
        }
    }
}


