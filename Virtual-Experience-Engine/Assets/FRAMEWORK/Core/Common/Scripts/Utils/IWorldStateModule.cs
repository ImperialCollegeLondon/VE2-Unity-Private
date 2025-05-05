using System;
using System.Collections.Generic;
using VE2.Core.Common;
using static VE2.Core.Common.CommonSerializables;

namespace VE2.Core.Common
{
    public interface IWorldStateModule //TODO: Rename to IWorldStateSyncable?
    {
        public VE2Serializable State { get; }
        public TransmissionProtocol TransmissionProtocol { get; }
        public float TransmissionFrequency { get; }
        public string ID { get; }
        public byte[] StateAsBytes { get; set; }
    }
}


