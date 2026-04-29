using System.Collections.Generic;
using System.Numerics;

namespace ReplayFiles.Core
{
    public class S2C_PlayAnimationData
    {
        public uint RoutingNetID { get; set; }
        public AnimationFlags AnimationFlags { get; set; }
        public float ScaleTime { get; set; }
        public float StartProgress { get; set; }
        public float SpeedRatio { get; set; }
        public string AnimationName { get; set; } = "";
    }

    public class S2C_PlayAnimationParser : GamePacketParser
    {
        protected override object ReadBody(uint routingNetId, ref SpanReader reader)
        {
            return new S2C_PlayAnimationData
            {
                RoutingNetID = routingNetId,
                AnimationFlags = (AnimationFlags)reader.ReadByte(),
                ScaleTime = reader.ReadSingleLittleEndian(),
                StartProgress = reader.ReadSingleLittleEndian(),
                SpeedRatio = reader.ReadSingleLittleEndian(),
                AnimationName = reader.ReadFixedString(64).TrimEnd('\0')
            };
        }
    }

    public class S2C_StopAnimationData
    {
        public uint RoutingNetID { get; set; }
        public bool Fade { get; set; }
        public bool IgnoreLock { get; set; }
        public bool StopAll { get; set; }
        public string AnimationName { get; set; } = "";
    }

    public class S2C_StopAnimationParser : GamePacketParser
    {
        protected override object ReadBody(uint routingNetId, ref SpanReader reader)
        {
            byte flags = reader.ReadByte();
            return new S2C_StopAnimationData
            {
                RoutingNetID = routingNetId,
                Fade = (flags & 1) != 0,
                IgnoreLock = (flags & 2) != 0,
                StopAll = (flags & 4) != 0,
                AnimationName = reader.ReadFixedString(64).TrimEnd('\0')
            };
        }
    }

    public class S2C_SetAnimStatesData
    {
        public uint RoutingNetID { get; set; }
        public Dictionary<string, string> AnimationOverrides { get; set; } = new Dictionary<string, string>();
    }

    public class S2C_SetAnimStatesParser : GamePacketParser
    {
        protected override object ReadBody(uint routingNetId, ref SpanReader reader)
        {
            var data = new S2C_SetAnimStatesData { RoutingNetID = routingNetId };
            int count = reader.ReadByte();

            for (int i = 0; i < count; i++)
            {
                int fromLen = reader.ReadInt32LittleEndian();
                string fromAnim = reader.ReadFixedString(fromLen).TrimEnd('\0');

                int toLen = reader.ReadInt32LittleEndian();
                string toAnim = reader.ReadFixedString(toLen).TrimEnd('\0');

                data.AnimationOverrides[fromAnim] = toAnim;
            }
            return data;
        }
    }
}