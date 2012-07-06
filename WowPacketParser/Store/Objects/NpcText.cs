using PacketParser.Enums;
using PacketParser.Misc;
namespace PacketParser.DataStructures
{
    public class NpcText : ITextOutputDisabled
    {
        public float[] Probabilities;

        public string[] Texts1;

        public string[] Texts2;

        public Language[] Languages;

        public uint[][] EmoteDelays;

        public EmoteType[][] EmoteIds;
    }
}
