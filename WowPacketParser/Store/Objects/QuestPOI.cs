using System.Collections.Generic;
using PacketParser.Misc;

namespace PacketParser.DataStructures
{
    public class QuestPOI : ITextOutputDisabled
    {
        public int ObjectiveIndex;

        public int Map;

        public int WorldMapAreaId;

        public int FloorId;

        public int UnkInt1;

        public int UnkInt2;

        public ICollection<QuestPOIPoint> Points;
    }
}
