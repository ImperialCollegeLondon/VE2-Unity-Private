using System.Collections.Generic;
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
    //  COMMENTED OUT TILL THERE IS A PROGRAMMATIC API FOR GRABBING

    // [TestFixture]
    // [Category("Grabbable Service Tests")]
    // public class FreeGrabbableTest
    // {
    //     private IV_FreeGrabbable _grabbablePluginInterface;
    //     private IRangedGrabInteractionModule _grabbablePlayerInterface;
    //     private PluginGrabbableScript _customerScript;
    //     private V_FreeGrabbableStub _v_freeGrabbableStub;
    //     private InteractorID _interactorID;
    //     private InteractorContainer _interactorContainerStub = new();

    //     [OneTimeSetUp]
    //     public void SetUpOnce()
    //     {
    //         //Create an 
    //         System.Random random = new();
    //         ushort localClientID = (ushort)random.Next(0, ushort.MaxValue);
    //         _interactorID = new(localClientID, InteractorType.Mouse2D);

    //         IInteractor interactorStub = Substitute.For<IInteractor>();
    //         _interactorContainerStub.RegisterInteractor(_interactorID.ToString(), interactorStub);

    //         _customerScript = Substitute.For<PluginGrabbableScript>();
    //     }

    //     [SetUp]
    //     public void SetUpBeforeEveryTest() 
    //     {
    //         FreeGrabbableService freeGrabbableService = FreeGrabbableServiceStubFactory.Create(interactorContainer: _interactorContainerStub);

    //         _v_freeGrabbableStub = new(freeGrabbableService);

    //         _grabbablePluginInterface = _v_freeGrabbableStub;

    //         IRangedGrabPlayerInteractableIntegrator grabbableRaycastInterface = _v_freeGrabbableStub;
    //         _grabbablePlayerInterface = grabbableRaycastInterface.RangedGrabInteractionModule; 

    //         _grabbablePluginInterface.OnGrab.AddListener(_customerScript.HandleGrabReceived);
    //         _grabbablePluginInterface.OnDrop.AddListener(_customerScript.HandleDropReceived);
    //     }

    //     [Test]
    //     public void FreeGrabbable_WhenGrabbed_EmitsToPlugin()
    //     {
    //         //Invoke grab, check customer received the grab, and that the interactorID is set
    //         _grabbablePlayerInterface.RequestLocalGrab(_interactorID);
    //         _customerScript.Received(1).HandleGrabReceived();
    //         Assert.IsTrue(_grabbablePluginInterface.IsGrabbed);
    //         Assert.AreEqual(_grabbablePluginInterface.MostRecentInteractingClientID, _interactorID.ClientID);

    //         //Invoke drop, Check customer received the drop, and that the interactorID is set
    //         _grabbablePlayerInterface.RequestLocalDrop(_interactorID);
    //         _customerScript.Received(1).HandleDropReceived();
    //         Assert.IsFalse(_grabbablePluginInterface.IsGrabbed);
    //         Assert.AreEqual(_grabbablePluginInterface.MostRecentInteractingClientID, _interactorID.ClientID);
    //     }

    //     [TearDown]
    //     public void TearDownAfterEveryTest()
    //     {
    //         _customerScript.ClearReceivedCalls();  
            
    //         _grabbablePluginInterface.OnGrab.RemoveAllListeners();
    //         _grabbablePluginInterface.OnDrop.RemoveAllListeners();

    //         _v_freeGrabbableStub.TearDown();
    //         _grabbablePlayerInterface = null;
    //         _grabbablePluginInterface = null;      
    //     }

    //     [OneTimeTearDown]
    //     public void TearDownOnce() { }
    // }

    public class PluginGrabbableScript
    {
        public virtual void HandleGrabReceived() { }

        public virtual void HandleDropReceived() { }
    }

    public class V_FreeGrabbableStub : IV_FreeGrabbable, IRangedGrabPlayerInteractableIntegrator
    {
        #region Plugin Interfaces     
        IFreeGrabbableStateModule IV_FreeGrabbable._StateModule => _FreeGrabbableService.StateModule;
        IRangedGrabInteractionModule IV_FreeGrabbable._RangedGrabModule => _FreeGrabbableService.RangedGrabInteractionModule;
        #endregion

        #region Player Interfaces
        IRangedInteractionModule IRangedPlayerInteractableIntegrator.RangedInteractionModule => _FreeGrabbableService.RangedGrabInteractionModule;
        #endregion

        protected FreeGrabbableService _FreeGrabbableService = null;

        public V_FreeGrabbableStub(FreeGrabbableService service)
        {
            _FreeGrabbableService = service;
        }

        public void TearDown()
        {
            _FreeGrabbableService.TearDown();
            _FreeGrabbableService = null;
        }
    }

    public static class FreeGrabbableServiceStubFactory
    {
        public static FreeGrabbableService Create(
            List<IHandheldInteractionModule> handheldInteractionModules = null,
            FreeGrabbableConfig config = null,
            FreeGrabbableState state = null,
            string debugName = "debug",
            WorldStateModulesContainer worldStateModulesContainer = null,
            InteractorContainer interactorContainer = null,
            IRigidbodyWrapper rigidbodyWrapper = null,
            PhysicsConstants physicsConstants = null)
        {
            handheldInteractionModules ??= new List<IHandheldInteractionModule>();
            config ??= new FreeGrabbableConfig();
            state ??= new FreeGrabbableState();
            worldStateModulesContainer ??= Substitute.For<WorldStateModulesContainer>();
            interactorContainer ??= new InteractorContainer();
            rigidbodyWrapper ??= Substitute.For<IRigidbodyWrapper>();
            physicsConstants ??= new PhysicsConstants();

            FreeGrabbableService freeGrabbableService = new(
                handheldInteractionModules,
                config,
                state,
                debugName,
                worldStateModulesContainer,
                interactorContainer,
                rigidbodyWrapper,
                physicsConstants);

            return freeGrabbableService;
        }

    }
}
