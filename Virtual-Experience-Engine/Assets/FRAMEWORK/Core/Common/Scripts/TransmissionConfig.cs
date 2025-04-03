using System;
using UnityEngine;

namespace VE2.Core.Common
{
    [Serializable]
    public class RepeatedTransmissionConfig : TransmissionConfig
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
    public class TransmissionConfig
    {
        [SerializeField] public TransmissionProtocol TransmissionType;
    }

    public enum TransmissionProtocol 
    {
        UDP,
        TCP
    }
}