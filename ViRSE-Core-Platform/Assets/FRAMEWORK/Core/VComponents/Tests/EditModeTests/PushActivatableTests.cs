using NSubstitute;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using ViRSE;
using ViRSE.Core.Shared;
using ViRSE.PluginRuntime;
using ViRSE.PluginRuntime.VComponents;
using static ViRSE.Core.Shared.CoreCommonSerializables;


namespace ViRSE.Tests
{
    public class PushActivatableTests
    {
        [Test]
        public void PushActivatable_WhenClicked_EmitsToPlugin()
        {
            PushActivatableConfig config = new(); // Config with default values
            ViRSESerializable state = new SingleInteractorActivatableState();

            PushActivatable pushActivatable =
                PushActivatableFactory.Create(config, state, "debug");

            ISingleInteractorActivatableStateModule stateInterface = pushActivatable.StateModule;
            CustomerScript customerScript = Substitute.For<CustomerScript>();   
            stateInterface.OnActivate.AddListener(customerScript.HandleActivateReceived);
            stateInterface.OnDeactivate.AddListener(customerScript.HandleDectivateReceived);

            // Invoke the click to activate
            IRangedClickPlayerInteractable rangedClickInterface = pushActivatable.RangedClickInteractionModule;
            InteractorID interactorID = new InteractorID(0, InteractorType.TwoD);
            rangedClickInterface.InvokeOnClickDown(interactorID);

            //Check customer received the activation, and that the interactorID is set
            customerScript.Received(1).HandleActivateReceived();
            Assert.AreEqual(interactorID, stateInterface.CurrentInteractor);

            // Invoke the click to deactivate
            rangedClickInterface.InvokeOnClickDown(interactorID);
            customerScript.Received(1).HandleDectivateReceived();
            Assert.IsNull(stateInterface.CurrentInteractor);
        }

        public class CustomerScript
        {
            public void HandleActivateReceived() { }

            public void HandleDectivateReceived() { }
        }
    }
}
