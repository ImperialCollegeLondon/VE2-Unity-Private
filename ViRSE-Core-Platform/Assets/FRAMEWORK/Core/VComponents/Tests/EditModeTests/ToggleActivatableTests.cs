using NSubstitute;
using NUnit.Framework;
using ViRSE.Core.VComponents.PluginInterfaces;
using ViRSE.Core.VComponents.PlayerInterfaces;
using ViRSE.Core.VComponents.InternalInterfaces;
using UnityEngine;
using ViRSE.Common;

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
            IRangedClickPlayerInteractable activatablePlayerInterface = v_activatableStub;

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

    internal class V_ToggleActivatableStub : MonoBehaviour, IV_ToggleActivatable, IRangedClickPlayerInteractable, ICollidePlayerInteractable
    {
        #region Plugin Interfaces
        ISingleInteractorActivatableStateModuleImplementor ISingleInteractorStateModulePluginInterface._StateModuleImplementor => ToggleActivatable;
        IRangedInteractionModuleImplementor IRangedInteractablePluginInterface._RangedModuleImplementor => ToggleActivatable;
        IGeneralInteractionModuleImplementor IGeneralInteractionPluginInterface._GeneralModuleImplementor => ToggleActivatable;
        #endregion

        #region Player Interfaces
        IRangedInteractionModuleImplementor IRangedPlayerInteractable.RangedModuleImplementor => ToggleActivatable;
        IGeneralInteractionModuleImplementor IGeneralPlayerInteractable._GeneralModuleImplementor => ToggleActivatable;
        ICollideInteractionModuleImplementor ICollidePlayerInteractable._CollideModuleImplementor => ToggleActivatable;
        #endregion

        public ToggleActivatable ToggleActivatable = null;
    }
}
