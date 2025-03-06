using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Internal;

namespace VE2.Core.VComponents.Tests
{
    internal class FreeGrabbableTest
    {
        [Test]
        public void FreeGrabbable_WhenGrabbed_EmitsToPlugin()
        {
            //Create an ID
            System.Random random = new();
            ushort localClientID = (ushort)random.Next(0, ushort.MaxValue);
            InteractorID interactorID = new(localClientID, InteractorType.Mouse2D);

            IInteractor interactorStub = Substitute.For<IInteractor>();
            InteractorContainer interactorContainerStub = new();
            interactorContainerStub.RegisterInteractor(interactorID.ToString(), interactorStub);

            FreeGrabbableService freeGrabbable = new( 
                new List<IHandheldInteractionModule>() {},
                new FreeGrabbableConfig(),
                new FreeGrabbableState(), 
                "debug",
                Substitute.For<IWorldStateSyncService>(),
                interactorContainerStub,
                Substitute.For<IRigidbodyWrapper>(), 
                new PhysicsConstants());

            //Stub out the VC (integration layer) with the grabbable
            V_FreeGrabbableProviderStub v_freeGrabbableStub = new(freeGrabbable);

            //Get interfaces
            IV_FreeGrabbable grabbablePluginInterface = v_freeGrabbableStub;
            IRangedGrabInteractionModuleProvider grabbableRaycastInterface = v_freeGrabbableStub;
            IRangedGrabInteractionModule grabbablePlayerInterface = grabbableRaycastInterface.RangedGrabInteractionModule;

            //Wire up the customer script to receive the events
            PluginGrabbableScript pluginScript = Substitute.For<PluginGrabbableScript>();
            grabbablePluginInterface.OnGrab.AddListener(pluginScript.HandleGrabReceived);
            grabbablePluginInterface.OnDrop.AddListener(pluginScript.HandleDropReceived);

            //Invoke grab, check customer received the grab, and that the interactorID is set
            grabbablePlayerInterface.RequestLocalGrab(interactorID);
            pluginScript.Received(1).HandleGrabReceived();
            Assert.IsTrue(grabbablePluginInterface.IsGrabbed);
            Assert.AreEqual(grabbablePluginInterface.MostRecentInteractingClientID, localClientID);

            //Invoke drop, Check customer received the drop, and that the interactorID is set
            grabbablePlayerInterface.RequestLocalDrop(interactorID);
            pluginScript.Received(1).HandleDropReceived();
            Assert.IsFalse(grabbablePluginInterface.IsGrabbed);
            Assert.AreEqual(grabbablePluginInterface.MostRecentInteractingClientID, localClientID);
        }
    }

    internal class PluginGrabbableScript
    {
        public virtual void HandleGrabReceived() { }

        public virtual void HandleDropReceived() { }
    }

    internal class V_FreeGrabbableProviderStub : IV_FreeGrabbable, IRangedGrabInteractionModuleProvider
    {
        #region Plugin Interfaces     
        IFreeGrabbableStateModule IV_FreeGrabbable._StateModule => _service.StateModule;
        IRangedGrabInteractionModule IV_FreeGrabbable._RangedGrabModule => _service.RangedGrabInteractionModule;
        #endregion

        #region Player Interfaces
        IRangedInteractionModule IRangedInteractionModuleProvider.RangedInteractionModule => _service.RangedGrabInteractionModule;
        #endregion

        private readonly FreeGrabbableService _service = null;

        public V_FreeGrabbableProviderStub(FreeGrabbableService service)
        {
            _service = service;
        }

        public void TearDown()
        {
            _service.TearDown();
        }
    }
}
