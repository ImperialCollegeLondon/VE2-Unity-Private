using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using VE2.Core.VComponents.API;
using VE2.Core.VComponents.Internal;

namespace VE2.Core.VComponents.Tests
{
    [TestFixture]
    [Category("Handheld Adjustable Tests")]
    internal class HandheldAdjustableTests
    {
        //handheld adjustable
        private IV_HandheldAdjustable _handheldAdjustablePluginInterface => _v_handheldAdjustableProviderStub;
        private V_HandheldAdjustableProviderStub _v_handheldAdjustableProviderStub;

        private PluginAdjustableScript _customerScript;


        [SetUp]
        public void SetUpBeforeEveryTest()
        {
            //create the handheld adjustable
            HandheldAdjustableService handheldAdjustable = new(
                new HandheldAdjustableConfig(), 
                new AdjustableState(), 
                "debug", 
                Substitute.For<IWorldStateSyncService>());

            _v_handheldAdjustableProviderStub = new(handheldAdjustable);

            _customerScript = Substitute.For<PluginAdjustableScript>();
            _handheldAdjustablePluginInterface.OnValueAdjusted.AddListener(_customerScript.HandleValueAdjusted);
        }

        [Test]
        public void HandheldAdjustable_WhenAdjustedByPlugin_EmitsToPlugin([Random(0f, 1f, 1)] float randomValue)
        {
            //set the adjustable value
            _handheldAdjustablePluginInterface.Value = randomValue;

            //Check customer received the value adjusted, and that the interactorID is set
            _customerScript.Received(1).HandleValueAdjusted(randomValue);
            Assert.IsTrue(_handheldAdjustablePluginInterface.Value == randomValue);
            Assert.AreEqual(_handheldAdjustablePluginInterface.MostRecentInteractingClientID, ushort.MaxValue);
        }
    }

    internal class PluginAdjustableScript
    {
        public virtual void HandleValueAdjusted(float value) { }
    }

    internal class V_HandheldAdjustableProviderStub : IV_HandheldAdjustable
    {
        #region Plugin Interfaces
        IAdjustableStateModule IV_HandheldAdjustable._StateModule => _HandheldAdjustable.StateModule;
        IHandheldScrollInteractionModule IV_HandheldAdjustable._HandheldScrollModule => _HandheldAdjustable.HandheldScrollInteractionModule;
        #endregion

        internal IHandheldScrollInteractionModule HandheldScrollInteractionModule => _HandheldAdjustable.HandheldScrollInteractionModule;
        protected HandheldAdjustableService _HandheldAdjustable = null;

        public V_HandheldAdjustableProviderStub (HandheldAdjustableService HandheldAdjustable)
        {
            _HandheldAdjustable = HandheldAdjustable;
        }

        public void TearDown()
        {
            _HandheldAdjustable.TearDown();
            _HandheldAdjustable = null;
        }
    }
}

