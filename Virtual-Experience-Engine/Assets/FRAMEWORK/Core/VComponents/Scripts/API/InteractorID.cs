using System.Collections;
using System.Collections.Generic;
using System.IO;
using static VE2.Core.Common.CommonSerializables;

//Note, this lives in the VC API rather than the player API as the VC interfaces need to take InteractorIDs as arguments
namespace VE2.Core.VComponents.API
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

    /// <summary>
    /// Not to be confused with ViRSE.Core.VComponents.Plugin
    /// </summary>
    public enum InteractorType
    {   
        None,
        Mouse2D,
        LeftHandVR,
        RightHandVR,
        Feet
    }
}