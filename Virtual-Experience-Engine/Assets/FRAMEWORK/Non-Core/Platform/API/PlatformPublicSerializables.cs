using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VE2.Platform.API
{
    using System.IO;
    using static VE2.Common.CommonSerializables;

    public class PlatformPublicSerializables
    {
        public class ServerConnectionSettings : VE2Serializable
        {
            public string Username;
            public string Password;
            public string ServerAddress;
            public ushort ServerPort;

            public ServerConnectionSettings() { }

            public ServerConnectionSettings(string username, string password, string serverAddress, ushort serverPort)
            {
                Username = username;
                Password = password;
                ServerAddress = serverAddress;
                ServerPort = serverPort;
            }

            public ServerConnectionSettings(byte[] bytes) : base(bytes) { }

            protected override byte[] ConvertToBytes()
            {
                using MemoryStream stream = new();
                using BinaryWriter writer = new(stream);

                writer.Write(Username);
                writer.Write(Password);
                writer.Write(ServerAddress);
                writer.Write(ServerPort);

                return stream.ToArray();
            }
            protected override void PopulateFromBytes(byte[] data)
            {
                using MemoryStream stream = new(data);
                using BinaryReader reader = new(stream);

                Username = reader.ReadString();
                Password = reader.ReadString();
                ServerAddress = reader.ReadString();
                ServerPort = reader.ReadUInt16();
            }
        }
    }

}
