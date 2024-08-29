using System;
using UnityEngine;
using ViRSE.Core.Shared;

namespace ViRSE.PluginRuntime.VComponents
{
    [System.Serializable]
    public class PushActivatableConfig
    {
        [SpaceArea(spaceAfter: 15), SerializeField, IgnoreParent] public ActivatableStateConfig StateConfig = new();
        [SpaceArea(spaceAfter: 15), SerializeField, IgnoreParent] public GeneralInteractionConfig GeneralInteractionConfig = new();
        [SerializeField, IgnoreParent] public RangedInteractionConfig RangedInteractionConfig = new();

    }

    public abstract class BaseStateHolder : MonoBehaviour
    {
        public abstract BaseStateConfig BaseStateConfig { get; }
    }

    public class V_PushActivatable : BaseStateHolder, IPushActivatable, IRangedClickPlayerInteractableImplementor, ICollidePlayerInteratableImplementor
    {
        [SerializeField, HideLabel, IgnoreParent] private PushActivatableConfig _config = new(); 
        [SerializeField, HideInInspector] private SingleInteractorActivatableState _state = new();

        private PushActivatable _pushActivatable;

        public override BaseStateConfig BaseStateConfig => _config.StateConfig;

        #region Plugin Interfaces
        ISingleInteractorActivatableStateModule ISingleInteractorActivatableStateModuleImplementor._module => _pushActivatable.StateModule;
        IGeneralInteractionModule IGeneralInteractionModuleImplementor._module => _pushActivatable.GeneralInteractionModule;
        IRangedInteractionModule IRangedInteractionModuleImplementor._module => _pushActivatable.RangedClickInteractionModule;
        #endregion

        #region Player Rig Interfaces
        IGeneralPlayerInteractable IGeneralPlayerInteractableImplementor.GeneralPlayerInteractable => _pushActivatable.GeneralInteractionModule;
        IRangedPlayerInteractable IRangedPlayerInteractableImplementor.RangedPlayerInteractable => _pushActivatable.RangedClickInteractionModule;
        IRangedClickPlayerInteractable IRangedClickPlayerInteractableImplementor.RangedClickPlayerInteractable => _pushActivatable.RangedClickInteractionModule;
        ICollidePlayerInteratable ICollidePlayerInteratableImplementor.CollidePlayerInteratable => _pushActivatable.ColliderInteractionModule;
        #endregion

        private void OnValidate()
        {
            if (_config == null)
                _config = new();

            //TODO - want something better than OnValidate here really
            //What if we plonk a syncer in the scene, we don't want to have to get each VC to validate to pick it up
            _config.StateConfig.OnValidate();
        }

        private void Start()
        {
            _pushActivatable = PushActivatableFactory.Create(_config, _state, gameObject.name);
        }
    }

    public static class PushActivatableFactory
    {
        public static PushActivatable Create(PushActivatableConfig config, ViRSESerializable state, string goName)
        {
            SingleInteractorActivatableStateModule stateModule = new(state, config.StateConfig, goName);
            GeneralInteractionModule GeneralInteractionModule = new(config.GeneralInteractionConfig);
            RangedClickInteractionModule RangedClickInteractionModule = new(config.RangedInteractionConfig);
            ColliderInteractionModule ColliderInteractionModule = new();

            return new PushActivatable(stateModule, GeneralInteractionModule, RangedClickInteractionModule, ColliderInteractionModule);
        }
    }

    [Serializable]
    public class PushActivatable
    {
        #region Modules
        public SingleInteractorActivatableStateModule StateModule { get; private set; }
        public GeneralInteractionModule GeneralInteractionModule { get; private set; }
        public RangedClickInteractionModule RangedClickInteractionModule { get; private set; }
        public ColliderInteractionModule ColliderInteractionModule { get; private set; }
        #endregion

        public PushActivatable(
            SingleInteractorActivatableStateModule stateModule,
            GeneralInteractionModule generalInteractionModule,
            RangedClickInteractionModule rangedClickInteractionModule,
            ColliderInteractionModule colliderInteractionModule)
        {
            StateModule = stateModule;
            GeneralInteractionModule = generalInteractionModule;
            RangedClickInteractionModule = rangedClickInteractionModule;
            ColliderInteractionModule = colliderInteractionModule;

            RangedClickInteractionModule.OnClickDown += HandleOnInteract;
            ColliderInteractionModule.OnCollideEnter += HandleOnInteract;
        }

        private void HandleOnInteract(InteractorID interactorID)
        {
            //Could live in interactor, call InvertState on state module, which would emit an event to the network module "OnStateChanged"
            StateModule.InvertState(interactorID);
        }
    }
}
