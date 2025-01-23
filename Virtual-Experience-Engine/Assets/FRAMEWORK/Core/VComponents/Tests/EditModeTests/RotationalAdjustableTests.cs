using UnityEngine;
using NSubstitute;
using NUnit.Framework;
using VE2.Core.VComponents.PluginInterfaces;
using VE2.Core.VComponents.NonInteractableInterfaces;
using VE2.Core.VComponents.InteractableInterfaces;
using VE2.Core.VComponents.Internal;
using VE2.Common.TransformWrapper;
using System.Collections.Generic;
using VE2.Common;
using VE2.Core.VComponents.InteractableFindables;

namespace VE2.Core.VComponents.Tests
{
    [TestFixture]
    [Category("Rotational Adjustable Tests")]
    public class RotationalAdjustableTests
    {
        private IV_RotationalAdjustable _rotationAdjustmentPluginInterface;
        private V_RotationalAdjustableStub _v_rotationalAdjustableStub;

        private PluginAdjustableMock _customerScript;

        [OneTimeSetUp]
        public void SetUpOnce()
        {
            //Wire up the customer script to receive the events           
            _customerScript = Substitute.For<PluginAdjustableMock>();
        }

        [SetUp]
        public void SetUpBeforeEveryTest()
        {
            //create the handheld adjustable
            RotationalAdjustableService rotationalAdjustable = RotationalAdjustableServiceFactory.Create();
            _v_rotationalAdjustableStub = new(rotationalAdjustable);

            //hook up interfaces
            _rotationAdjustmentPluginInterface = _v_rotationalAdjustableStub;

            //wire up the customer script to receive the events
            _rotationAdjustmentPluginInterface.OnValueAdjusted.AddListener(_customerScript.HandleValueAdjusted);
        }

        [Test]
        public void LinearAdjustable_WhenAdjustedByPlugin_EmitsToPlugin([Random(0f, 1f, 1)] float randomValue)
        {
            //set the adjustable value, Check customer received the value adjusted, and that the interactorID is set
            _rotationAdjustmentPluginInterface.SpatialValue = randomValue;
            _customerScript.Received(1).HandleValueAdjusted(randomValue);
            Assert.IsTrue(_rotationAdjustmentPluginInterface.OutputValue == randomValue);
            Assert.AreEqual(_rotationAdjustmentPluginInterface.MostRecentInteractingClientID, ushort.MaxValue);
        }

        [TearDown]
        public void TearDownAfterEveryTest()
        {
            _customerScript.ClearReceivedCalls();
            _rotationAdjustmentPluginInterface.OnValueAdjusted.RemoveAllListeners();

            _v_rotationalAdjustableStub.TearDown();
            _rotationAdjustmentPluginInterface = null;
        }

        [OneTimeTearDown]
        public void TearDownOnce() { }
    }

    public class V_RotationalAdjustableStub : IV_RotationalAdjustable, IRangedGrabPlayerInteractableIntegrator
    {
        #region Plugin Interfaces
        IAdjustableStateModule IV_RotationalAdjustable._AdjustableStateModule => _rotationalAdjustable.AdjustableStateModule;
        IFreeGrabbableStateModule IV_RotationalAdjustable._FreeGrabbableStateModule => _rotationalAdjustable.FreeGrabbableStateModule;
        IRangedAdjustableInteractionModule IV_RotationalAdjustable._RangedAdjustableModule => _rotationalAdjustable.RangedAdjustableInteractionModule;
        #endregion

        #region Player Interfaces
        IRangedInteractionModule IRangedPlayerInteractableIntegrator.RangedInteractionModule => _rotationalAdjustable.RangedAdjustableInteractionModule;
        #endregion

        public float MinimumSpatialValue { get => _rotationalAdjustable.MinimumSpatialValue; set => _rotationalAdjustable.MinimumSpatialValue = value; }
        public float MaximumSpatialValue { get => _rotationalAdjustable.MaximumSpatialValue; set => _rotationalAdjustable.MaximumSpatialValue = value; }
        public float SpatialValue { get => _rotationalAdjustable.SpatialValue; set => _rotationalAdjustable.SpatialValue = value; }
        public int NumberOfValues { get => _rotationalAdjustable.NumberOfValues; set => _rotationalAdjustable.NumberOfValues = value; }

        protected RotationalAdjustableService _rotationalAdjustable = null;

        public V_RotationalAdjustableStub(RotationalAdjustableService rotationalAdjustable)
        {
            _rotationalAdjustable = rotationalAdjustable;
        }

        public void TearDown()
        {
            _rotationalAdjustable.TearDown();
            _rotationalAdjustable = null;
        }
    }

    public class RotationalAdjustableServiceFactory
    {
        public static RotationalAdjustableService Create(
            ITransformWrapper transformWrapper = null,
            List<IHandheldInteractionModule> handheldInteractions = null,
            RotationalAdjustableConfig config = null,
            AdjustableState adjustableState = null,
            FreeGrabbableState grabbableState = null,
            string id = "debug",
            WorldStateModulesContainer worldStateModulesContainer = null,
            InteractorContainer interactorContainer = null
        )
        {
            transformWrapper ??= Substitute.For<ITransformWrapper>();
            handheldInteractions ??= new List<IHandheldInteractionModule>();
            config ??= new RotationalAdjustableConfig();
            adjustableState ??= new AdjustableState();
            grabbableState ??= new FreeGrabbableState();
            worldStateModulesContainer ??= new WorldStateModulesContainer();
            interactorContainer ??= new InteractorContainer();

            RotationalAdjustableService rotationalAdjustable = new(transformWrapper, handheldInteractions, config, adjustableState, grabbableState, id, worldStateModulesContainer, interactorContainer);

            return rotationalAdjustable;
        }
    }
}
