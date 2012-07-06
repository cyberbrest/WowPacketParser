using System;
using PacketParser.Enums;
using PacketParser.Misc;
using PacketParser.Processing;
using PacketParser.DataStructures;

namespace PacketParser.Parsing.Parsers
{
    public static class GameObjectHandler
    {
        [Parser(Opcode.CMSG_GAMEOBJECT_QUERY)]
        public static void HandleGameObjectQuery(Packet packet)
        {
            QueryHandler.ReadQueryHeader(ref packet);
        }

        [Parser(Opcode.SMSG_GAMEOBJECT_QUERY_RESPONSE)]
        public static void HandleGameObjectQueryResponse(Packet packet)
        {
            var gameObject = new GameObjectTemplate();
            var entry = packet.ReadEntry("Entry");

            if (entry.Value) // entry is masked
                return;

            gameObject.Type = packet.ReadEnum<GameObjectType>("Type", TypeCode.Int32);
            gameObject.DisplayId = packet.ReadUInt32("Display ID");

            var name = new string[4];
            packet.StoreBeginList("Names");
            for (var i = 0; i < 4; i++)
                name[i] = packet.ReadCString("Name", i);
            packet.StoreEndList();
            gameObject.Name = name[0];

            gameObject.IconName = packet.ReadCString("Icon Name");
            gameObject.CastCaption = packet.ReadCString("Cast Caption");
            gameObject.UnkString = packet.ReadCString("Unk String");

            gameObject.Data = new int[ClientVersion.AddedInVersion(ClientVersionBuild.V4_0_6_13596) ? 32 : 24];
            packet.StoreBeginList("Data");
            for (var i = 0; i < gameObject.Data.Length; i++)
                gameObject.Data[i] = packet.ReadInt32("Data", i);
            packet.StoreEndList();

            if (ClientVersion.AddedInVersion(ClientVersionBuild.V3_0_2_9056)) // not sure when it was added exactly - did not exist in 2.4.1 sniff
                gameObject.Size = packet.ReadSingle("Size");

            gameObject.QuestItems = new uint[ClientVersion.AddedInVersion(ClientVersionBuild.V3_2_0_10192) ? 6 : 4];
            packet.StoreBeginList("Quest Items");
            if (ClientVersion.AddedInVersion(ClientVersionBuild.V3_1_0_9767))
                for (var i = 0; i < gameObject.QuestItems.Length; i++)
                    gameObject.QuestItems[i] = (uint)packet.ReadEntryWithName<Int32>(StoreNameType.Item, "Quest Item", i);
            packet.StoreEndList();

            if (ClientVersion.AddedInVersion(ClientVersionBuild.V4_0_6_13596))
                gameObject.UnknownUInt = packet.ReadUInt32("Unknown UInt32");

            packet.Store("GameObjectTemplateObject", gameObject);

            PacketFileProcessor.Current.GetProcessor<NameStore>().AddName(StoreNameType.GameObject, entry.Key, gameObject.Name, packet.TimeSpan);
        }

        [Parser(Opcode.SMSG_DESTRUCTIBLE_BUILDING_DAMAGE)]
        public static void HandleDestructibleBuildingDamage(Packet packet)
        {
            packet.ReadPackedGuid("GO GUID");
            packet.ReadPackedGuid("Vehicle GUID:");
            packet.ReadPackedGuid("Player GUID");
            packet.ReadInt32("Damage");
            packet.ReadEntryWithName<Int32>(StoreNameType.Spell, "Spell ID");
        }

        [Parser(Opcode.SMSG_GAMEOBJECT_DESPAWN_ANIM)]
        [Parser(Opcode.CMSG_GAMEOBJ_USE)]
        [Parser(Opcode.CMSG_GAMEOBJ_REPORT_USE)]
        [Parser(Opcode.SMSG_GAMEOBJECT_PAGETEXT)]
        [Parser(Opcode.SMSG_GAMEOBJECT_RESET_STATE)]
        public static void HandleGOMisc(Packet packet)
        {
            packet.ReadGuid("GUID");
        }

        [Parser(Opcode.SMSG_GAMEOBJECT_CUSTOM_ANIM)]
        public static void HandleGOCustomAnim(Packet packet)
        {
            packet.ReadGuid("GUID");
            packet.ReadInt32("Anim");
        }
    }
}
