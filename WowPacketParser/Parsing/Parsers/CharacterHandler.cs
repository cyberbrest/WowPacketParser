using System;
using WowPacketParser.Enums;
using WowPacketParser.Enums.Version;
using WowPacketParser.Misc;
using WowPacketParser.Store.Objects;
using WowPacketParser.Store;
using Guid=WowPacketParser.Misc.Guid;

namespace WowPacketParser.Parsing.Parsers
{
    public static class CharacterHandler
    {
        [Parser(Opcode.CMSG_STANDSTATECHANGE)]
        public static void HandleStandStateChange(Packet packet)
        {
            packet.ReadInt32("Standstate");
        }

        [Parser(Opcode.SMSG_STANDSTATE_UPDATE)]
        public static void HandleStandStateUpdate(Packet packet)
        {
            packet.ReadByte("Standstate");
        }

        [Parser(Opcode.CMSG_CHAR_CREATE)]
        public static void HandleClientCharCreate(Packet packet)
        {
            packet.ReadCString("Name");
            packet.ReadEnum<Race>("Race", TypeCode.Byte);
            packet.ReadEnum<Class>("Class", TypeCode.Byte);
            packet.ReadEnum<Gender>("Gender", TypeCode.Byte);
            packet.ReadByte("Skin");
            packet.ReadByte("Face");
            packet.ReadByte("Hair Style");
            packet.ReadByte("Hair Color");
            packet.ReadByte("Facial Hair");
            packet.ReadByte("Outfit Id");
        }

        [Parser(Opcode.CMSG_CHAR_DELETE)]
        public static void HandleClientCharDelete(Packet packet)
        {
            packet.ReadGuid("GUID");
        }

        [Parser(Opcode.CMSG_CHAR_RENAME)]
        public static void HandleClientCharRename(Packet packet)
        {
            packet.ReadGuid("GUID");
            packet.ReadCString("New Name");
        }

        [Parser(Opcode.SMSG_CHAR_RENAME)]
        public static void HandleServerCharRename(Packet packet)
        {
            if (packet.ReadEnum<ResponseCode>("Race", TypeCode.Byte) != ResponseCode.RESPONSE_SUCCESS)
                return;

            var guid = packet.ReadGuid("GUID");
            var name = packet.ReadCString("Name");
            StoreGetters.AddName(guid, name);
        }

        [Parser(Opcode.SMSG_CHAR_CREATE)]
        [Parser(Opcode.SMSG_CHAR_DELETE)]
        public static void HandleCharResponse(Packet packet)
        {
            packet.ReadEnum<ResponseCode>("Response", TypeCode.Byte);
        }

        [Parser(Opcode.CMSG_ALTER_APPEARANCE)]
        public static void HandleAlterAppearance(Packet packet)
        {
            // In some ancient version, this could be ReadByte
            packet.ReadInt32("Hair Style");
            packet.ReadInt32("Hair Color");
            packet.ReadInt32("Facial Hair");
            packet.ReadInt32("Skin Color");
        }

        [Parser(Opcode.SMSG_BARBER_SHOP_RESULT)]
        public static void HandleBarberShopResult(Packet packet)
        {
            packet.ReadEnum<BarberShopResult>("Result", TypeCode.Int32);
        }

        [Parser(Opcode.CMSG_CHAR_CUSTOMIZE)]
        public static void HandleClientCharCustomize(Packet packet)
        {
            packet.ReadGuid("GUID");
            packet.ReadCString("New Name");
            packet.ReadEnum<Gender>("Gender", TypeCode.Byte);
            packet.ReadByte("Skin");
            packet.ReadByte("Face");
            packet.ReadByte("Hair Style");
            packet.ReadByte("Hair Color");
            packet.ReadByte("Facial Hair");
        }

        [Parser(Opcode.SMSG_CHAR_CUSTOMIZE)]
        public static void HandleServerCharCustomize(Packet packet)
        {
            if (packet.ReadEnum<ResponseCode>("Response", TypeCode.Byte) != ResponseCode.RESPONSE_SUCCESS)
                return;

            var guid = packet.ReadGuid("GUID");
            var name = packet.ReadCString("Name");

            StoreGetters.AddName(guid, name);

            packet.ReadEnum<Gender>("Gender", TypeCode.Byte);
            packet.ReadByte("Skin");
            packet.ReadByte("Face");
            packet.ReadByte("Hair Style");
            packet.ReadByte("Hair Color");
            packet.ReadByte("Facial Hair");
        }

        [Parser(Opcode.SMSG_CHAR_ENUM, ClientVersionBuild.Zero, ClientVersionBuild.V4_2_2_14545)]
        public static void HandleCharEnum(Packet packet)
        {
            var count = packet.ReadByte("Count");
            packet.StoreBeginList("Characters");
            for (var i = 0; i < count; i++)
            {
                var guid = packet.ReadGuid("GUID", i);
                var name = packet.ReadCString("Name", i);
                var race = packet.ReadEnum<Race>("Race", TypeCode.Byte, i);
                StoreGetters.AddName(guid, name);
                var clss = packet.ReadEnum<Class>("Class", TypeCode.Byte, i);
                packet.ReadEnum<Gender>("Gender", TypeCode.Byte, i);

                packet.ReadByte("Skin", i);
                packet.ReadByte("Face", i);
                packet.ReadByte("Hair Style", i);
                packet.ReadByte("Hair Color", i);
                packet.ReadByte("Facial Hair", i);

                var level = packet.ReadByte("Level", i);
                var zone = packet.ReadEntryWithName<UInt32>(StoreNameType.Zone, "Zone Id", i);
                var mapId = packet.ReadEntryWithName<Int32>(StoreNameType.Map, "Map Id", i);

                var pos = packet.ReadVector3("Position", i);
                if (ClientVersion.AddedInVersion(ClientVersionBuild.V4_0_3_13329))
                    packet.ReadGuid("Guild GUID", i);
                else
                    packet.ReadInt32("Guild Id", i);

                if (ClientVersion.AddedInVersion(ClientVersionBuild.V3_0_2_9056))
                    packet.ReadEnum<CustomizationFlag>("Customization Flags", TypeCode.Int32, i);

                var firstLogin = packet.ReadBoolean("First Login", i);
                packet.ReadInt32("Pet Display Id", i);
                packet.ReadInt32("Pet Level", i);
                packet.ReadEnum<CreatureFamily>("Pet Family", TypeCode.Int32, i);

                packet.StoreBeginList("Equipment", i);
                for (var j = 0; j < 19; j++)
                {
                    packet.ReadInt32("Equip Display Id", i, j);
                    packet.ReadEnum<InventoryType>("Equip Inventory Type", TypeCode.Byte, i, j);
                    packet.ReadInt32("Equip Aura Id", i, j);
                }
                packet.StoreEndList();

                int bagCount = ClientVersion.AddedInVersion(ClientVersionBuild.V3_3_3_11685) ? 4 : 1;
                packet.StoreBeginList("Bags", i);
                for (var j = 0; j < bagCount; j++)
                {
                    packet.ReadInt32("Bag Display Id", i, j);
                    packet.ReadEnum<InventoryType>("Bag Inventory Type", TypeCode.Byte, i, j);
                    packet.ReadInt32("Bag Aura Id", i, j);
                }
                packet.StoreEndList();

                if (firstLogin)
                {
                    var startPos = new StartPosition {Map = mapId, Position = pos, Zone = zone};
                    Storage.StartPositions.Add(new Tuple<Race, Class>(race, clss), startPos, packet.TimeSpan);
                }

                var playerInfo = new Player {Race = race, Class = clss, Name = name, FirstLogin = firstLogin, Level = level};

                if (Storage.Objects.ContainsKey(guid))
                    Storage.Objects[guid] = new Tuple<WoWObject, TimeSpan?>(playerInfo, packet.TimeSpan);
                else
                    Storage.Objects.Add(guid, playerInfo, packet.TimeSpan);
            }
            packet.StoreEndList();
        }

        [Parser(Opcode.SMSG_CHAR_ENUM, ClientVersionBuild.V4_2_2_14545, ClientVersionBuild.V4_3_0_15005)]
        public static void HandleCharEnum422(Packet packet)
        {
            packet.ReadByte("Unk Flag");
            int count = packet.ReadInt32("Char Count");
            packet.ReadInt32("Unk Count");

            var bits = new bool[count, 17];

            for (int c = 0; c < count; c++)
                for (int j = 0; j < 17; j++)
                    bits[c, j] = packet.ReadBit();

            packet.StoreBeginList("Characters");
            for (int c = 0; c < count; c++)
            {
                var low = new byte[8];
                var guild = new byte[8];
                var name = packet.ReadCString("Name", c);

                if (bits[c, 0])
                    guild[5] = (byte)(packet.ReadByte() ^ 1);

                packet.ReadByte("Face", c);
                var mapId = packet.ReadInt32("Map", c);

                if (bits[c, 12])
                    low[1] = (byte)(packet.ReadByte() ^ 1);

                if (bits[c, 1])
                    low[4] = (byte)(packet.ReadByte() ^ 1);

                if (bits[c, 10])
                    guild[4] = (byte)(packet.ReadByte() ^ 1);

                if (bits[c, 15])
                    guild[0] = (byte)(packet.ReadByte() ^ 1);

                var pos = packet.ReadVector3("Position", c);

                if (bits[c, 11])
                    low[0] = (byte)(packet.ReadByte() ^ 1);

                var zone = packet.ReadEntryWithName<Int32>(StoreNameType.Zone, "Zone Id", c);
                packet.ReadInt32("Pet Level", c);

                if (bits[c, 8])
                    low[3] = (byte)(packet.ReadByte() ^ 1);

                if (bits[c, 14])
                    low[7] = (byte)(packet.ReadByte() ^ 1);

                packet.ReadByte("Facial Hair", c);
                packet.ReadByte("Skin", c);
                var clss = packet.ReadEnum<Class>("Class", TypeCode.Byte, c);
                packet.ReadInt32("Pet Family", c);
                packet.ReadEnum<CharacterFlag>("CharacterFlag", TypeCode.Int32, c);

                if (bits[c, 9])
                    low[2] = (byte)(packet.ReadByte() ^ 1);

                packet.ReadInt32("Pet Display ID", c);

                if (bits[c, 3])
                    guild[7] = (byte)(packet.ReadByte() ^ 1);

                var level = packet.ReadByte("Level", c);

                if (bits[c, 7])
                    low[6] = (byte)(packet.ReadByte() ^ 1);

                packet.ReadByte("Hair Style", c);

                if (bits[c, 13])
                    guild[2] = (byte)(packet.ReadByte() ^ 1);

                var race = packet.ReadEnum<Race>("Race", TypeCode.Byte, c);
                packet.ReadByte("Hair Color", c);

                if (bits[c, 5])
                    guild[6] = (byte)(packet.ReadByte() ^ 1);

                packet.ReadEnum<Gender>("Gender", TypeCode.Byte, c);

                if (bits[c, 6])
                    low[5] = (byte)(packet.ReadByte() ^ 1);

                if (bits[c, 2])
                    guild[3] = (byte)(packet.ReadByte() ^ 1);

                packet.ReadByte("List Order", c);

                packet.StoreBeginList("Equipment", c);
                for (int itm = 0; itm < 19; itm++)
                {
                    packet.ReadInt32("Item EnchantID", c, itm);
                    packet.ReadEnum<InventoryType>("Item InventoryType", TypeCode.Byte, c, itm);
                    packet.ReadInt32("Item DisplayID", c, itm);
                }
                packet.StoreEndList();

                packet.StoreBeginList("Bags", c);
                for (int itm = 0; itm < 4; itm++)
                {
                    packet.ReadInt32("Bag EnchantID", c, itm);
                    packet.ReadEnum<InventoryType>("Bag InventoryType", TypeCode.Byte, c, itm);
                    packet.ReadInt32("Bag DisplayID", c, itm);
                }
                packet.StoreEndList();

                packet.ReadEnum<CustomizationFlag>("CustomizationFlag", TypeCode.UInt32, c);

                if (bits[c, 4])
                    guild[1] = (byte)(packet.ReadByte() ^ 1);

                var playerGuid = packet.StoreBitstreamGuid("Character GUID", low, c);
                packet.StoreBitstreamGuid("Guild GUID", guild, c);

                var firstLogin = bits[c, 16];
                if (firstLogin)
                {
                    var startPos = new StartPosition {Map = mapId, Position = pos, Zone = zone};

                    Storage.StartPositions.Add(new Tuple<Race, Class>(race, clss), startPos, packet.TimeSpan);
                }

                var playerInfo = new Player { Race = race, Class = clss, Name = name, FirstLogin = firstLogin, Level = level };
                if (Storage.Objects.ContainsKey(playerGuid))
                    Storage.Objects[playerGuid] = new Tuple<WoWObject, TimeSpan?>(playerInfo, packet.TimeSpan);
                else
                    Storage.Objects.Add(playerGuid, playerInfo, packet.TimeSpan);
            }
            packet.StoreEndList();
        }

        [Parser(Opcode.SMSG_CHAR_ENUM, ClientVersionBuild.V4_3_0_15005, ClientVersionBuild.V4_3_3_15354)]
        public static void HandleCharEnum430(Packet packet)
        {
            var count = packet.ReadBits("Char count", 17);

            var charGuids = new byte[count][];
            var guildGuids = new byte[count][];
            var firstLogins = new bool[count];
            var nameLenghts = new uint[count];

            for (var c = 0; c < count; ++c)
            {
                charGuids[c] = new byte[8];
                guildGuids[c] = new byte[8];

                guildGuids[c][2] = (byte)(packet.ReadBit() ? 1 : 0);
                charGuids[c][2] = (byte)(packet.ReadBit() ? 1 : 0);
                charGuids[c][6] = (byte)(packet.ReadBit() ? 1 : 0);
                charGuids[c][5] = (byte)(packet.ReadBit() ? 1 : 0);
                charGuids[c][4] = (byte)(packet.ReadBit() ? 1 : 0);
                guildGuids[c][4] = (byte)(packet.ReadBit() ? 1 : 0);
                guildGuids[c][3] = (byte)(packet.ReadBit() ? 1 : 0);
                guildGuids[c][7] = (byte)(packet.ReadBit() ? 1 : 0);
                nameLenghts[c] = packet.ReadBits(7);
                guildGuids[c][0] = (byte)(packet.ReadBit() ? 1 : 0);
                charGuids[c][0] = (byte)(packet.ReadBit() ? 1 : 0);
                charGuids[c][3] = (byte)(packet.ReadBit() ? 1 : 0);
                charGuids[c][1] = (byte)(packet.ReadBit() ? 1 : 0);
                firstLogins[c] = packet.ReadBit();
                guildGuids[c][5] = (byte)(packet.ReadBit() ? 1 : 0);
                charGuids[c][7] = (byte)(packet.ReadBit() ? 1 : 0);
                guildGuids[c][6] = (byte)(packet.ReadBit() ? 1 : 0);
                guildGuids[c][1] = (byte)(packet.ReadBit() ? 1 : 0);
            }

            var unkCounter = packet.ReadBits("Unk Counter", 23);
            packet.ReadBit(); // no idea, not used in client

            packet.StoreBeginList("Characters");
            for (int c = 0; c < count; ++c)
            {
                packet.StoreBeginList("Equipment", c);
                for (var itm = 0; itm < 19; ++itm)
                {
                    packet.ReadEnum<InventoryType>("Item InventoryType", TypeCode.Byte, c, itm);
                    packet.ReadInt32("Item DisplayID", c, itm);
                    packet.ReadInt32("Item EnchantID", c, itm);
                }
                packet.StoreEndList();

                packet.StoreBeginList("Bags", c);
                for (var itm = 0; itm < 4; ++itm)
                {
                    packet.ReadEnum<InventoryType>("Bag InventoryType", TypeCode.Byte, c, itm);
                    packet.ReadInt32("Bag DisplayID", c, itm);
                    packet.ReadInt32("Bag EnchantID", c, itm);
                }
                packet.StoreEndList();

                if (guildGuids[c][0] != 0)
                    guildGuids[c][0] ^= packet.ReadByte();
                if (guildGuids[c][1] != 0)
                    guildGuids[c][1] ^= packet.ReadByte();

                packet.ReadByte("Face", c);
                packet.ReadInt32("Pet Display ID", c);
                if (guildGuids[c][7] != 0)
                    guildGuids[c][7] ^= packet.ReadByte();

                packet.ReadEnum<Gender>("Gender", TypeCode.Byte, c);
                var level = packet.ReadByte("Level", c);
                packet.ReadInt32("Pet Level", c);
                var zone = packet.ReadEntryWithName<UInt32>(StoreNameType.Zone, "Zone Id", c);
                var y = packet.ReadSingle("Position Y", c);
                packet.ReadInt32("Pet Family", c);
                packet.ReadByte("Hair Style", c);
                if (charGuids[c][1] != 0)
                    charGuids[c][1] ^= packet.ReadByte();

                var name = packet.ReadWoWString("Name", (int)nameLenghts[c], c);
                if (charGuids[c][0] != 0)
                    charGuids[c][0] ^= packet.ReadByte();

                var race = packet.ReadEnum<Race>("Race", TypeCode.Byte, c);
                packet.ReadByte("List Order", c);
                if (charGuids[c][7] != 0)
                    charGuids[c][7] ^= packet.ReadByte();

                var z = packet.ReadSingle("Position Z", c);
                var mapId = packet.ReadInt32("Map", c);
                if (guildGuids[c][4] != 0)
                    guildGuids[c][4] ^= packet.ReadByte();

                packet.ReadByte("Hair Color", c);
                if (charGuids[c][3] != 0)
                    charGuids[c][3] ^= packet.ReadByte();

                packet.ReadEnum<CharacterFlag>("CharacterFlag", TypeCode.Int32, c);
                packet.ReadByte("Skin", c);
                if (charGuids[c][4] != 0)
                    charGuids[c][4] ^= packet.ReadByte();
                if (charGuids[c][5] != 0)
                    charGuids[c][5] ^= packet.ReadByte();
                if (guildGuids[c][5] != 0)
                    guildGuids[c][5] ^= packet.ReadByte();

                packet.ReadEnum<CustomizationFlag>("CustomizationFlag", TypeCode.UInt32, c);
                var x = packet.ReadSingle("Position X", c);
                packet.ReadByte("Facial Hair", c);
                if (charGuids[c][6] != 0)
                    charGuids[c][6] ^= packet.ReadByte();
                if (guildGuids[c][3] != 0)
                    guildGuids[c][3] ^= packet.ReadByte();
                if (charGuids[c][2] != 0)
                    charGuids[c][2] ^= packet.ReadByte();

                var clss = packet.ReadEnum<Class>("Class", TypeCode.Byte, c);
                if (guildGuids[c][6] != 0)
                    guildGuids[c][6] ^= packet.ReadByte();
                if (guildGuids[c][2] != 0)
                    guildGuids[c][2] ^= packet.ReadByte();

                var playerGuid = packet.StoreBitstreamGuid("Character GUID", charGuids[c], c);
                packet.StoreBitstreamGuid("Guild GUID", guildGuids[c], c);

                if (firstLogins[c])
                {
                    var startPos = new StartPosition { Map = mapId, Position = new Vector3(x, y, z), Zone = zone };

                    Storage.StartPositions.Add(new Tuple<Race, Class>(race, clss), startPos, packet.TimeSpan);
                }

                var playerInfo = new Player{Race = race, Class = clss, Name = name, FirstLogin = firstLogins[c], Level = level};
                if (Storage.Objects.ContainsKey(playerGuid))
                    Storage.Objects[playerGuid] = new Tuple<WoWObject, TimeSpan?>(playerInfo, packet.TimeSpan);
                else
                    Storage.Objects.Add(playerGuid, playerInfo, packet.TimeSpan);
            }

            packet.StoreBeginList("Unk Datas");
            for (var c = 0; c < unkCounter; c++)
            {
                packet.ReadUInt32("Unk UInt32", c);
                packet.ReadByte("Unk Byte", c);
            }
            packet.StoreEndList();
        }

        [Parser(Opcode.SMSG_CHAR_ENUM, ClientVersionBuild.V4_3_3_15354, ClientVersionBuild.V4_3_4_15595)]
        public static void HandleCharEnum433(Packet packet)
        {
            var unkCounter = packet.ReadBits("Unk Counter", 23);
            var count = packet.ReadBits("Char count", 17);

            var charGuids = new byte[count][];
            var guildGuids = new byte[count][];
            var firstLogins = new bool[count];
            var nameLenghts = new uint[count];

            for (var c = 0; c < count; ++c)
            {
                charGuids[c] = new byte[8];
                guildGuids[c] = new byte[8];
                //100%  pozition, and flag
                //%50   flag 
                //20    nothing

                charGuids[c][0] = (byte)(packet.ReadBit() ? 1 : 0); //100%
                guildGuids[c][0] = (byte)(packet.ReadBit() ? 1 : 0);//50%
                charGuids[c][2] = (byte)(packet.ReadBit() ? 1 : 0); //100%
                guildGuids[c][2] = (byte)(packet.ReadBit() ? 1 : 0);//50%
                firstLogins[c] = packet.ReadBit();                  //100%
                charGuids[c][3] = (byte)(packet.ReadBit() ? 1 : 0); //100%
                charGuids[c][6] = (byte)(packet.ReadBit() ? 1 : 0); //100%
                guildGuids[c][2] = (byte)(packet.ReadBit() ? 1 : 0);//20%

                charGuids[c][4] = (byte)(packet.ReadBit() ? 1 : 0); //20%
                charGuids[c][5] = (byte)(packet.ReadBit() ? 1 : 0); //20%
                nameLenghts[c] = packet.ReadBits(4);                //100%
                guildGuids[c][3] = (byte)(packet.ReadBit() ? 1 : 0);//20%
                guildGuids[c][4] = (byte)(packet.ReadBit() ? 1 : 0);//50%

                guildGuids[c][5] = (byte)(packet.ReadBit() ? 1 : 0);//20%
                charGuids[c][1] = (byte)(packet.ReadBit() ? 1 : 0); //100%
                packet.ReadBit();                                   //20%
                guildGuids[c][6] = (byte)(packet.ReadBit() ? 1 : 0);//20%
                charGuids[c][7] = (byte)(packet.ReadBit() ? 1 : 0); //100%
                guildGuids[c][7] = (byte)(packet.ReadBit() ? 1 : 0);//50%
                packet.ReadBit();                                   //20%
                packet.ReadBit();                                   //20%
            }

            // no idea, not used in client
            packet.ReadByte();

            packet.StoreBeginList("Characters");
            for (int c = 0; c < count; ++c)
            {
                packet.StoreBeginList("Equipment", c);
                for (var itm = 0; itm < 19; ++itm)
                {


                    packet.ReadInt32("Item EnchantID", c, itm);
                    packet.ReadInt32("Item DisplayID", c, itm);
                    packet.ReadEnum<InventoryType>("Item InventoryType", TypeCode.Byte, c, itm);
                }
                packet.StoreEndList();

                packet.StoreBeginList("Bags", c);
                for (var itm = 0; itm < 4; ++itm)
                {

                    packet.ReadInt32("Bag EnchantID", c, itm);
                    packet.ReadInt32("Bag DisplayID", c, itm);
                    packet.ReadEnum<InventoryType>("Bag InventoryType", TypeCode.Byte, c, itm);
                }
                packet.StoreEndList();

                var zone = packet.ReadEntryWithName<UInt32>(StoreNameType.Zone, "Zone Id", c);
                packet.ReadInt32("Pet Level", c);
                packet.ReadEnum<CharacterFlag>("CharacterFlag", TypeCode.Int32, c);

                packet.ReadByte("Facial Hair", c);

                if (guildGuids[c][0] != 0)
                    // guildGuids[c][0] ^= packet.ReadByte();
                    if (charGuids[c][0] != 0)
                        charGuids[c][0] ^= packet.ReadByte();

                if (charGuids[c][2] != 0)
                    charGuids[c][2] ^= packet.ReadByte();
                if (guildGuids[c][2] != 0)
                    //  guildGuids[c][2] ^= packet.ReadByte();

                    if (charGuids[c][7] != 0)
                        charGuids[c][7] ^= packet.ReadByte();
                if (guildGuids[c][7] != 0)
                    // guildGuids[c][7] ^= packet.ReadByte();

                    packet.ReadByte("List Order", c);
                packet.ReadInt32("Pet Display ID", c);

                // no ideal //////////////////////////////
                if (charGuids[c][4] != 0)
                    charGuids[c][4] ^= packet.ReadByte();

                if (guildGuids[c][4] != 0)
                    // guildGuids[c][4] ^= packet.ReadByte();

                if (charGuids[c][5] != 0)
                        // charGuids[c][5] ^= packet.ReadByte();

                if (guildGuids[c][5] != 0)
                            // guildGuids[c][5] ^= packet.ReadByte();

                if (guildGuids[c][1] != 0)
                                // guildGuids[c][1] ^= packet.ReadByte();

                                if (guildGuids[c][3] != 0)
                                    // guildGuids[c][3] ^= packet.ReadByte();

                                    if (guildGuids[c][6] != 0)
                                        // guildGuids[c][6] ^= packet.ReadByte();

                                        //////////////////////////////////////////

                                        if (charGuids[c][3] != 0)
                                            charGuids[c][3] ^= packet.ReadByte();

                var clss = packet.ReadEnum<Class>("Class", TypeCode.Byte, c);

                if (charGuids[c][6] != 0)
                    charGuids[c][6] ^= packet.ReadByte();

                var x = packet.ReadSingle("Position X", c);

                if (charGuids[c][1] != 0)
                    charGuids[c][1] ^= packet.ReadByte();

                var race = packet.ReadEnum<Race>("Race", TypeCode.Byte, c);
                packet.ReadInt32("Pet Family", c);
                var y = packet.ReadSingle("Position Y", c);
                packet.ReadEnum<Gender>("Gender", TypeCode.Byte, c);
                packet.ReadByte("Hair Style", c);
                var level = packet.ReadByte("Level", c);
                var z = packet.ReadSingle("Position Z", c);
                packet.ReadEnum<CustomizationFlag>("CustomizationFlag", TypeCode.UInt32, c);
                packet.ReadByte("Skin", c);
                packet.ReadByte("Hair Color", c);
                packet.ReadByte("Face", c);
                var mapId = packet.ReadInt32("Map", c);
                var name = packet.ReadWoWString("Name", (int)nameLenghts[c], c);

                var playerGuid = packet.StoreBitstreamGuid("Character GUID", charGuids[c], c);
                packet.StoreBitstreamGuid("Guild GUID", guildGuids[c], c);

                if (firstLogins[c])
                {
                    var startPos = new StartPosition();
                    startPos.Map = mapId;
                    startPos.Position = new Vector3(x, y, z);
                    startPos.Zone = zone;

                    Storage.StartPositions.Add(new Tuple<Race, Class>(race, clss), startPos, packet.TimeSpan);
                }

                var playerInfo = new Player { Race = race, Class = clss, Name = name, FirstLogin = firstLogins[c], Level = level };
                if (Storage.Objects.ContainsKey(playerGuid))
                    Storage.Objects[playerGuid] = new Tuple<WoWObject, TimeSpan?>(playerInfo, packet.TimeSpan);
                else
                    Storage.Objects.Add(playerGuid, playerInfo, packet.TimeSpan);
            }

            packet.StoreBeginList("Unk Datas");
            for (var c = 0; c < unkCounter; c++)
            {
                packet.ReadUInt32("Unk UInt32", c);
                packet.ReadByte("Unk Byte", c);
            }
            packet.StoreEndList();
        }
        
        [Parser(Opcode.SMSG_CHAR_ENUM, ClientVersionBuild.V4_3_4_15595)]
        public static void HandleCharEnum434(Packet packet)
        {
            //var unkCounter = packet.ReadBits("Unk Counter", 23);
            var unkCounter = packet.ReadByte();
            packet.ReadByte();
            packet.ReadByte();
            
            var count = packet.ReadBits("Char count", 17);

            var charGuids = new byte[count][];
            var guildGuids = new byte[count][];
            var firstLogins = new bool[count];
            var nameLenghts = new uint[count];

            for (var c = 0; c < count; ++c)
            {
                charGuids[c] = new byte[8];
                guildGuids[c] = new byte[8];
                /*
                guildGuids[c][2] = (byte)(packet.ReadBit() ? 1 : 0);
                charGuids[c][2] = (byte)(packet.ReadBit() ? 1 : 0);
                charGuids[c][6] = (byte)(packet.ReadBit() ? 1 : 0);
                charGuids[c][5] = (byte)(packet.ReadBit() ? 1 : 0);
                charGuids[c][4] = (byte)(packet.ReadBit() ? 1 : 0);
                guildGuids[c][4] = (byte)(packet.ReadBit() ? 1 : 0);
                guildGuids[c][3] = (byte)(packet.ReadBit() ? 1 : 0);
                guildGuids[c][7] = (byte)(packet.ReadBit() ? 1 : 0);
                nameLenghts[c] = packet.ReadBits(7);
                guildGuids[c][0] = (byte)(packet.ReadBit() ? 1 : 0);
                charGuids[c][0] = (byte)(packet.ReadBit() ? 1 : 0);
                charGuids[c][3] = (byte)(packet.ReadBit() ? 1 : 0);
                charGuids[c][1] = (byte)(packet.ReadBit() ? 1 : 0);
                firstLogins[c] = packet.ReadBit();
                guildGuids[c][5] = (byte)(packet.ReadBit() ? 1 : 0);
                charGuids[c][7] = (byte)(packet.ReadBit() ? 1 : 0);
                guildGuids[c][6] = (byte)(packet.ReadBit() ? 1 : 0);
                guildGuids[c][1] = (byte)(packet.ReadBit() ? 1 : 0);*/


                charGuids[c][7] = (byte)(packet.ReadBit() ? 1 : 0);
                guildGuids[c][4] = (byte)(packet.ReadBit() ? 1 : 0);
                guildGuids[c][3] = (byte)(packet.ReadBit() ? 1 : 0);
                guildGuids[c][7] = (byte)(packet.ReadBit() ? 1 : 0);
                nameLenghts[c] = packet.ReadBits(7);

                charGuids[c][4] = (byte)(packet.ReadBit() ? 1 : 0);
                charGuids[c][3] = (byte)(packet.ReadBit() ? 1 : 0);
                guildGuids[c][5] = (byte)(packet.ReadBit() ? 1 : 0);
                charGuids[c][5] = (byte)(packet.ReadBit() ? 1 : 0);
                guildGuids[c][5] = (byte)(packet.ReadBit() ? 1 : 0);
                charGuids[c][1] = (byte)(packet.ReadBit() ? 1 : 0);
                guildGuids[c][5] = (byte)(packet.ReadBit() ? 1 : 0);
                guildGuids[c][5] = (byte)(packet.ReadBit() ? 1 : 0);

                firstLogins[c] = packet.ReadBit();
                charGuids[c][0] = (byte)(packet.ReadBit() ? 1 : 0);
                charGuids[c][2] = (byte)(packet.ReadBit() ? 1 : 0);
                charGuids[c][6] = (byte)(packet.ReadBit() ? 1 : 0);
                
                guildGuids[c][7] = (byte)(packet.ReadBit() ? 1 : 0);
                packet.ReadBit();
                packet.ReadBit();
                packet.ReadBit();
                packet.ReadBit();
                packet.ReadBit();
                packet.ReadBit();
                packet.ReadBit();

            }

           
          //packet.ReadBit(); // no idea, not used in client


            if (count > 0)
            {
                packet.StoreBeginList("Characters");
                for (int c = 0; c < count; ++c)
                {
                    var clss = packet.ReadEnum<Class>("Class", TypeCode.Byte, c);
                    
                    packet.StoreBeginList("Equipment", c);
                    for (var itm = 0; itm < 19; ++itm)
                    {
                        packet.ReadEnum<InventoryType>("Item InventoryType", TypeCode.Byte, c, itm);
                        packet.ReadInt32("Item DisplayID", c, itm);
                        packet.ReadInt32("Item EnchantID", c, itm);
                    }
                    packet.StoreEndList();

                    packet.StoreBeginList("Bags", c);
                    for (var itm = 0; itm < 4; ++itm)
                    {
                        packet.ReadEnum<InventoryType>("Bag InventoryType", TypeCode.Byte, c, itm);
                        packet.ReadInt32("Bag DisplayID", c, itm);
                        packet.ReadInt32("Bag EnchantID", c, itm);
                    }
                    packet.StoreEndList();

                    packet.ReadInt32("Pet Family", c);
                    packet.ReadByte("List Order", c);
                    packet.ReadByte("Hair Style", c);
                    packet.ReadInt32("Pet Display ID", c);
                    packet.ReadEnum<CharacterFlag>("CharacterFlag", TypeCode.Int32, c);
                    packet.ReadByte("Hair Color", c);
                    var mapId = packet.ReadInt32("Map", c);
                    var z = packet.ReadSingle("Position Z", c);
                    packet.ReadInt32("Pet Level", c);

                    if (charGuids[c][3] != 0)
                        charGuids[c][3] ^= packet.ReadByte();

                    var y = packet.ReadSingle("Position Y", c);

                    packet.ReadEnum<CustomizationFlag>("CustomizationFlag", TypeCode.UInt32, c);
                    packet.ReadByte("Facial Hair", c);

                    if (charGuids[c][7] != 0)
                        charGuids[c][7] ^= packet.ReadByte();

                    packet.ReadEnum<Gender>("Gender", TypeCode.Byte, c);
                    var name = packet.ReadWoWString("Name", (int)nameLenghts[c], c);
                    packet.ReadByte("Face", c);

                    if (charGuids[c][0] != 0)
                        charGuids[c][0] ^= packet.ReadByte();

                    if (charGuids[c][2] != 0)
                        charGuids[c][2] ^= packet.ReadByte();

                    var x = packet.ReadSingle("Position X", c);
                    packet.ReadByte("Skin", c);
                    var race = packet.ReadEnum<Race>("Race", TypeCode.Byte, c);
                    var level = packet.ReadByte("Level", c);
                    if (charGuids[c][1] != 0)
                        charGuids[c][1] ^= packet.ReadByte();
                    var zone = packet.ReadEntryWithName<UInt32>(StoreNameType.Zone, "Zone Id", c);

                    // Not ideal
                    if (guildGuids[c][0] != 0)
                        guildGuids[c][0] ^= packet.ReadByte();
                    if (guildGuids[c][1] != 0)
                        guildGuids[c][1] ^= packet.ReadByte();

                    if (guildGuids[c][7] != 0)
                        guildGuids[c][7] ^= packet.ReadByte();

                    if (guildGuids[c][4] != 0)
                        guildGuids[c][4] ^= packet.ReadByte();


                    if (charGuids[c][4] != 0)
                        charGuids[c][4] ^= packet.ReadByte();
                    if (charGuids[c][5] != 0)
                        charGuids[c][5] ^= packet.ReadByte();
                    if (guildGuids[c][5] != 0)
                        guildGuids[c][5] ^= packet.ReadByte();


                    if (charGuids[c][6] != 0)
                        charGuids[c][6] ^= packet.ReadByte();
                    if (guildGuids[c][3] != 0)
                        guildGuids[c][3] ^= packet.ReadByte();


                    if (guildGuids[c][6] != 0)
                        guildGuids[c][6] ^= packet.ReadByte();
                    if (guildGuids[c][2] != 0)
                        guildGuids[c][2] ^= packet.ReadByte();

                    var playerGuid = packet.StoreBitstreamGuid("Character GUID", charGuids[c], c);
                    packet.StoreBitstreamGuid("Guild GUID", guildGuids[c], c);

                    if (firstLogins[c])
                    {
                        var startPos = new StartPosition();
                        startPos.Map = mapId;
                        startPos.Position = new Vector3(x, y, z);
                        startPos.Zone = zone;

                        Storage.StartPositions.Add(new Tuple<Race, Class>(race, clss), startPos, packet.TimeSpan);
                    }

                    var playerInfo = new Player { Race = race, Class = clss, Name = name, FirstLogin = firstLogins[c], Level = level };
                    if (Storage.Objects.ContainsKey(playerGuid))
                        Storage.Objects[playerGuid] = new Tuple<WoWObject, TimeSpan?>(playerInfo, packet.TimeSpan);
                    else
                        Storage.Objects.Add(playerGuid, playerInfo, packet.TimeSpan);
                }
                packet.StoreEndList();

                packet.StoreBeginList("Unk Datas");
                for (var c = 0; c < unkCounter; c++)
                {
                    packet.ReadUInt32("Unk UInt32", c);
                    packet.ReadByte("Unk Byte", c);
                }
                packet.StoreEndList();
            }
        }
        
        [Parser(Opcode.SMSG_COMPRESSED_CHAR_ENUM)]
        public static void HandleCompressedCharEnum(Packet packet)
        {
            packet.Inflate(packet.ReadInt32());
            {
                if (ClientVersion.AddedInVersion(ClientVersionBuild.V4_3_4_15595))
                    HandleCharEnum434(packet);
                else if (ClientVersion.AddedInVersion(ClientVersionBuild.V4_3_3_15354))
                    HandleCharEnum433(packet);
                else if (ClientVersion.AddedInVersion(ClientVersionBuild.V4_3_0_15005))
                    HandleCharEnum430(packet);
                else
                    HandleCharEnum422(packet);
            }
        }

        [Parser(Opcode.SMSG_PLAYER_VEHICLE_DATA)]
        public static void HandlePlayerVehicleData(Packet packet)
        {
            packet.ReadPackedGuid("GUID");
            packet.ReadInt32("Vehicle Id");
        }

        [Parser(Opcode.CMSG_PLAYED_TIME)]
        [Parser(Opcode.SMSG_PLAYED_TIME)]
        public static void HandlePlayedTime(Packet packet)
        {
            if (packet.Opcode == Opcodes.GetOpcode(Opcode.SMSG_PLAYED_TIME))
            {
                packet.ReadInt32("Time Played");
                packet.ReadInt32("Total");
            }
            packet.ReadBoolean("Print in chat");
        }

        [Parser(Opcode.SMSG_LOG_XPGAIN)]
        public static void HandleLogXPGain(Packet packet)
        {
            packet.ReadGuid("GUID");
            packet.ReadUInt32("Total XP");
            var type = packet.ReadByte("XP type"); // Need enum

            if (type == 0) // kill
            {
                packet.ReadUInt32("Base XP");
                packet.ReadSingle("Group rate (unk)");
            }

            packet.ReadBoolean("RAF Bonus");
        }

        [Parser(Opcode.SMSG_TITLE_EARNED)]
        public static void HandleTitleEarned(Packet packet)
        {
            packet.ReadUInt32("Title Id");
            packet.ReadUInt32("Earned?"); // vs lost
        }

        [Parser(Opcode.CMSG_SET_TITLE)]
        public static void HandleSetTitle(Packet packet)
        {
            packet.ReadUInt32("Title Id");
        }

        [Parser(Opcode.SMSG_INIT_CURRENCY, ClientVersionBuild.V4_2_2_14545)]
        public static void HandleInitCurrency422(Packet packet)
        {
            var count = packet.ReadUInt32("Count");
            var bits = new bool[count, 3];

            for (var i = 0; i < count; ++i)
                for (var j = 0; j < 3; ++j)
                    bits[i, j] = packet.ReadBit();

            packet.StoreBeginList("CurrencyDatas");
            for (var i = 0; i < count; ++i)
            {
                packet.ReadInt32("Currency Id", i);
                if (bits[i, 0])
                    packet.ReadInt32("Weekly Cap", i);

                packet.ReadInt32("Total Count", i);
                packet.ReadByte("Unk Byte1", i);

                if (bits[i, 1])
                    packet.ReadInt32("Season Total Earned?", i);

                if (bits[i, 2])
                    packet.ReadUInt32("Week Count", i);
            }
            packet.StoreEndList();
        }

        [Parser(Opcode.SMSG_INIT_CURRENCY, ClientVersionBuild.Zero, ClientVersionBuild.V4_2_2_14545)]
        public static void HandleInitCurrency(Packet packet)
        {
            var count = packet.ReadUInt32("Count");
            packet.StoreBeginList("CurrencyDatas");
            for (var i = 0; i < count; ++i)
            {
                packet.ReadUInt32("Week Count", i);
                packet.ReadByte("Unk Byte", i);
                packet.ReadUInt32("Currency ID", i);
                packet.ReadTime("Reset Time", i);
                packet.ReadUInt32("Week Cap", i);
                packet.ReadInt32("Total Count", i);
            }
            packet.StoreEndList();
        }
    }
}
