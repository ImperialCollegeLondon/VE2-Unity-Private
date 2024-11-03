using NSubstitute;
using NUnit.Framework;
using ViRSE.Core.VComponents.PluginInterfaces;
using UnityEngine;
using ViRSE.Common;
using VIRSE.Core.VComponents.InteractableInterfaces;
using ViRSE.Core.VComponents.NonInteractableInterfaces;
using ViRSE.Core.VComponents.RaycastInterfaces;

namespace ViRSE.Core.VComponents.Tests
{
    public class PushActivatableTests
    {
        [Test]
        public void PushActivatable_WhenClicked_EmitsToPlugin()
        {
            //Create the activatable with default values
            ToggleActivatable toggleActivatable = new (new ToggleActivatableConfig(), new SingleInteractorActivatableState(), "debug", Substitute.For<WorldStateModulesContainer>());

            //Stub out the VC (integration layer) with the activatable
            GameObject emptyGO = new();
            V_ToggleActivatableStub v_activatableStub = emptyGO.AddComponent< V_ToggleActivatableStub>();
            v_activatableStub.ToggleActivatable = toggleActivatable;

            //Get interfaces
            IV_ToggleActivatable activatablePluginInterface = v_activatableStub;
            IRangedClickInteractionModule activatablePlayerInterface = ((IRangedClickPlayerInteractableIntegrator)v_activatableStub).RangedClickInteractionModule;

            //Wire up the customer script to receive the events
            CustomerScript customerScript = Substitute.For<CustomerScript>();
            activatablePluginInterface.OnActivate.AddListener(customerScript.HandleActivateReceived);
            activatablePluginInterface.OnDeactivate.AddListener(customerScript.HandleDectivateReceived);

            //Create an ID
            System.Random random = new();
            ushort localClientID = (ushort)random.Next(0, ushort.MaxValue + 1);

            //Invoke click, Check customer received the activation, and that the interactorID is set
            activatablePlayerInterface.Click(localClientID);
            customerScript.Received(1).HandleActivateReceived();
            Assert.IsTrue(activatablePluginInterface.IsActivated);
            Assert.AreEqual(activatablePluginInterface.MostRecentInteractingClientID, localClientID);

            // Invoke the click to deactivate
            activatablePlayerInterface.Click(localClientID);
            customerScript.Received(1).HandleDectivateReceived();
            Assert.IsFalse(activatablePluginInterface.IsActivated);
            Assert.AreEqual(activatablePluginInterface.MostRecentInteractingClientID, localClientID);
        }

        public class CustomerScript
        {
            public void HandleActivateReceived() { }

            public void HandleDectivateReceived() { }
        }
    }

    internal class V_ToggleActivatableStub : MonoBehaviour, IV_ToggleActivatable, IRangedClickPlayerInteractableIntegrator, ICollidePlayerInteractableIntegrator
    {
        #region Plugin Interfaces
        ISingleInteractorActivatableStateModule IV_ToggleActivatable._StateModule => ToggleActivatable.StateModule;
        IRangedClickInteractionModule IV_ToggleActivatable._RangedClickModule => ToggleActivatable.RangedClickInteractionModule;
        #endregion

        #region Player Interfaces
        ICollideInteractionModule ICollidePlayerInteractableIntegrator._CollideInteractionModule => ToggleActivatable.ColliderInteractionModule;
        IRangedInteractionModule IRangedPlayerInteractableIntegrator.RangedInteractionModule => ToggleActivatable.RangedClickInteractionModule;
        #endregion

        public ToggleActivatable ToggleActivatable = null;
    }
}
