using System;
using UnityEngine;
using VE2.Core.Common;

namespace VE2.Common.Shared
{
    [Serializable]
    internal class RepeatedTransmissionConfig : TransmissionConfig
    {
        [Suffix("Hz")]
        [Range(0.2f, 50f)]
        [SerializeField] public float TransmissionFrequency = 1;

        public RepeatedTransmissionConfig(TransmissionProtocol transmissionType, float transmissionFrequency)
        {
            TransmissionType = transmissionType;
            TransmissionFrequency = transmissionFrequency;
        }

        protected virtual void OnValidate() //TODO - OnVlidate needs to come from VC
        {
            if (TransmissionFrequency > 1)
                TransmissionFrequency = Mathf.RoundToInt(TransmissionFrequency);
        }
    }

    [Serializable]
    internal class TransmissionConfig
    {
        [SerializeField] public TransmissionProtocol TransmissionType;
    }
}