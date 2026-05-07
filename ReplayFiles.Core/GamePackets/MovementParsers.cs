using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Serialization;

namespace ReplayFiles.Core
{
    public class SpeedParamsData
    {
        public float PathSpeedOverride { get; set; }
        public float ParabolicGravity { get; set; }
        public Vector2 ParabolicStartPoint { get; set; }
        public bool Facing { get; set; }
        public uint FollowNetID { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string FollowTargetName { get; set; }

        public float FollowDistance { get; set; }
        public float FollowBackDistance { get; set; }
        public float FollowTravelTime { get; set; }
    }

    public class MovementData
    {
        public string MoveType { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int SyncID { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public uint UnitNetID { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool HasTeleportID { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public byte TeleportID { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SpeedParamsData SpeedParams { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<Vector2> Waypoints { get; set; }
    }

    public class WaypointGroupData
    {
        public string Type { get; set; }
        public uint SenderNetID { get; set; }
        public int SyncID { get; set; }
        public short Count { get; set; }
        public List<MovementData> Movements { get; set; } = new List<MovementData>();
    }

    public class WaypointListHeroWithSpeedData
    {
        public string Type { get; set; }
        public uint SenderNetID { get; set; }
        public uint SyncID { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public SpeedParamsData SpeedParams { get; set; }
        public short Count { get; set; }
        public List<Vector2> Waypoints { get; set; } = new List<Vector2>();
    }

    public class MovementDriverHomingData
    {
        public uint TargetNetID { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string TargetName { get; set; } // To maybe get a name later on to cross reference to get a better idea of what the target is

        public float TargetHeightModifier { get; set; }
        public Vector3 TargetPosition { get; set; }
        public float Speed { get; set; }
        public float Gravity { get; set; }
        public float RateOfTurn { get; set; }
        public float Duration { get; set; }
        public uint MovementPropertyFlags { get; set; }
    }

    public class MovementDriverReplicationData
    {
        public string Type { get; set; }
        public uint SenderNetID { get; set; }
        public byte MovementTypeID { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public MovementDriverHomingData HomingData { get; set; }
    }

    public class WaypointGroupParser : GamePacketParser
    {
        protected override object ReadBody(uint routingNetId, ref SpanReader reader)
        {
            try
            {
                var data = new WaypointGroupData { Type = "WaypointGroup", SenderNetID = routingNetId };
                if (reader.Remaining >= 4) data.SyncID = reader.ReadInt32LittleEndian();
                if (reader.Remaining >= 2) data.Count = reader.ReadInt16LittleEndian();

                for (int i = 0; i < data.Count; i++)
                {
                    var move = new MovementData { MoveType = "Normal", SyncID = data.SyncID };
                    if (reader.Remaining < 1) break;

                    byte bitfield = reader.ReadByte();
                    byte size = (byte)(bitfield >> 1);
                    move.HasTeleportID = (bitfield & 1) != 0;

                    if (size > 0)
                    {
                        if (reader.Remaining >= 4) move.UnitNetID = reader.ReadUInt32LittleEndian();
                        if (move.HasTeleportID && reader.Remaining >= 1) move.TeleportID = reader.ReadByte();

                        move.Waypoints = new List<Vector2>();
                        for (int w = 0; w < size; w++)
                        {
                            if (reader.Remaining >= 2) move.Waypoints.Add(new Vector2(reader.ReadSByte(), reader.ReadSByte()));
                        }
                    }
                    data.Movements.Add(move);
                }
                return data;
            }
            catch 
            { 
                return null; 
            }
        }
    }

    public class WaypointGroupWithSpeedParser : GamePacketParser
    {
        protected override object ReadBody(uint routingNetId, ref SpanReader reader)
        {
            try
            {
                var data = new WaypointGroupData { Type = "WaypointGroupWithSpeed", SenderNetID = routingNetId };
                if (reader.Remaining >= 4) data.SyncID = reader.ReadInt32LittleEndian();
                if (reader.Remaining >= 2) data.Count = reader.ReadInt16LittleEndian();

                for (int i = 0; i < data.Count; i++)
                {
                    var move = new MovementData { MoveType = "WithSpeed", SyncID = data.SyncID };
                    if (reader.Remaining < 1) break;

                    byte bitfield = reader.ReadByte();
                    byte size = (byte)(bitfield >> 1);
                    move.HasTeleportID = (bitfield & 1) != 0;

                    if (size > 0)
                    {
                        if (reader.Remaining >= 4) move.UnitNetID = reader.ReadUInt32LittleEndian();
                        if (move.HasTeleportID && reader.Remaining >= 1) move.TeleportID = reader.ReadByte();

                        if (reader.Remaining >= 33)
                        {
                            move.SpeedParams = new SpeedParamsData
                            {
                                PathSpeedOverride = reader.ReadSingleLittleEndian(),
                                ParabolicGravity = reader.ReadSingleLittleEndian(),
                                ParabolicStartPoint = new Vector2(reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian()),
                                Facing = reader.ReadByte() != 0,
                                FollowNetID = reader.ReadUInt32LittleEndian(),
                                FollowDistance = reader.ReadSingleLittleEndian(),
                                FollowBackDistance = reader.ReadSingleLittleEndian(),
                                FollowTravelTime = reader.ReadSingleLittleEndian()
                            };
                        }

                        move.Waypoints = new List<Vector2>();
                        for (int w = 0; w < size; w++)
                        {
                            if (reader.Remaining >= 2) move.Waypoints.Add(new Vector2(reader.ReadSByte(), reader.ReadSByte()));
                        }
                    }
                    data.Movements.Add(move);
                }
                return data;
            }
            catch 
            { 
                return null; 
            }
        }
    }

    public class WaypointListParser : GamePacketParser
    {
        protected override object ReadBody(uint routingNetId, ref SpanReader reader)
        {
            try
            {
                var data = new WaypointListHeroWithSpeedData { Type = "WaypointList", SenderNetID = routingNetId };
                if (reader.Remaining >= 4) data.SyncID = reader.ReadUInt32LittleEndian();
                if (reader.Remaining >= 2) data.Count = reader.ReadInt16LittleEndian();

                for (int i = 0; i < data.Count; i++)
                {
                    if (reader.Remaining >= 8) data.Waypoints.Add(new Vector2(reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian()));
                }
                return data;
            }
            catch 
            {
                return null; 
            }
        }
    }

    public class WaypointListHeroWithSpeedParser : GamePacketParser
    {
        protected override object ReadBody(uint routingNetId, ref SpanReader reader)
        {
            try
            {
                var data = new WaypointListHeroWithSpeedData { Type = "WaypointListHeroWithSpeed", SenderNetID = routingNetId };
                if (reader.Remaining >= 4) data.SyncID = reader.ReadUInt32LittleEndian();

                if (reader.Remaining >= 33)
                {
                    data.SpeedParams = new SpeedParamsData
                    {
                        PathSpeedOverride = reader.ReadSingleLittleEndian(),
                        ParabolicGravity = reader.ReadSingleLittleEndian(),
                        ParabolicStartPoint = new Vector2(reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian()),
                        Facing = reader.ReadByte() != 0,
                        FollowNetID = reader.ReadUInt32LittleEndian(),
                        FollowDistance = reader.ReadSingleLittleEndian(),
                        FollowBackDistance = reader.ReadSingleLittleEndian(),
                        FollowTravelTime = reader.ReadSingleLittleEndian()
                    };
                }

                if (reader.Remaining >= 2) data.Count = reader.ReadInt16LittleEndian();

                for (int i = 0; i < data.Count; i++)
                {
                    if (reader.Remaining >= 8) data.Waypoints.Add(new Vector2(reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian()));
                }
                return data;
            }
            catch 
            {
                return null; 
            }
        }
    }

    public class MovementDriverReplicationParser : GamePacketParser
    {
        protected override object ReadBody(uint routingNetId, ref SpanReader reader)
        {
            try
            {
                var data = new MovementDriverReplicationData { Type = "MovementDriverReplication", SenderNetID = routingNetId };
                if (reader.Remaining < 25) return data;

                data.MovementTypeID = reader.ReadByte();
                data.Position = new Vector3(reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian());
                data.Velocity = new Vector3(reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian());

                int paramType = reader.ReadInt32LittleEndian();
                if (paramType == 1 && reader.Remaining >= 32)
                {
                    data.HomingData = new MovementDriverHomingData
                    {
                        TargetNetID = reader.ReadUInt32LittleEndian(),
                        TargetHeightModifier = reader.ReadSingleLittleEndian(),
                        TargetPosition = new Vector3(reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian(), reader.ReadSingleLittleEndian()),
                        Speed = reader.ReadSingleLittleEndian(),
                        Gravity = reader.ReadSingleLittleEndian(),
                        RateOfTurn = reader.ReadSingleLittleEndian(),
                        Duration = reader.ReadSingleLittleEndian(),
                        MovementPropertyFlags = reader.ReadUInt32LittleEndian()
                    };
                }
                return data;
            }
            catch 
            {
                return null; 
            }
        }
    }
}