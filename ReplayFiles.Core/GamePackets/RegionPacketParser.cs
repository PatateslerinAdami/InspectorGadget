using System.Numerics;

namespace ReplayFiles.Core
{
    public class AddRegionData
    {
        public uint RoutingNetID { get; set; }
        public uint TeamID { get; set; }
        public int RegionType { get; set; }
        public int ClientID { get; set; }
        public uint UnitNetID { get; set; }
        public uint BubbleNetID { get; set; }
        public uint VisionTargetNetID { get; set; }
        public Vector2 Position { get; set; }
        public float TimeToLive { get; set; }
        public float ColisionRadius { get; set; }
        public float GrassRadius { get; set; }
        public float SizeMultiplier { get; set; }
        public float SizeAdditive { get; set; }
        public bool HasCollision { get; set; }
        public bool GrantVision { get; set; }
        public bool RevealStealth { get; set; }
        public float BaseRadius { get; set; }
    }

    public class AddRegionParser : GamePacketParser
    {
        protected override object ReadBody(uint routingNetId, ref SpanReader reader)
        {
            var data = new AddRegionData { RoutingNetID = routingNetId };
            data.TeamID = reader.ReadUInt32LittleEndian();
            data.RegionType = reader.ReadInt32LittleEndian();
            data.ClientID = reader.ReadInt32LittleEndian();
            data.UnitNetID = reader.ReadUInt32LittleEndian();
            data.BubbleNetID = reader.ReadUInt32LittleEndian();
            data.VisionTargetNetID = reader.ReadUInt32LittleEndian();
            data.Position = new Vector2(reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian());
            data.TimeToLive = reader.ReadSingleLittleEndian();
            data.ColisionRadius = reader.ReadSingleLittleEndian();
            data.GrassRadius = reader.ReadSingleLittleEndian();
            data.SizeMultiplier = reader.ReadSingleLittleEndian();
            data.SizeAdditive = reader.ReadSingleLittleEndian();

            byte flags = reader.ReadByte();
            data.HasCollision = (flags & 1) != 0;
            data.GrantVision = (flags & 2) != 0;
            data.RevealStealth = (flags & 4) != 0;

            data.BaseRadius = reader.ReadSingleLittleEndian();

            return data;
        }
    }

    public class S2C_MoveRegionData
    {
        public uint RoutingNetID { get; set; }
        public uint RegionNetID { get; set; }
        public Vector2 Position { get; set; }
    }

    public class S2C_MoveRegionParser : GamePacketParser
    {
        protected override object ReadBody(uint routingNetId, ref SpanReader reader)
        {
            return new S2C_MoveRegionData
            {
                RoutingNetID = routingNetId,
                RegionNetID = reader.ReadUInt32LittleEndian(),
                Position = new Vector2(reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian())
            };
        }
    }

    public class RemoveRegionData
    {
        public uint RoutingNetID { get; set; }
        public uint RegionNetID { get; set; }
    }

    public class RemoveRegionParser : GamePacketParser
    {
        protected override object ReadBody(uint routingNetId, ref SpanReader reader)
        {
            return new RemoveRegionData
            {
                RoutingNetID = routingNetId,
                RegionNetID = reader.ReadUInt32LittleEndian()
            };
        }
    }
    
    public class AddConeRegionData : AddRegionData
    {
        public float ConeAngle { get; set; }
        public float Unknown2 { get; set; }
        public float Unknown3 { get; set; }
    }

    public class AddConeRegionParser : GamePacketParser
    {
        protected override object ReadBody(uint routingNetId, ref SpanReader reader)
        {
            var data = new AddConeRegionData { RoutingNetID = routingNetId };
            data.TeamID = reader.ReadUInt32LittleEndian();
            data.RegionType = reader.ReadInt32LittleEndian();
            data.ClientID = reader.ReadInt32LittleEndian();
            data.UnitNetID = reader.ReadUInt32LittleEndian();
            data.BubbleNetID = reader.ReadUInt32LittleEndian();
            data.VisionTargetNetID = reader.ReadUInt32LittleEndian();
            data.Position = new Vector2(reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian());
            data.TimeToLive = reader.ReadSingleLittleEndian();
            data.ColisionRadius = reader.ReadSingleLittleEndian();
            data.GrassRadius = reader.ReadSingleLittleEndian();
            data.SizeMultiplier = reader.ReadSingleLittleEndian();
            data.SizeAdditive = reader.ReadSingleLittleEndian();

            byte flags = reader.ReadByte();
            data.HasCollision = (flags & 1) != 0;
            data.GrantVision = (flags & 2) != 0;
            data.RevealStealth = (flags & 4) != 0;

            data.BaseRadius = reader.ReadSingleLittleEndian();

            data.ConeAngle = reader.ReadSingleLittleEndian();
            data.Unknown2 = reader.ReadSingleLittleEndian();
            data.Unknown3 = reader.ReadSingleLittleEndian();

            return data;
        }
    }
}