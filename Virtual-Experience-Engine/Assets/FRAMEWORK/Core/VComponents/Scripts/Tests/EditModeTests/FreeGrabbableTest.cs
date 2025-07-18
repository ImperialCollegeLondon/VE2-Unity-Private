using System.Collections.Generic;
using NSubstitute;
using NUnit.Framework;
using UnityEngine.Events;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Internal;

namespace VE2.Core.VComponents.Tests
{
    internal class FreeGrabbableTest
    {
        //TODO: Doesn't belong here? Looks like an integration test moreso than a unit/service test
        [Test]
        public void FreeGrabbable_WhenGrabbed_EmitsToPlugin()
        {
            //Create an ID
            System.Random random = new();
            ushort localClientID = (ushort)random.Next(0, ushort.MaxValue);
            InteractorID interactorID = new(localClientID, InteractorType.Mouse2D);

            IInteractor interactorStub = Substitute.For<IInteractor>();
            interactorStub.GrabberTransformWrapper.Returns(Substitute.For<ITransformWrapper>());
            HandInteractorContainer interactorContainerStub = new();
            interactorContainerStub.RegisterInteractor(interactorID.ToString(), interactorStub);

            FreeGrabbableService freeGrabbable = new( 
                new List<IHandheldInteractionModule>() {},
                new FreeGrabbableConfig(Substitute.For<ITransformWrapper>()),
                new GrabbableState(), 
                "debug",
                Substitute.For<IWorldStateSyncableContainer>(),
                Substitute.For<IGrabInteractablesContainer>(),
                interactorContainerStub,
                Substitute.For<IRigidbodyWrapper>(), 
                new PhysicsConstants(),
                new V_FreeGrabbable(),
                Substitute.For<IClientIDWrapper>(),
                Substitute.For<IColliderWrapper>());

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
            Assert.AreEqual(grabbablePluginInterface.MostRecentInteractingClientID.Value, localClientID);

            //Invoke drop, Check customer received the drop, and that the interactorID is set
            grabbablePlayerInterface.RequestLocalDrop(interactorID);
            pluginScript.Received(1).HandleDropReceived();
            Assert.IsFalse(grabbablePluginInterface.IsGrabbed);
            Assert.AreEqual(grabbablePluginInterface.MostRecentInteractingClientID.Value, localClientID);
        }
    }

    internal class PluginGrabbableScript
    {
        public virtual void HandleGrabReceived() { }

        public virtual void HandleDropReceived() { }
    }

    internal partial class V_FreeGrabbableProviderStub : IV_FreeGrabbable
    {
        #region State Module Interface
        internal IGrabbableStateModule _StateModule => Service.StateModule;

        public UnityEvent OnGrab => _StateModule.OnGrab;
        public UnityEvent OnDrop => _StateModule.OnDrop;

        public bool IsGrabbed { get { return _StateModule.IsGrabbed; } }
        public IClientIDWrapper MostRecentInteractingClientID => _StateModule.MostRecentInteractingClientID;

        public bool TryLocalGrab(bool lockGrab, VRHandInteractorType priorityHandToGrabWith) => _StateModule.TryLocalGrab(lockGrab, priorityHandToGrabWith);

        public void ForceLocalGrab(bool lockGrab, VRHandInteractorType handToGrabWith) => _StateModule.ForceLocalGrab(lockGrab, handToGrabWith);

        public void UnlockLocalGrab() => _StateModule.UnlockLocalGrab();

        public void ForceLocalDrop() => _StateModule.ForceLocalDrop();
        #endregion

        #region Ranged Interaction Module Interface
        internal IRangedGrabInteractionModule _RangedGrabModule => Service.RangedGrabInteractionModule;
        public float InteractRange { get => _RangedGrabModule.InteractRange; set => _RangedGrabModule.InteractRange = value; }
        #endregion

        #region General Interaction Module Interface
        //We have two General Interaction Modules here, it doesn't matter which one we point to, both share the same General Interaction Config object!
        public bool AdminOnly {get => _RangedGrabModule.AdminOnly; set => _RangedGrabModule.AdminOnly = value; }
        public bool EnableControllerVibrations { get => _RangedGrabModule.EnableControllerVibrations; set => _RangedGrabModule.EnableControllerVibrations = value; }
        public bool ShowTooltipsAndHighlight { get => _RangedGrabModule.ShowTooltipsAndHighlight; set => _RangedGrabModule.ShowTooltipsAndHighlight = value; }
        public bool IsInteractable { get => _RangedGrabModule.IsInteractable; set => _RangedGrabModule.IsInteractable = value; }

        #endregion
    }

    internal partial class V_FreeGrabbableProviderStub : IRangedGrabInteractionModuleProvider
    {
        #region Player Interfaces
        IRangedInteractionModule IRangedInteractionModuleProvider.RangedInteractionModule => Service.RangedGrabInteractionModule;
        #endregion

        public FreeGrabbableService Service { private get; set; }

        public V_FreeGrabbableProviderStub(FreeGrabbableService service)
        {
            this.Service = service;
        }

        public V_FreeGrabbableProviderStub() { }

        public void TearDown()
        {
            Service.TearDown();
        }
    }
}