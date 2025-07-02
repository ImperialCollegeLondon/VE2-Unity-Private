using UnityEngine;
using NSubstitute;
using NUnit.Framework;
using VE2.Core.VComponents.Internal;
using VE2.Common.Shared;
using System.Collections.Generic;
using VE2.Core.VComponents.API;
using VE2.Common.API;
using UnityEngine.Events;

namespace VE2.Core.VComponents.Tests
{
    [TestFixture]
    [Category("Rotating Adjustable Tests")]
    internal class RotatingAdjustableTests
    {
        private IV_RotatingAdjustable _rotatingAdjustmentPluginInterface => _v_rotatingAdjustableProviderStub;
        private V_RotatingAdjustableProviderStub _v_rotatingAdjustableProviderStub;

        private PluginAdjustableScript _customerScript;

        [SetUp]
        public void SetUpBeforeEveryTest()
        {
            //create the handheld adjustable
            RotatingAdjustableService rotatingAdjustable = new(
                new List<IHandheldInteractionModule>(),
                new RotatingAdjustableConfig(Substitute.For<ITransformWrapper>(), Substitute.For<ITransformWrapper>()),
                new AdjustableState(),
                new GrabbableState(),
                "debug",
                Substitute.For<IWorldStateSyncableContainer>(),
                Substitute.For<IGrabInteractablesContainer>(),
                new HandInteractorContainer(),
                Substitute.For<IClientIDWrapper>());
            _v_rotatingAdjustableProviderStub = new(rotatingAdjustable);

            //wire up the customer script to receive the events       
            _customerScript = Substitute.For<PluginAdjustableScript>();
            _rotatingAdjustmentPluginInterface.OnValueAdjusted.AddListener(_customerScript.HandleValueAdjusted);
        }

        [Test]
        public void RotatingAdjustable_WhenAdjustedByPlugin_EmitsToPlugin([Random(0f, 1f, 1)] float randomValue)
        {
            //set the adjustable value, Check customer received the value adjusted, and that the interactorID reflects programmatic activation (ie, null!)
            _rotatingAdjustmentPluginInterface.SpatialValue = randomValue;
            _customerScript.Received(1).HandleValueAdjusted(randomValue);
            Assert.IsTrue(_rotatingAdjustmentPluginInterface.Value == randomValue);
            Assert.AreEqual(_rotatingAdjustmentPluginInterface.MostRecentGrabbingClientID, null);
        }

        [TearDown]
        public void TearDownAfterEveryTest()
        {
            _customerScript.ClearReceivedCalls();
            _rotatingAdjustmentPluginInterface.OnValueAdjusted.RemoveAllListeners();

            _v_rotatingAdjustableProviderStub.TearDown();
        }

        [OneTimeTearDown]
        public void TearDownOnce() { }
    }

    internal partial class V_RotatingAdjustableProviderStub : IV_RotatingAdjustable
    {
        #region State Module Interface
        internal IAdjustableStateModule _AdjustableStateModule => _Service.AdjustableStateModule;
        internal IGrabbableStateModule _GrabbableStateModule => _Service.GrabbableStateModule;

        public UnityEvent<float> OnValueAdjusted => _AdjustableStateModule.OnValueAdjusted;
        public UnityEvent OnGrab => _GrabbableStateModule.OnGrab;
        public UnityEvent OnDrop => _GrabbableStateModule.OnDrop;

        public bool IsGrabbed => _GrabbableStateModule.IsGrabbed;
        public bool IsLocallyGrabbed => _GrabbableStateModule.IsLocalGrabbed;
        public float Value => _AdjustableStateModule.OutputValue;
        public void SetValue(float value) => _AdjustableStateModule.SetOutputValue(value);
        public float MinimumOutputValue { get => _AdjustableStateModule.MinimumOutputValue; set => _AdjustableStateModule.MinimumOutputValue = value; }
        public float MaximumOutputValue { get => _AdjustableStateModule.MaximumOutputValue; set => _AdjustableStateModule.MaximumOutputValue = value; }

        public float MinimumSpatialValue { get => _Service.MinimumSpatialValue; set => _Service.MinimumSpatialValue = value; }
        public float MaximumSpatialValue { get => _Service.MaximumSpatialValue; set => _Service.MaximumSpatialValue = value; }
        public float SpatialValue { get => _Service.SpatialValue; set => _Service.SpatialValue = value; }
        public int NumberOfValues { get => _Service.NumberOfValues; set => _Service.NumberOfValues = value; }

        public void SetMinimumAndMaximumSpatialValuesRange(float min, float max)
        {
            MinimumSpatialValue = min;
            MaximumSpatialValue = max;
        }

        public void SetMinimumAndMaximumOutputValuesRange(float min, float max)
        {
            MinimumOutputValue = min;
            MaximumOutputValue = max;
        }
        
        public IClientIDWrapper MostRecentGrabbingClientID => _GrabbableStateModule.MostRecentInteractingClientID;
        public IClientIDWrapper MostRecentAdjustingClientID => _AdjustableStateModule.MostRecentInteractingClientID;
        #endregion

        #region Ranged Interaction Module Interface
        internal IRangedAdjustableInteractionModule _RangedAdjustableModule => _Service.RangedAdjustableInteractionModule;    
        public float InteractRange { get => _RangedAdjustableModule.InteractRange; set => _RangedAdjustableModule.InteractRange = value; }
        #endregion

        #region General Interaction Module Interface
        //We have two General Interaction Modules here, it doesn't matter which one we point to, both share the same General Interaction Config object!
        public bool AdminOnly {get => _RangedAdjustableModule.AdminOnly; set => _RangedAdjustableModule.AdminOnly = value; }
        public bool EnableControllerVibrations { get => _RangedAdjustableModule.EnableControllerVibrations; set => _RangedAdjustableModule.EnableControllerVibrations = value; }
        public bool ShowTooltipsAndHighlight { get => _RangedAdjustableModule.ShowTooltipsAndHighlight; set => _RangedAdjustableModule.ShowTooltipsAndHighlight = value; }
        public bool IsInteractable { get => _RangedAdjustableModule.IsInteractable; set => _RangedAdjustableModule.IsInteractable = value; }
        #endregion
    }

    internal partial class V_RotatingAdjustableProviderStub : IRangedGrabInteractionModuleProvider
    {
        #region Player Interfaces
        IRangedInteractionModule IRangedInteractionModuleProvider.RangedInteractionModule => _Service.RangedAdjustableInteractionModule;
        #endregion

        protected RotatingAdjustableService _Service = null;

        public V_RotatingAdjustableProviderStub(RotatingAdjustableService service)
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
