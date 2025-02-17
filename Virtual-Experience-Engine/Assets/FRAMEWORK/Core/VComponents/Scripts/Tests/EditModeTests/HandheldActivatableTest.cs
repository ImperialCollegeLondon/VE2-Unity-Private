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
    public class HandheldActivatableTest
    {
        [Test]
        public void HandheldActivatable_WhenActivatedByPlugin_EmitsToPlugin()
        {
            //Create the activatable with default values
            HandheldActivatableService handheldActivatable = new(new HandheldActivatableConfig(), new SingleInteractorActivatableState(), "debug", Substitute.For<IWorldStateSyncService>());

            //Stub out the VC (integration layer) with the activatable
            V_HandheldActivatableStub v_activatableStub = new(handheldActivatable);

            //Get interfaces
            IV_HandheldActivatable activatablePluginInterface = v_activatableStub;

            //Wire up the customer script to receive the events
            PluginScriptMock customerScript = Substitute.For<PluginScriptMock>();
            activatablePluginInterface.OnActivate.AddListener(customerScript.HandleActivateReceived);
            activatablePluginInterface.OnDeactivate.AddListener(customerScript.HandleDeactivateReceived);

            //Invoke click, Check customer received the activation, and that the interactorID is set
            activatablePluginInterface.IsActivated = true;
            customerScript.Received(1).HandleActivateReceived();
            Assert.IsTrue(activatablePluginInterface.IsActivated);
            Assert.AreEqual(activatablePluginInterface.MostRecentInteractingClientID, ushort.MaxValue);

            // Invoke the click to deactivate
            activatablePluginInterface.IsActivated = false;
            customerScript.Received(1).HandleDeactivateReceived();
            Assert.IsFalse(activatablePluginInterface.IsActivated);
            Assert.AreEqual(activatablePluginInterface.MostRecentInteractingClientID, ushort.MaxValue);
        }
    }

    public class V_HandheldActivatableStub : IV_HandheldActivatable
    {
        #region Plugin Interfaces
        ISingleInteractorActivatableStateModule IV_HandheldActivatable._StateModule => _HandheldActivatable.StateModule;
        IHandheldClickInteractionModule IV_HandheldActivatable._HandheldClickModule => _HandheldActivatable.HandheldClickInteractionModule;
        #endregion

        protected HandheldActivatableService _HandheldActivatable = null;

        public V_HandheldActivatableStub(HandheldActivatableService HandheldActivatable)
        {
            _HandheldActivatable = HandheldActivatable;
        }
    }
}


