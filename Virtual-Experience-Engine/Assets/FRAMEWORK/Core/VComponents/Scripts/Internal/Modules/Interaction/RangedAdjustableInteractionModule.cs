using System;
using System.Collections.Generic;
using UnityEngine;
using VE2.Common.API;
using VE2.Common.Shared;
using VE2.Core.VComponents.API;

namespace VE2.Core.VComponents.Internal
{
    [Serializable]
    internal class RangedAdjustableInteractionConfig : RangedGrabInteractionConfig
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

    internal class RangedAdjustableInteractionModule : RangedGrabInteractionModule, IRangedAdjustableInteractionModule
    {
        public event Action OnScrollUp;
        public event Action OnScrollDown;
        public event Action OnValueChanged;

        //TODO - parent class exposes this, likely don't need this here
        public ITransformWrapper Transform { get; }

        public RangedAdjustableInteractionModule(string id, IGrabInteractablesContainer grabInteractablesContainer,
            List<IHandheldInteractionModule> handheldModules, RangedGrabInteractionConfig rangedGrabInteractionConfig, GeneralInteractionConfig generalInteractionConfig)
                : base(id, grabInteractablesContainer, handheldModules, rangedGrabInteractionConfig, generalInteractionConfig)
        {
            Transform = rangedGrabInteractionConfig.AttachPointWrapper;
        }

        public void ScrollUp() => OnScrollUp?.Invoke();
        public void ScrollDown() => OnScrollDown?.Invoke();

        public void NotifyValueChanged()
        {
            OnValueChanged?.Invoke();
        }
    }
}