using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class RangedAdjustable2DInteractionConfig : RangedGrabInteractionConfig
    {
        [BeginGroup(Style = GroupStyle.Round)]
        [Title("Ranged Adjustable Interaction Settings", ApplyCondition = true)]
        [EndGroup]
        [SerializeField, PropertyOrder(-100)] private Transform _transformToAdjust = null;
        private ITransformWrapper _transformToAdjustWrapper;
        public ITransformWrapper TransformToAdjust
        {
            get
            {
                if (_transformToAdjustWrapper == null && _transformToAdjust != null)
                    _transformToAdjustWrapper = new TransformWrapper(_transformToAdjust);

                return _transformToAdjustWrapper;
            }
            set => _transformToAdjustWrapper = value; //TODO: Maybe try and also set _transformToAdjust if its castable to Transform?
        }
    }

    internal class RangedAdjustable2DInteractionModule : RangedGrabInteractionModule, IRangedAdjustable2DInteractionModule
    {
        public event Action<ushort> OnScroll;

        //This one is ready by the interactor to handle haptics
        public event Action OnValueChanged;

        //TODO - parent class exposes this, likely don't need this here
        public ITransformWrapper Transform { get; }

        public RangedAdjustable2DInteractionModule(string id, IGrabInteractablesContainer grabInteractablesContainer,
            List<IHandheldInteractionModule> handheldModules, RangedGrabInteractionConfig rangedGrabInteractionConfig, GeneralInteractionConfig generalInteractionConfig)
                : base(id, grabInteractablesContainer, handheldModules, rangedGrabInteractionConfig, generalInteractionConfig)
        {
            Transform = rangedGrabInteractionConfig.AttachPointWrapper;
        }

        public void Scroll(ushort clientID) => OnScroll?.Invoke(clientID);

        public void NotifyValueChanged() => OnValueChanged?.Invoke();

        public void ScrollUp(ushort clientID)
        {
            //DO NOTHING AS THIS IS A 2D INTERACTION MODULE
        }

        public void ScrollDown(ushort clientID)
        {
            //DO NOTHING AS THIS IS A 2D INTERACTION MODULE
        }
    }
}