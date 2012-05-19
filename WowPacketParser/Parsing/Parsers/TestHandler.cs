using WowPacketParser.Enums;
using WowPacketParser.Misc;

namespace WowPacketParser.Parsing.Parsers
{
    public static class TestHandler
    {
        [Parser(62540)]
        public static void Handle62540(Packet packet)
        {
            var count = packet.ReadInt32("Count");

            var unklist = packet.StoreBeginList("UnknownList");
            for (var i = 0; i < count; i++)
                packet.ReadInt32("Unk1", i);

            for (var i = 0; i < count; i++)
                packet.ReadInt32("Unk2", i);
            packet.StoreEndList();

            packet.ReadInt32("Unk3");

            packet.StoreContinueList(unklist);
            for (var i = 0; i < count; i++)
                packet.ReadInt32("Unk4", i);

            for (var i = 0; i < count; i++)
                packet.ReadInt32("Unk5", i);

            for (var i = 0; i < count; i++)
                packet.ReadInt32("Unk6", i);

            for (var i = 0; i < count; i++)
                packet.ReadInt64("Unk7", i);
            packet.StoreEndList();
        }

        [Parser(41694)]
        public static void Handle41694(Packet packet)
        {
            var count = packet.ReadInt32("Count");

            var unklist = packet.StoreBeginList("UnknownList");
            for (var i = 0; i < count; i++)
                packet.ReadInt32("Unk1", i);

            for (var i = 0; i < count; i++)
                packet.ReadGuid("Unk2", i);

            for (var i = 0; i < count; i++)
            {
                var count2 = packet.ReadInt32("UnkCount2");

                packet.StoreBeginList("UnknownList2", i);
                for (var j = 0; j < count2; j++)
                    packet.ReadInt64("Unk", i, j);
                packet.StoreEndList();
            }

            for (var i = 0; i < count; i++)
                packet.ReadInt32("Unk3", i);

            for (var i = 0; i < count; i++)
            {
                packet.ReadInt32("Unk4", i);
                packet.ReadInt32("Unk5", i);
            }

            for (var i = 0; i < count; i++)
                packet.ReadInt32("Unk6", i);

            for (var i = 0; i < count; i++)
                packet.ReadPackedTime("Time", i);
            packet.StoreEndList();
        }

        [Parser(30332)]
        public static void Handle30332(Packet packet)
        {
            packet.ReadInt64("Unk"); // Not guid
        }

        [Parser(13438)]
        public static void Handle13438(Packet packet)
        {
            packet.ReadInt64("Unk1");
            packet.ReadInt64("Unk2");
            packet.ReadInt64("Unk3");
            packet.ReadInt64("Unk4");
            packet.ReadInt64("Unk5");
        }

        [Parser(13004)]
        public static void Handle13004(Packet packet)
        {
            packet.StoreBeginList("UnkList");
            for (var i = 0; i < 5; i++)
            {
                packet.StoreBeginList("UnkList");
                for (var j = 0; j < 4; j++)
                    packet.ReadInt32("Unk", i, j);
                packet.StoreEndList();
            }
            packet.StoreEndList();
        }

        [Parser(13516)]
        public static void Handle13516(Packet packet)
        {
            packet.ReadByte("Unk1");
            packet.ReadInt32("Unk2");
            packet.ReadSingle("Unk3");
            packet.ReadInt32("Unk4");
        }

        [Parser(44964)] // 4.0.6a
        public static void Handle44964(Packet packet)
        {
            packet.Store("HexDump", packet.ToHex());
        }

        [Parser(Opcode.TEST_422_9838)]
        public static void Handle9838(Packet packet)
        {
            // sub_6C2FD0

            packet.ReadInt32("Unknown 01"); // v3 + 40
            packet.ReadInt32("Unknown 02"); // v3 + 36
            packet.ReadInt32("Unknown 03"); // v3 + 68
            packet.ReadInt32("Unknown 04"); // v3 + 72
            packet.ReadInt32("Unknown 05"); // v3 + 44
            packet.ReadInt32("Unknown 06"); // v3 + 60
            packet.ReadInt32("Unknown 07"); // v3 + 52
            packet.ReadInt32("Unknown 08"); // v3 + 24
            packet.ReadInt32("Unknown 09"); // v3 + 48
            packet.ReadInt32("Unknown 10"); // v3 + 76
            packet.ReadInt32("Unknown 11"); // v3 + 64
            packet.ReadInt32("Unknown 12"); // v3 + 56
            packet.ReadInt32("Unknown 13"); // v3 + 20
            packet.ReadInt32("Unknown 14"); // v3 + 32
            packet.ReadInt32("Unknown 15"); // v3 + 16
            packet.ReadInt32("Unknown 16"); // v3 + 84
            packet.ReadInt32("Unknown 17"); // v3 + 80
            packet.ReadInt32("Unknown 18"); // v3 + 28
        }

        [Parser(Opcode.TEST_422_13022, ClientVersionBuild.V4_2_2_14545)]
        public static void Handle13022(Packet packet)
        {
            var guid = packet.StartBitStream(3, 7, 6, 2, 5, 4, 0, 1);

            packet.ParseBitStream(guid, 4, 1, 5, 2);

            packet.ReadInt32("Unk Int32");

            packet.ParseBitStream(guid, 0, 3, 7, 6);

            packet.ToGuid("Unk Guid?", guid);
        }
    }
}
