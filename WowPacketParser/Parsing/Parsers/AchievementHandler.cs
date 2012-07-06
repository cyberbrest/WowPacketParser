using System;
using PacketParser.Enums;
using PacketParser.Misc;
using Guid = PacketParser.DataStructures.Guid;
using PacketParser.Processing;
using PacketParser.DataStructures;

namespace PacketParser.Parsing.Parsers
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
            var name = packet.ReadCString("Player Name");
            var guid = packet.ReadGuid("Player GUID");
            PacketFileProcessor.Current.GetProcessor<NameStore>().AddPlayerName(guid, name);
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
            packet.ReadInt32("Unk Int32"); // some flag... & 1 -> delete
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

                packet.ReadInt32("Unk Int32", i); // Unk flag, same as in SMSG_CRITERIA_UPDATE

                packet.ReadPackedTime("Criteria Time", i);

                packet.StoreBeginList("Timers", i);
                for (var j = 0; j < 2; j++)
                    packet.ReadInt32("Timer", i, j);
                packet.StoreEndList();
                ++i;
            }
            packet.StoreEndList();
        }

        [Parser(Opcode.SMSG_ALL_ACHIEVEMENT_DATA, ClientVersionBuild.Zero, ClientVersionBuild.V4_0_6a_13623)]
        public static void HandleAllAchievementData(Packet packet)
        {
            ReadAllAchievementData(ref packet);
        }

        [Parser(Opcode.SMSG_ALL_ACHIEVEMENT_DATA, ClientVersionBuild.V4_0_6a_13623, ClientVersionBuild.V4_1_0_13914)]
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
                if (ClientVersion.AddedInVersion(ClientVersionBuild.V4_3_4_15595))
                    HandleAllAchievementData434(packet);
                else if (ClientVersion.AddedInVersion(ClientVersionBuild.V4_2_2_14545))
                    HandleAllAchievementData422(packet);
                else if (ClientVersion.AddedInVersion(ClientVersionBuild.V4_0_6a_13623))
                    HandleAllAchievementData406(packet);
                else
                    HandleAllAchievementData(packet);
        }

        [Parser(Opcode.SMSG_ALL_ACHIEVEMENT_DATA, ClientVersionBuild.V4_2_2_14545, ClientVersionBuild.V4_3_0_15005)]
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

        [Parser(Opcode.SMSG_ALL_ACHIEVEMENT_DATA, ClientVersionBuild.V4_3_4_15595)]
        public static void HandleAllAchievementData434(Packet packet)
        {
            var criterias = packet.ReadBits("Criteria count", 21);
            var counter = new byte[criterias][];
            var guid = new byte[criterias][];
            var flags = new byte[criterias];
            var criteriasList = packet.StoreBeginList("Criterias");
            for (var i = 0; i < criterias; ++i)
            {
                counter[i] = new byte[8];
                guid[i] = new byte[8];

                guid[i][4] = (byte)(packet.ReadBit() ? 1 : 0);
                counter[i][3] = (byte)(packet.ReadBit() ? 1 : 0);
                guid[i][5] = (byte)(packet.ReadBit() ? 1 : 0);
                counter[i][0] = (byte)(packet.ReadBit() ? 1 : 0);
                counter[i][6] = (byte)(packet.ReadBit() ? 1 : 0);
                guid[i][3] = (byte)(packet.ReadBit() ? 1 : 0);
                guid[i][0] = (byte)(packet.ReadBit() ? 1 : 0);
                counter[i][4] = (byte)(packet.ReadBit() ? 1 : 0);
                guid[i][2] = (byte)(packet.ReadBit() ? 1 : 0);
                counter[i][7] = (byte)(packet.ReadBit() ? 1 : 0);
                guid[i][7] = (byte)(packet.ReadBit() ? 1 : 0);
                flags[i] = (byte)(packet.ReadBits(2) & 0xFFu);
                guid[i][6] = (byte)(packet.ReadBit() ? 1 : 0);
                counter[i][2] = (byte)(packet.ReadBit() ? 1 : 0);
                counter[i][1] = (byte)(packet.ReadBit() ? 1 : 0);
                counter[i][5] = (byte)(packet.ReadBit() ? 1 : 0);
                guid[i][1] = (byte)(packet.ReadBit() ? 1 : 0);
            }
            packet.StoreEndList();

            var achievements = packet.ReadBits("Achievement count", 23);
            packet.StoreContinueList(criteriasList);
            for (var i = 0; i < criterias; ++i)
            {
                if (guid[i][3] != 0) guid[i][3] ^= packet.ReadByte();
                if (counter[i][5] != 0) counter[i][5] ^= packet.ReadByte();
                if (counter[i][6] != 0) counter[i][6] ^= packet.ReadByte();
                if (guid[i][4] != 0) guid[i][4] ^= packet.ReadByte();
                if (guid[i][6] != 0) guid[i][6] ^= packet.ReadByte();
                if (counter[i][2] != 0) counter[i][2] ^= packet.ReadByte();

                packet.ReadUInt32("Criteria Timer 2", i);

                if (guid[i][2] != 0) guid[i][2] ^= packet.ReadByte();

                packet.ReadUInt32("Criteria Id", i);

                if (guid[i][5] != 0) guid[i][5] ^= packet.ReadByte();
                if (counter[i][0] != 0) counter[i][0] ^= packet.ReadByte();
                if (counter[i][3] != 0) counter[i][3] ^= packet.ReadByte();
                if (counter[i][1] != 0) counter[i][1] ^= packet.ReadByte();
                if (counter[i][4] != 0) counter[i][4] ^= packet.ReadByte();
                if (guid[i][0] != 0) guid[i][0] ^= packet.ReadByte();
                if (guid[i][7] != 0) guid[i][7] ^= packet.ReadByte();
                if (counter[i][7] != 0) counter[i][7] ^= packet.ReadByte();

                packet.ReadUInt32("Criteria Timer 1", i);
                packet.ReadPackedTime("Criteria Date", i);

                if (guid[i][1] != 0) guid[i][1] ^= packet.ReadByte();

                packet.Store("Criteria Flags", flags[i], i);
                packet.Store("Criteria Counter", BitConverter.ToUInt64(counter[i], 0), i);
                packet.StoreBitstreamGuid("Criteria GUID", guid[i], i);
            }
            packet.StoreEndList();

            var achievementsList = packet.StoreBeginList("Achievements");
            for (var i = 0; i < achievements; ++i)
            {
                packet.ReadUInt32("Achievement Id", i);
                packet.ReadPackedTime("Achievement Date", i);
            }
            packet.StoreEndList();
        }
    }
}
