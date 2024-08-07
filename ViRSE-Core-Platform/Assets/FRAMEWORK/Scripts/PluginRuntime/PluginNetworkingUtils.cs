using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ViRSE.PluginRuntime
{
    public class WorldStateSnapshot : ViRSENetworkSerializable
    {
        public string InstanceCode { get; private set; }
        public WorldStateBundle WorldStateBundle { get; private set; }

        public WorldStateSnapshot(byte[] bytes) : base(bytes) { }

        public WorldStateSnapshot(string instanceCode, WorldStateBundle worldStateBundle)
        {
            InstanceCode = instanceCode;
            WorldStateBundle = worldStateBundle;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(InstanceCode);

            byte[] WorldStateBundleBytes = WorldStateBundle.Bytes;
            writer.Write(WorldStateBundleBytes.Length);
            writer.Write(WorldStateBundleBytes);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] bytes)
        {
            using MemoryStream stream = new(bytes);
            using BinaryReader reader = new(stream);

            InstanceCode = reader.ReadString();

            int worldStateBundleBytesLength = reader.ReadInt32();
            WorldStateBundle = new(reader.ReadBytes(worldStateBundleBytesLength));
        }
    }

    public class WorldStateBundle : ViRSENetworkSerializable
    {
        public List<WorldStateWrapper> WorldStateWrappers { get; private set; }

        public WorldStateBundle(byte[] bytes) : base(bytes) { }

        public WorldStateBundle(List<WorldStateWrapper> serializedSyncableStates)
        {
            WorldStateWrappers = serializedSyncableStates;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);

            writer.Write((ushort)WorldStateWrappers.Count);
            foreach (WorldStateWrapper worldStateWrapper in WorldStateWrappers)
            {
                byte[] worldStateWrapperBytes = worldStateWrapper.Bytes;
                writer.Write((ushort)worldStateWrapperBytes.Length);
                writer.Write(worldStateWrapperBytes);
            }

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] bytes)
        {
            using MemoryStream stream = new(bytes);
            using BinaryReader reader = new(stream);

            ushort worldStateWrappersCount = reader.ReadUInt16();
            WorldStateWrappers = new List<WorldStateWrapper>();

            for (int i = 0; i < worldStateWrappersCount; i++)
            {
                int stateWrapperBytesLength = reader.ReadUInt16();
                byte[] stateWrapperBytes = reader.ReadBytes(stateWrapperBytesLength);
                WorldStateWrappers.Add(new WorldStateWrapper(stateWrapperBytes));
            }
        }
    }

    public class WorldStateWrapper : ViRSENetworkSerializable
    {
        public string ID { get; private set; }
        public byte[] StateBytes { get; private set; }

        public WorldStateWrapper(byte[] bytes) : base(bytes) { }

        public WorldStateWrapper(string id, byte[] state)
        {
            ID = id;
            StateBytes = state;
        }

        protected override byte[] ConvertToBytes()
        {
            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);

            writer.Write(ID);
            writer.Write((ushort)StateBytes.Length);
            writer.Write(StateBytes);

            return stream.ToArray();
        }

        protected override void PopulateFromBytes(byte[] bytes)
        {
            using MemoryStream stream = new(bytes);
            using BinaryReader reader = new(stream);

            ID = reader.ReadString();

            int stateBytesLength = reader.ReadUInt16();
            StateBytes = reader.ReadBytes(stateBytesLength);
        }
    }

}
