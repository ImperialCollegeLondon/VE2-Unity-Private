using System.IO;
using ViRSE;

public class PlayerSettings
{
    public string DisplayName;
    public float MasterVolume;
    public float GameVolume;
    public float ChatVolume;
    public ushort HeadType;
    public ushort TorsoType;
    public ushort HandsType;
    public float ColorRed;
    public float ColorGreen;
    public float ColorBlue;
    public float LookSensitivity;
    public bool HoldToCrouch;
    public float DragSpeed;
    public bool DragDarkening;
    public bool TeleportDarkening;
    public bool SnapTurnDarkening;
    public float SnapTurnAmount;
    public bool Vibrate;
    public bool ToolCycling;
    public bool ControllerLabels;
    public float WristLookPrecision;

    public byte[] Bytes { get => GetSettingsAsBytes(); private set => PopulateFromByteData(value); }

    private byte[] GetSettingsAsBytes()
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);

        writer.Write(DisplayName);
        writer.Write(MasterVolume);
        writer.Write(GameVolume);
        writer.Write(ChatVolume);
        writer.Write(HeadType);
        writer.Write(TorsoType);
        writer.Write(HandsType);
        writer.Write(ColorRed);
        writer.Write(ColorGreen);
        writer.Write(ColorBlue);
        writer.Write(LookSensitivity);
        writer.Write(HoldToCrouch);
        writer.Write(DragSpeed);
        writer.Write(DragDarkening);
        writer.Write(TeleportDarkening);
        writer.Write(SnapTurnDarkening);
        writer.Write(SnapTurnAmount);
        writer.Write(Vibrate);
        writer.Write(ToolCycling);
        writer.Write(ControllerLabels);
        writer.Write(WristLookPrecision);

        return stream.ToArray();
    }

    private void PopulateFromByteData(byte[] data)
    {
        using MemoryStream stream = new(data);
        using BinaryReader reader = new(stream);

        DisplayName = reader.ReadString();
        MasterVolume = reader.ReadSingle();
        GameVolume = reader.ReadSingle();
        ChatVolume = reader.ReadSingle();
        HeadType = reader.ReadUInt16();
        TorsoType = reader.ReadUInt16();
        HandsType = reader.ReadUInt16();
        ColorRed = reader.ReadSingle();
        ColorGreen = reader.ReadSingle();
        ColorBlue = reader.ReadSingle();
        LookSensitivity = reader.ReadSingle();
        HoldToCrouch = reader.ReadBoolean();
        DragSpeed = reader.ReadSingle();
        DragDarkening = reader.ReadBoolean();
        TeleportDarkening = reader.ReadBoolean();
        SnapTurnDarkening = reader.ReadBoolean();
        SnapTurnAmount = reader.ReadSingle();
        Vibrate = reader.ReadBoolean();
        ToolCycling = reader.ReadBoolean();
        ControllerLabels = reader.ReadBoolean();
        WristLookPrecision = reader.ReadSingle();
    }
}

