using PacketParser.Misc;

namespace PacketParser.DataStructures
{
    public class QuestPOIPoint : ITextOutputDisabled
    {
        public int Index; // Client expects a certain order although this is not on sniffs

        public int X;

        public int Y;
    }
}
