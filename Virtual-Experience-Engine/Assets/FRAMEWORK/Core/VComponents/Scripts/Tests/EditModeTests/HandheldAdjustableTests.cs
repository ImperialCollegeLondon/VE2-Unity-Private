using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using VE2.Common.Shared;
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
                Substitute.For<IWorldStateSyncableContainer>(),
                Substitute.For<IClientIDWrapper>());

            _v_handheldAdjustableProviderStub = new(handheldAdjustable);

            _customerScript = Substitute.For<PluginAdjustableScript>();
            _handheldAdjustablePluginInterface.OnValueAdjusted.AddListener(_customerScript.HandleValueAdjusted);
        }

        [Test]
        public void HandheldAdjustable_WhenAdjustedByPlugin_EmitsToPlugin([Random(0f, 1f, 1)] float randomValue)
        {
            //set the adjustable value
            _handheldAdjustablePluginInterface.SetValue(randomValue);

            //Check customer received the value adjusted, and that the interactorID reflects programmatic activation (ie, null!)
            _customerScript.Received(1).HandleValueAdjusted(randomValue);
            Assert.IsTrue(_handheldAdjustablePluginInterface.Value == randomValue);
            Assert.AreEqual(_handheldAdjustablePluginInterface.MostRecentInteractingClientID, null);
        }
    }

    internal class PluginAdjustableScript
    {
        public virtual void HandleValueAdjusted(float value) { }
    }

    internal partial class V_HandheldAdjustableProviderStub : IV_HandheldAdjustable
    {
        #region State Module Interface
        internal IAdjustableStateModule _StateModule => _Service.StateModule;

        public UnityEvent<float> OnValueAdjusted => _StateModule.OnValueAdjusted;
        public float Value => _StateModule.OutputValue;
        public void SetValue(float value) => _StateModule.SetOutputValue(value);
        public float MinimumValue { get => _StateModule.MinimumOutputValue; set => _StateModule.MinimumOutputValue = value; }
        public float MaximumValue { get => _StateModule.MaximumOutputValue; set => _StateModule.MaximumOutputValue = value; }
        public IClientIDWrapper MostRecentInteractingClientID => _StateModule.MostRecentInteractingClientID;
        #endregion

        #region Handheld Interaction Module Interface
        internal IHandheldScrollInteractionModule _HandheldScrollModule => _Service.HandheldScrollInteractionModule;
        #endregion

        #region General Interaction Module Interface
        public bool AdminOnly { get => _HandheldScrollModule.AdminOnly; set => _HandheldScrollModule.AdminOnly = value; }
        public bool EnableControllerVibrations { get => _HandheldScrollModule.EnableControllerVibrations; set => _HandheldScrollModule.EnableControllerVibrations = value; }
        public bool ShowTooltipsAndHighlight { get => _HandheldScrollModule.ShowTooltipsAndHighlight; set => _HandheldScrollModule.ShowTooltipsAndHighlight = value; }
        #endregion
    }

    internal partial class V_HandheldAdjustableProviderStub
    {
        internal IHandheldScrollInteractionModule HandheldScrollInteractionModule => _Service.HandheldScrollInteractionModule;
        protected HandheldAdjustableService _Service = null;

        public V_HandheldAdjustableProviderStub(HandheldAdjustableService service)
        {
            _Service = service;
        }

        public void TearDown()
        {
            _Service.TearDown();
            _Service = null;
        }
    }
}

