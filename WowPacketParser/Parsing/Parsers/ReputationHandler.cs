using System;
using WowPacketParser.Enums;
using WowPacketParser.Misc;

namespace WowPacketParser.Parsing.Parsers
{
    public static class ReputationHandler
    {
        [Parser(Opcode.SMSG_INITIALIZE_FACTIONS)]
        public static void HandleInitializeFactions(Packet packet)
        {
            var count = packet.ReadInt32("Count");
            packet.StoreBeginList("Factions");
            for (var i = 0; i < count; i++)
            {
                packet.ReadEnum<FactionFlag>("Faction Flags", TypeCode.Byte, i);
                packet.ReadInt32("Faction Standing", i);
            }
            packet.StoreEndList();
        }

        [Parser(Opcode.SMSG_SET_FACTION_VISIBLE)]
        [Parser(Opcode.CMSG_SET_WATCHED_FACTION)]
        public static void HandleSetFactionMisc(Packet packet)
        {
            packet.ReadUInt32("Faction Id");
        }

        [Parser(Opcode.SMSG_SET_FORCED_REACTIONS)]
        public static void HandleForcedReactions(Packet packet)
        {
            var counter = packet.ReadInt32("Faction Count");
            packet.StoreBeginList("Factions");
            for (var i = 0; i < counter; i++)
            {
                packet.ReadUInt32("Faction Id", i);
                packet.ReadUInt32("Reputation Rank", i);
            }
            packet.StoreEndList();
        }

        [Parser(Opcode.SMSG_SET_FACTION_STANDING)]
        public static void HandleSetFactionStanding(Packet packet)
        {
            if (ClientVersion.AddedInVersion(ClientVersionBuild.V2_4_0_8089))
                packet.ReadSingle("Reputation loss");

            if (ClientVersion.AddedInVersion(ClientType.WrathOfTheLichKing))
                packet.ReadBoolean("Play Visual");

            var count = packet.ReadInt32("Count");
            packet.StoreBeginList("Factions");
            for (var i = 0; i < count; i++)
            {
                packet.ReadInt32("Faction List Id", i);
                packet.ReadInt32("Standing", i);
            }
            packet.StoreEndList();
        }
    }
}
