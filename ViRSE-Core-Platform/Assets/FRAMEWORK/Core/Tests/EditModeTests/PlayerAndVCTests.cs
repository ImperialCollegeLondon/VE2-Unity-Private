using NSubstitute;
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using ViRSE;
using ViRSE.Core.Shared;
using ViRSE.Core;
using ViRSE.Core.VComponents;
using ViRSE.Core.Player;
using static ViRSE.Core.Shared.CoreCommonSerializables;
using System;


namespace ViRSE.Tests
{
    public class PushActivatableTests
    {
        //TODO - figure out interactor ID 
        //Also figure out general refactoring, do we really need all these ugly test doubles?
        [Test]
        public void OnUserClick_WithHoveringActivatable_CustomerScriptReceivesOnActivate()
        {
            //Create an activatable with default config and state
            PushActivatable pushActivatable = new( 
                new PushActivatableConfig(),
                new SingleInteractorActivatableState(),
                "testID",
                Substitute.For<WorldStateModulesContainer>()
            );

            //Wire up mock customer script
            IPushActivatable pushActivatableInterface = pushActivatable;
            CustomerScript customerScript = Substitute.For<CustomerScript>();
            pushActivatableInterface.OnActivate.AddListener(customerScript.HandleActivateReceived);
            pushActivatableInterface.OnDeactivate.AddListener(customerScript.HandleDeactivateReceived);
    
            //Stub out the VC's integration layer
            RangedPlayerInteractableStub rangedInteractableStub = new(pushActivatable);

            //Stub out the raycast provider
            RaycastResultWrapperStub raycastResultStub = new(rangedInteractableStub);
            RaycastProviderStub raycastProvider = new(raycastResultStub);

            //Stub out the input handler    
            InputHandler2DStub inputHandler2DStub = new();
            InputHandlerStub inputHandlerStub = new(inputHandler2DStub);

            //Create the player (2d)
            ViRSEPlayerService playerService = new(
                new PlayerTransformData(),
                new PlayerStateConfig(),
                false,
                true,
                Substitute.For<ViRSEPlayerStateModuleContainer>(),
                new PlayerSettingsProviderStub(),
                Substitute.For<IPlayerAppearanceOverridesProvider>()
            );

            //Check customer received the activation, and that the interactorID is set
            inputHandler2DStub.SimulateMouseClick();
            customerScript.Received(1).HandleActivateReceived();
            //Assert.AreEqual(interactorID, stateInterface.CurrentInteractor);

            // Invoke the click to deactivate
            inputHandler2DStub.SimulateMouseClick();
            customerScript.Received(1).HandleDeactivateReceived();
            //Assert.IsNull(stateInterface.CurrentInteractor);
        }

        public class CustomerScript
        {
            public void HandleActivateReceived() { }

            public void HandleDeactivateReceived() { }
        }

        public class RaycastResultWrapperStub : IRaycastResultWrapper
        {
            public Collider Collider => null;
            public GameObject GameObject => null;
            public IRangedPlayerInteractableIntegrator RangedInteractableHit => _interactableInteracterStub;
            public Vector3 Point => Vector3.zero;

            private RangedPlayerInteractableStub _interactableInteracterStub;

            public RaycastResultWrapperStub(RangedPlayerInteractableStub interactableInteracterStub)
            {
                _interactableInteracterStub = interactableInteracterStub;
            }
        }

        public class RaycastProviderStub : RaycastProvider
        {
            private readonly IRaycastResultWrapper _raycastHitWrapperStub;

            public RaycastProviderStub(IRaycastResultWrapper raycastHitWrapper)
            {
                _raycastHitWrapperStub = raycastHitWrapper;
                _instance = this;
            }

            public override bool Raycast(Vector3 rayOrigin, Vector3 raycastDirection, out IRaycastResultWrapper hit, float maxRaycastDistance, LayerMask layerMask)
            {
                hit = _raycastHitWrapperStub;
                return true;
            }
        }

        public class RangedPlayerInteractableStub : IRangedPlayerInteractableIntegrator
        {
            IRangedPlayerInteractableImplementor IRangedPlayerInteractableIntegrator.RangedPlayerInteractableImplementor => _interactableInteracterStub;
            IGeneralPlayerInteractableImplementor IGeneralPlayerInteractableIntegrator.GeneralPlayerInteractableImplementor => _interactableInteracterStub;
            private IRangedPlayerInteractableImplementor _interactableInteracterStub;

            public RangedPlayerInteractableStub(IRangedPlayerInteractableImplementor interactableInteracterStub)
            {
                _interactableInteracterStub = interactableInteracterStub;
            }
        }

        public class InputHandlerStub : V_InputHandler
        {
            private readonly InputHandler2D _inputHandler2DStub;
            public override InputHandler2D InputHandler2D => _inputHandler2DStub;

            public InputHandlerStub(InputHandler2D InputHandler2DStub)
            {
                _inputHandler2DStub = InputHandler2DStub;
                _instance = this;
            }   
        }

        public class InputHandler2DStub : InputHandler2D
        {
            public void SimulateMouseClick() => InvokeOnMouseLeftClick();
        }

        public class PlayerSettingsProviderStub : IPlayerSettingsProvider
        {
            public bool ArePlayerSettingsReady => true;
            public UserSettingsPersistable UserSettings => new();
            public string GameObjectName => "test";
            public bool IsEnabled => true;
            public event Action OnPlayerSettingsReady;
            public event Action OnLocalChangeToPlayerSettings;

            public void NotifyProviderOfChangeToUserSettings() { }
        }
    }
}
