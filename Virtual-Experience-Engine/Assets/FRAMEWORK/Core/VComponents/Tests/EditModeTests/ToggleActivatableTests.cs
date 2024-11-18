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
    public class PushActivatableTests
    {
        [Test]
        public void PushActivatable_WhenClicked_EmitsToPlugin()
        {
            //Create the activatable with default values
            ToggleActivatableService toggleActivatable = new (new ToggleActivatableConfig(), new SingleInteractorActivatableState(), "debug", Substitute.For<WorldStateModulesContainer>());

            //Stub out the VC (integration layer) with the activatable
            V_ToggleActivatableStub v_activatableStub = new(toggleActivatable);

            //Get interfaces
            IV_ToggleActivatable activatablePluginInterface = v_activatableStub;
            IRangedClickPlayerInteractableIntegrator activatableRaycastInterface = v_activatableStub;
            IRangedClickInteractionModule activatablePlayerInterface = activatableRaycastInterface.RangedClickInteractionModule;

            //Wire up the customer script to receive the events
            PluginScriptMock customerScript = Substitute.For<PluginScriptMock>();
            activatablePluginInterface.OnActivate.AddListener(customerScript.HandleActivateReceived);
            activatablePluginInterface.OnDeactivate.AddListener(customerScript.HandleDeactivateReceived);

            //Create an ID
            System.Random random = new();
            ushort localClientID = (ushort)random.Next(0, ushort.MaxValue);

            //Invoke click, Check customer received the activation, and that the interactorID is set
            activatablePlayerInterface.Click(localClientID);
            customerScript.Received(1).HandleActivateReceived();
            Assert.IsTrue(activatablePluginInterface.IsActivated);
            Assert.AreEqual(activatablePluginInterface.MostRecentInteractingClientID, localClientID);

            // Invoke the click to deactivate
            activatablePlayerInterface.Click(localClientID);
            customerScript.Received(1).HandleDeactivateReceived();
            Assert.IsFalse(activatablePluginInterface.IsActivated);
            Assert.AreEqual(activatablePluginInterface.MostRecentInteractingClientID, localClientID);
        }
    }

    public class PluginScriptMock
    {
        public virtual void HandleActivateReceived() { }
        public virtual void HandleDeactivateReceived() { }
        public virtual void HandleValueAdjusted(float value) { }
    }

    public class V_ToggleActivatableStub : IV_ToggleActivatable, IRangedClickPlayerInteractableIntegrator, ICollidePlayerInteractableIntegrator
    {
        #region Plugin Interfaces
        ISingleInteractorActivatableStateModule IV_ToggleActivatable._StateModule => _ToggleActivatable.StateModule;
        IRangedClickInteractionModule IV_ToggleActivatable._RangedClickModule => _ToggleActivatable.RangedClickInteractionModule;
        #endregion

        #region Player Interfaces
        ICollideInteractionModule ICollidePlayerInteractableIntegrator._CollideInteractionModule => _ToggleActivatable.ColliderInteractionModule;
        IRangedInteractionModule IRangedPlayerInteractableIntegrator.RangedInteractionModule => _ToggleActivatable.RangedClickInteractionModule;
        #endregion

        protected ToggleActivatableService _ToggleActivatable = null;

        public V_ToggleActivatableStub(ToggleActivatableService ToggleActivatable)
        {
            _ToggleActivatable = ToggleActivatable;
        }
    }
}
