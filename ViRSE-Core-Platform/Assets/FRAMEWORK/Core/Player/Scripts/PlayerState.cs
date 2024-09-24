using System.IO;
using UnityEngine;
using ViRSE;
using static ViRSE.Core.Shared.CoreCommonSerializables;

//TODO, we know the player will always be rotated to be level with the floor 
//So that means we can actually just transmit a single float for the rotation angle
public class PlayerState : ViRSESerializable 
{
    public Vector3 RootPosition { get; private set;}
    public Quaternion RootRotation { get; private set; }
    public Vector3 HeadPosition { get; private set; }
    public Quaternion HeadRotation { get; private set; }

    public PlayerState(byte[] bytes) : base(bytes) { }

    public PlayerState(Vector3 rootPosition, Quaternion rootRotation, Vector3 headPosition, Quaternion headRotation)
    {
        RootPosition = rootPosition;
        RootRotation = rootRotation;
        HeadPosition = headPosition;
        HeadRotation = headRotation;
    }

    protected override byte[] ConvertToBytes()
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.Write(RootPosition.x);
        writer.Write(RootPosition.y);
        writer.Write(RootPosition.z);

        writer.Write(RootRotation.x); 
        writer.Write(RootRotation.y); 
        writer.Write(RootRotation.z); 
        writer.Write(RootRotation.w);

        writer.Write(HeadPosition.x);
        writer.Write(HeadPosition.y);
        writer.Write(HeadPosition.z);

        writer.Write(HeadRotation.x);
        writer.Write(HeadRotation.y);
        writer.Write(HeadRotation.z);
        writer.Write(HeadRotation.w);

        return stream.ToArray();
    }

    protected override void PopulateFromBytes(byte[] data)
    {
        using MemoryStream stream = new(data);
        using BinaryReader reader = new(stream);

        RootPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        RootRotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        HeadPosition = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        HeadRotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }
}

public class PlayerStateWrapper : ViRSESerializable
{
    public ushort ID { get; private set; }
    public byte[] StateBytes { get; private set; }

    public PlayerStateWrapper(byte[] bytes) : base(bytes) { }

    public PlayerStateWrapper(ushort id, byte[] state)
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

        ID = reader.ReadUInt16();

        int stateBytesLength = reader.ReadUInt16();
        StateBytes = reader.ReadBytes(stateBytesLength);
    }
}
