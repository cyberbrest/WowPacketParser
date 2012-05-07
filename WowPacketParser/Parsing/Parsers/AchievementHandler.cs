using WowPacketParser.Enums;
using WowPacketParser.Misc;

namespace WowPacketParser.Parsing.Parsers
{
    public static class AchievementHandler
    {
        [Parser(Opcode.SMSG_ACHIEVEMENT_DELETED)]
        [Parser(Opcode.SMSG_CRITERIA_DELETED)]
        public static void HandleDeleted(Packet packet)
        {
            packet.ReadInt32("ID");
        }

        [Parser(Opcode.SMSG_SERVER_FIRST_ACHIEVEMENT)]
        public static void HandleServerFirstAchievement(Packet packet)
        {
            packet.ReadCString("Player Name");
            packet.ReadGuid("Player GUID");
            packet.ReadInt32("Achievement");
            packet.ReadInt32("Linked Name");
        }

        [Parser(Opcode.SMSG_ACHIEVEMENT_EARNED)]
        public static void HandleAchievementEarned(Packet packet)
        {
            packet.ReadPackedGuid("Player GUID");
            packet.ReadInt32("Achievement");
            packet.ReadPackedTime("Time");
            packet.ReadInt32("Unk Int32");
        }

        [Parser(Opcode.SMSG_CRITERIA_UPDATE)]
        public static void HandleCriteriaUpdate(Packet packet)
        {
            packet.ReadInt32("Criteria ID");
            packet.ReadPackedGuid("Criteria Counter");
            packet.ReadPackedGuid("Player GUID");
            packet.ReadInt32("Unk Int32");
            packet.ReadPackedTime("Time");

            packet.StoreBeginList("Timers");
            for (var i = 0; i < 2; i++)
                packet.ReadInt32("Timer", i);
            packet.StoreEndList();
        }

        public static void ReadAllAchievementData(ref Packet packet)
        {
            var i = 0;
            packet.StoreBeginList("Achievements");
            while (true)
            {
                var id = packet.ReadInt32();

                if (id == -1)
                    break;

                packet.Store("Achievement ID", id, i);

                packet.ReadPackedTime("Achievement Time", i);
                ++i;
            }
            packet.StoreEndList();

            i = 0;
            packet.StoreBeginList("Criterias");
            while (true)
            {
                var id = packet.ReadInt32();

                if (id == -1)
                    break;

                packet.Store("Criteria ID", id, i);

                var counter = packet.ReadPackedGuid();
                packet.Store("Criteria Counter", counter.Full, i);

                packet.ReadPackedGuid("Player GUID", i);

                packet.ReadInt32("Unk Int32", i);

                packet.ReadPackedTime("Criteria Time", i);

                packet.StoreBeginList("Timers", i);
                for (var j = 0; j < 2; j++)
                    packet.ReadInt32("Timer", i, j);
                packet.StoreEndList();
                ++i;
            }
            packet.StoreEndList();
        }

        [Parser(Opcode.SMSG_ALL_ACHIEVEMENT_DATA)]
        public static void HandleAllAchievementData(Packet packet)
        {
            ReadAllAchievementData(ref packet);
        }

        [Parser(Opcode.SMSG_ALL_ACHIEVEMENT_DATA, ClientVersionBuild.V4_0_6a_13623)]
        public static void HandleAllAchievementData406(Packet packet)
        {
            var achievements = packet.ReadUInt32("Achievement count");
            var criterias = packet.ReadUInt32("Criterias count");

            var achievementsList = packet.StoreBeginList("Achievements");
            for (var i = 0; i < achievements; ++i)
                packet.ReadUInt32("Achievement Id", 1, i);

            for (var i = 0; i < achievements; ++i)
                packet.ReadPackedTime("Achievement Time", 1, i);
            packet.StoreEndList();

            var criteriasList = packet.StoreBeginList("Criterias");
            for (var i = 0; i < criterias; ++i)
                packet.ReadUInt64("Counter", 0, i);

            for (var i = 0; i < criterias; ++i)
                packet.ReadUInt32("Criteria Timer 1", 0, i);

            for (var i = 0; i < criterias; ++i)
                packet.ReadPackedTime("Criteria Time", 0, i);

            for (var i = 0; i < criterias; ++i)
                packet.ReadGuid("Player GUID", 0, i);

            for (var i = 0; i < criterias; ++i)
                packet.ReadUInt32("Criteria Timer 2", 0, i);

            for (var i = 0; i < criterias; ++i)
                packet.ReadBits("Flag", 2, 0, i);

            for (var i = 0; i < criterias; ++i)
                packet.ReadUInt32("Criteria Id", 0, i);
            packet.StoreEndList();
        }

        [Parser(Opcode.CMSG_QUERY_INSPECT_ACHIEVEMENTS)]
        public static void HandleInspectAchievementData(Packet packet)
        {
            packet.ReadPackedGuid("GUID");
        }

        [Parser(Opcode.SMSG_RESPOND_INSPECT_ACHIEVEMENTS)]
        public static void HandleInspectAchievementDataResponse(Packet packet)
        {
            packet.ReadPackedGuid("Player GUID");
            ReadAllAchievementData(ref packet);
        }

        [Parser(Opcode.SMSG_GUILD_ACHIEVEMENT_DATA)]
        public static void HandleGuildAchievementData(Packet packet)
        {
            var cnt = packet.ReadUInt32("Count");

            packet.StoreBeginList("GuildAchievements");
            for (var i = 0; i < cnt; ++i)
                packet.ReadPackedTime("Date", i);

            for (var i = 0; i < cnt; ++i)
                packet.ReadUInt32("Achievement Id", i);
            packet.StoreEndList();
        }

        [Parser(Opcode.SMSG_COMPRESSED_ACHIEVEMENT_DATA)]
        public static void HandleCompressedAllAchievementData(Packet packet)
        {
            packet.Inflate(packet.ReadInt32());
                if (ClientVersion.AddedInVersion(ClientVersionBuild.V4_2_2_14545))
                    HandleAllAchievementData422(packet);
                else if (ClientVersion.AddedInVersion(ClientVersionBuild.V4_0_6a_13623))
                    HandleAllAchievementData406(packet);
                else
                    HandleAllAchievementData(packet);
        }

        [Parser(Opcode.SMSG_ALL_ACHIEVEMENT_DATA, ClientVersionBuild.V4_2_2_14545)]
        public static void HandleAllAchievementData422(Packet packet)
        {
            var criterias = packet.ReadUInt32("Criterias Count");
            var criteriasList = packet.StoreBeginList("Criterias");
            for (var i = 0; i < criterias; ++i)
                packet.ReadBits("Flag", 2, i);

            for (var i = 0; i < criterias; ++i)
                packet.ReadUInt64("Counter", i);
            packet.StoreEndList();

            var achievements = packet.ReadUInt32("Achievement Count");
            var achievementsList = packet.StoreBeginList("Achievements");
            for (var i = 0; i < achievements; ++i)
                packet.ReadPackedTime("Achievement Time", i);
            packet.StoreEndList();

            packet.StoreContinueList(criteriasList);
            for (var i = 0; i < criterias; ++i)
                packet.ReadGuid("Player GUID", i);

            for (var i = 0; i < criterias; ++i)
                packet.ReadPackedTime("Criteria Time",i);

            for (var i = 0; i < criterias; ++i)
                packet.ReadUInt32("Timer 1", i);
            packet.StoreEndList();

            packet.StoreContinueList(achievementsList);
            for (var i = 0; i < achievements; ++i)
                packet.ReadUInt32("Achievement Id", i);
            packet.StoreEndList();

            packet.StoreContinueList(criteriasList);
            for (var i = 0; i < criterias; ++i)
                packet.ReadUInt32("Criteria Id", i);

            for (var i = 0; i < criterias; ++i)
                packet.ReadUInt32("Timer 2", i);
            packet.StoreEndList();
        }
    }
}
