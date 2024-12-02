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
    [TestFixture]
    [Category("Handheld Adjustable Tests")]
    public class HandheldAdjustableTests
    {
        private PluginAdjustableMock _customerScript;
        private IV_HandheldAdjustable _handheldAdjustablePluginInterface;
        private V_HandheldAdjustableStub _v_handheldAdjustableStub;

        [OneTimeSetUp]
        public void SetUpOnce()
        {
            //Wire up the customer script to receive the events           
            _customerScript = Substitute.For<PluginAdjustableMock>();
        }

        [SetUp]
        public void SetUpBeforeEveryTest()
        {
            HandheldAdjustableService handheldAdjustable = HandheldAdjustableServiceStubFactory.Create();
            _v_handheldAdjustableStub = new(handheldAdjustable);

            _handheldAdjustablePluginInterface = _v_handheldAdjustableStub;

            _handheldAdjustablePluginInterface.OnValueAdjusted.AddListener(_customerScript.HandleValueAdjusted);
        }

        [Test]
        public void HandheldAdjustable_WhenAdjustedByPlugin_EmitsToPlugin([Random(0f, 1f, 1)] float randomValue)
        {
            _handheldAdjustablePluginInterface.Value = randomValue;
            _customerScript.Received(1).HandleValueAdjusted(randomValue);
            Assert.IsTrue(_handheldAdjustablePluginInterface.Value == randomValue);
            Assert.AreEqual(_handheldAdjustablePluginInterface.MostRecentInteractingClientID, ushort.MaxValue);
        }
    }

    public class PluginAdjustableMock
    {
        public void HandleValueAdjusted(float value) { }
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

        public void TearDown()
        {
            _HandheldAdjustable.TearDown();
            _HandheldAdjustable = null;
        }
    }

    public class HandheldAdjustableServiceStubFactory
    {
        public static HandheldAdjustableService Create(
            HandheldAdjustableConfig config = null,
            AdjustableState state = null,
            string debugName = "debug",
            WorldStateModulesContainer worldStateModulesContainer = null
        )
        {
            config ??= new HandheldAdjustableConfig();
            state ??= new AdjustableState();
            worldStateModulesContainer ??= Substitute.For<WorldStateModulesContainer>();

            HandheldAdjustableService handheldAdjustable = new(config, state, debugName, worldStateModulesContainer);

            return handheldAdjustable;
        }

    }
}

