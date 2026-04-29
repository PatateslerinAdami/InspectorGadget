using System.Numerics;

namespace ReplayFiles.Core;

public class FXCreateData
{
    public uint TargetNetID { get; set; }
    public uint NetAssignedNetID { get; set; }
    public uint CasterNetID { get; set; }
    public uint BindNetID { get; set; }
    public uint KeywordNetID { get; set; }
    public short PositionX { get; set; }
    public float PositionY { get; set; }
    public short PositionZ { get; set; }
    public short TargetPositionX { get; set; }
    public float TargetPositionY { get; set; }
    public short TargetPositionZ { get; set; }
    public short OwnerPositionX { get; set; }
    public float OwnerPositionY { get; set; }
    public short OwnerPositionZ { get; set; }
    public Vector3 OrientationVector { get; set; }
    public float TimeSpent { get; set; }
    public float ScriptScale { get; set; }
}

public class FXCreateGroupData
{
    public uint PackageHash { get; set; }
    public uint EffectNameHash { get; set; }
    public FXFlags Flags { get; set; }
    public uint TargetBoneNameHash { get; set; }
    public uint BoneNameHash { get; set; }
    public List<FXCreateData> FXCreateData { get; set; } = new List<FXCreateData>();
}

public class FXCreateGroupPacketData
{
    public List<FXCreateGroupData> Groups { get; set; } = new List<FXCreateGroupData>();
    public int BytesRemaining { get; set; }
}

public class FXCreateGroupParser : GamePacketParser
{
    protected override object ReadBody(uint routingNetId, ref SpanReader reader)
    {
        var result = new FXCreateGroupPacketData();

        int groupCount = reader.ReadByte();
        for (int i = 0; i < groupCount; i++)
        {
            var group = new FXCreateGroupData
            {
                PackageHash = reader.ReadUInt32LittleEndian(),
                EffectNameHash = reader.ReadUInt32LittleEndian(),
                Flags = (FXFlags)reader.ReadUInt16LittleEndian(),
                TargetBoneNameHash = reader.ReadUInt32LittleEndian(),
                BoneNameHash = reader.ReadUInt32LittleEndian()
            };

            int dataCount = reader.ReadByte();
            for (int j = 0; j < dataCount; j++)
            {
                var data = new FXCreateData
                {
                    TargetNetID = reader.ReadUInt32LittleEndian(),
                    NetAssignedNetID = reader.ReadUInt32LittleEndian(),
                    CasterNetID = reader.ReadUInt32LittleEndian(),
                    BindNetID = reader.ReadUInt32LittleEndian(),
                    KeywordNetID = reader.ReadUInt32LittleEndian(),
                    PositionX = reader.ReadInt16LittleEndian(),
                    PositionY = reader.ReadSingleLittleEndian(),
                    PositionZ = reader.ReadInt16LittleEndian(),
                    TargetPositionX = reader.ReadInt16LittleEndian(),
                    TargetPositionY = reader.ReadSingleLittleEndian(),
                    TargetPositionZ = reader.ReadInt16LittleEndian(),
                    OwnerPositionX = reader.ReadInt16LittleEndian(),
                    OwnerPositionY = reader.ReadSingleLittleEndian(),
                    OwnerPositionZ = reader.ReadInt16LittleEndian(),

                    // Read 3 floats for the Vector3
                    OrientationVector = new Vector3(
                        reader.ReadSingleLittleEndian(),
                        reader.ReadSingleLittleEndian(),
                        reader.ReadSingleLittleEndian()),

                    TimeSpent = reader.ReadSingleLittleEndian(),
                    ScriptScale = reader.ReadSingleLittleEndian()
                };
                group.FXCreateData.Add(data);
            }
            result.Groups.Add(group);
        }

        result.BytesRemaining = reader.Remaining;
        return result;
    }
}