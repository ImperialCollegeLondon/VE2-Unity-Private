using NSubstitute;
using NUnit.Framework;
using VE2.Core.VComponents.PluginInterfaces;
using VE2.Core.VComponents.InteractableFindables;
using VE2.Core.VComponents.Internal;
using System;
using VE2.Core.VComponents.Tests;
using VE2.Core.Player;
using VE2.Common;


namespace VE2.Core.Tests
{
    [TestFixture]
    [Category("Player and FreeGrabbable Tests")]
    public class PlayerAndFreeGrabbableTests
    {
        private IV_FreeGrabbable _grabbablePluginInterface;
        private IRangedGrabPlayerInteractableIntegrator _grabbableRaycastInterface;
        private PluginGrabbableScript _customerScript;
        private V_FreeGrabbableStub _v_freeGrabbableStub;
        private PlayerService _playerServiceStub;

        [OneTimeSetUp]
        public void SetUpOnce()
        {
            //substitute for the customer script
            _customerScript = Substitute.For<PluginGrabbableScript>();
        }

        [SetUp]
        public void SetUpBeforeEveryTest()
        {           
            //Create the free grabbable
            FreeGrabbableService freeGrabbableService = FreeGrabbableServiceStubFactory.Create(interactorContainer: InteractorSetup.InteractorContainerStub);

            _v_freeGrabbableStub = new(freeGrabbableService);

            _grabbablePluginInterface = _v_freeGrabbableStub;
            _grabbableRaycastInterface = _v_freeGrabbableStub;

            //hook up the listeners to the IV_grabbable
            _grabbablePluginInterface.OnGrab.AddListener(_customerScript.HandleGrabReceived);
            _grabbablePluginInterface.OnDrop.AddListener(_customerScript.HandleDropReceived);

            //Create the player service
            _playerServiceStub = new(
                new PlayerTransformData(),
                new PlayerStateConfig(),
                false,
                true,
                new PlayerStateModuleContainer(),
                InteractorSetup.InteractorContainerStub,
                PlayerSettingsProviderSetup.PlayerSettingsProviderStub,
                Substitute.For<IPlayerAppearanceOverridesProvider>(),
                MultiplayerSupportSetup.MultiplayerSupportStub,
                InputHandlerSetup.PlayerInputContainerStubWrapper.PlayerInputContainer,
                RayCastProviderSetup.RaycastProviderStub, 
                Substitute.For<IXRManagerWrapper>()
            );
        }
        
        [Test]
        public void WithHoveringGrabbable_OnUserGrab_CustomerScriptReceivesOnGrab()
        {
            //stub the result of the raycast to return the grabbable's interaction module
            RayCastProviderSetup.StubRangedInteractionModuleForRaycastProviderStub(_grabbableRaycastInterface.RangedGrabInteractionModule);

            //Invoke grab, check customer received the grab, and that the interactorID is set
            InputHandlerSetup.PlayerInputContainerStubWrapper.Grab2D.OnPressed += Raise.Event<Action>();
            _customerScript.Received(1).HandleGrabReceived();
            Assert.IsTrue(_grabbablePluginInterface.IsGrabbed);
            Assert.AreEqual(_grabbablePluginInterface.MostRecentInteractingClientID, MultiplayerSupportSetup.LocalClientID);

            //Invoke drop, Check customer received the drop, and that the interactorID is set
            InputHandlerSetup.PlayerInputContainerStubWrapper.Grab2D.OnPressed += Raise.Event<Action>();
            _customerScript.Received(1).HandleDropReceived();
            Assert.IsFalse(_grabbablePluginInterface.IsGrabbed);
            Assert.AreEqual(_grabbablePluginInterface.MostRecentInteractingClientID, MultiplayerSupportSetup.LocalClientID);
        }

        [TearDown]
        public void TearDownAfterEveryTest()
        {
            _customerScript.ClearReceivedCalls();  

            _grabbablePluginInterface.OnGrab.RemoveAllListeners();
            _grabbablePluginInterface.OnDrop.RemoveAllListeners();

            _v_freeGrabbableStub.TearDown();
            _grabbablePluginInterface = null;   
            _grabbableRaycastInterface = null;

            _playerServiceStub.TearDown();
        }

        [OneTimeTearDown]
        public void TearDownOnce() { }
    }
}
