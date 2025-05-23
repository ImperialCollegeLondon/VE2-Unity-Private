using static VE2.Common.Shared.CommonSerializables;

namespace VE2.Common.Shared
{
    internal interface IWorldStateModule //TODO: Rename to IWorldStateSyncable?
    {
        public VE2Serializable State { get; }
        public TransmissionProtocol TransmissionProtocol { get; }
        public float TransmissionFrequency { get; }
        public string ID { get; }
        public byte[] StateAsBytes { get; set; }
    }
}


