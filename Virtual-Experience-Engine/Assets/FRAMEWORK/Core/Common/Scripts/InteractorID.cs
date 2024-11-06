using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VE2.Common;
using VE2.Core.Common;
using static VE2.Common.CommonSerializables;

namespace VE2
{
    [System.Serializable]   
    public class InteractorID : VE2Serializable 
    {
        public ushort ClientID { get; private set; }
        public InteractorType InteractorType { get; private set; }

        public InteractorID(ushort clientID, InteractorType interactorType)
        {
            ClientID = clientID;
            InteractorType = interactorType;
        }

        public InteractorID(byte[] bytes):base(bytes) { }

        public override string ToString()
        {
            return $"Client{ClientID}-{InteractorType}";
        }

        public override bool Equals(object obj)
        {
            return obj is InteractorID iD &&
                   ClientID == iD.ClientID &&
                   InteractorType == iD.InteractorType;
        }
        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(ClientID);
            writer.Write((ushort)InteractorType);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] data)
        {
            using MemoryStream stream = new(data);
            using BinaryReader reader = new(stream);

            ClientID = reader.ReadUInt16();
            InteractorType = (InteractorType)reader.ReadUInt16();
        }
    }
}