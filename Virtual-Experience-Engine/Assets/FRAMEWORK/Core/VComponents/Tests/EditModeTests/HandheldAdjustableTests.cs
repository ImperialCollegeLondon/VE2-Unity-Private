using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.XR;
using VE2.Common;
using VE2.Core.VComponents.InteractableInterfaces;
using VE2.Core.VComponents.Internal;
using VE2.Core.VComponents.NonInteractableInterfaces;
using VE2.Core.VComponents.PluginInterfaces;

namespace VE2.Core.VComponents.Tests
{
    public class HandheldAdjustableTests
    {
        [Test]
        public void HandheldAdjustable_WhenAdjustedByPlugin_EmitsToPlugin()
        {
            HandheldAdjustableService handheldAdjustable = new(new HandheldAdjustableConfig(), new AdjustableState(), "debug", Substitute.For<WorldStateModulesContainer>());

            V_HandheldAdjustableStub v_adjustableStub = new(handheldAdjustable);

            IV_HandheldAdjustable adjustablePluginInterface = v_adjustableStub;

            System.Random random = new();
            float randomValue = (float)random.NextDouble();

            PluginActivatableMock customerScript = Substitute.For<PluginActivatableMock>();
            adjustablePluginInterface.OnValueAdjusted.AddListener( (value) => customerScript.HandleValueAdjusted(value) );
            adjustablePluginInterface.Value = randomValue;
            customerScript.Received(1).HandleValueAdjusted(randomValue);
            Assert.IsTrue(adjustablePluginInterface.Value == randomValue);
            Assert.AreEqual(adjustablePluginInterface.MostRecentInteractingClientID, ushort.MaxValue);
        }
    }

    public class V_HandheldAdjustableStub : IV_HandheldAdjustable
    {
        #region Plugin Interfaces
        IAdjustableStateModule IV_HandheldAdjustable._StateModule => _HandheldAdjustable.StateModule;
        IHandheldScrollInteractionModule IV_HandheldAdjustable._HandheldScrollModule => _HandheldAdjustable.HandheldScrollInteractionModule;
        #endregion

        protected HandheldAdjustableService _HandheldAdjustable = null;

        public V_HandheldAdjustableStub (HandheldAdjustableService HandheldAdjustable)
        {
            _HandheldAdjustable = HandheldAdjustable;
        }
    }
}

