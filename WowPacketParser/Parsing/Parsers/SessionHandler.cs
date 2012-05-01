using System;
using System.Text;
using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Store.Objects;
using Guid=WowPacketParser.Misc.Guid;

namespace WowPacketParser.Parsing.Parsers
{
    public static class SessionHandler
    {
        [ThreadStatic]
        public static Guid LoginGuid;

        public static Player LoggedInCharacter;

        [Parser(Opcode.SMSG_AUTH_CHALLENGE)]
        public static void HandleServerAuthChallenge(Packet packet)
        {
            if (ClientVersion.AddedInVersion(ClientVersionBuild.V3_2_0_10192))
                packet.ReadInt32("Shuffle Count");

            packet.ReadInt32("Server Seed");

            if (ClientVersion.AddedInVersion(ClientVersionBuild.V3_2_0_10192))
            {
                packet.StoreBeginList("Server States");
                for (var i = 0; i < 8; i++)
                    packet.ReadInt32("Server State", i);
                packet.StoreEndList();
            }
        }

        [Parser(Opcode.SMSG_AUTH_CHALLENGE, ClientVersionBuild.V4_2_2_14545)]
        public static void HandleServerAuthChallenge422(Packet packet)
        {
            packet.ReadInt32("Unk1");
            packet.ReadInt32("Unk2");
            packet.ReadInt32("Unk3");
            packet.ReadInt32("Unk4");
            packet.ReadInt32("Server Seed");
            packet.ReadByte("Unk Byte");
            packet.ReadInt32("Unk5");
            packet.ReadInt32("Unk6");
            packet.ReadInt32("Unk7");
            packet.ReadInt32("Unk8");
        }

        [Parser(Opcode.CMSG_AUTH_SESSION)]
        public static void HandleAuthSession(Packet packet)
        {
            // Do not overwrite version after Handler was initialized
            packet.ReadEnum<ClientVersionBuild>("Client Build", TypeCode.Int32);

            packet.ReadInt32("Unk Int32 1");
            packet.ReadCString("Account");

            if (ClientVersion.AddedInVersion(ClientVersionBuild.V3_0_2_9056))
                packet.ReadInt32("Unk Int32 2");

            packet.ReadInt32("Client Seed");

            if (ClientVersion.AddedInVersion(ClientVersionBuild.V3_2_0_10192))
                packet.ReadInt64("Unk Int64");

            packet.Store("Proof SHA-1 Hash: ", Utilities.ByteArrayToHexString(packet.ReadBytes(20)));

            AddonHandler.ReadClientAddonsList(packet);
        }

        //[Parser(Opcode.CMSG_AUTH_SESSION, ClientVersionBuild.V4_2_0_14333)]
        [Parser(Opcode.CMSG_AUTH_SESSION, ClientVersionBuild.V4_2_2_14545)]
        public static void HandleAuthSession422(Packet packet)
        {
            packet.ReadByte("Byte 1");
            packet.ReadByte("Byte 2");
            packet.ReadInt32("Int32 3");
            packet.ReadInt32("Int32 4");
            packet.ReadByte("Byte 5");
            packet.ReadByte("Byte 6");
            packet.ReadByte("Byte 7");

            packet.ReadByte("Byte 8");
            packet.ReadByte("Byte 9");
            packet.ReadByte("Byte 10");
            packet.ReadEnum<ClientVersionBuild>("Client Build", TypeCode.Int16);

            packet.ReadByte("Byte 11");
            packet.ReadByte("Byte 12");
            packet.ReadByte("Byte 13");
            packet.ReadByte("Byte 14");

            packet.ReadInt32("Int32 15");
            packet.ReadByte("Byte 16");
            packet.ReadByte("Byte 17");
            packet.ReadByte("Byte 18");
            packet.ReadByte("Byte 19");

            packet.ReadInt32("Int32 20");
            packet.ReadByte("Byte 21");

            packet.ReadInt32("Int32 22");
            packet.ReadByte("Byte 23");

            packet.ReadInt32("Int32 24");
            packet.ReadByte("Byte 25");

            packet.ReadInt32("Int32 26");
            packet.ReadInt32("Int32 27");

            packet.ReadCString("Account name");
            packet.ReadInt32("Int32 28");

            AddonHandler.ReadClientAddonsList(packet);
        }

        [Parser(Opcode.CMSG_AUTH_SESSION, ClientVersionBuild.V4_3_0_15005)]
        public static void HandleAuthSession430(Packet packet)
        {
            packet.ReadInt32("Int32 1");
            packet.ReadByte("Digest (1)");
            packet.ReadInt64("Int64 2");
            packet.ReadInt32("Int32 3");
            packet.ReadByte("Digest (2)");
            packet.ReadInt32("Int32 4");
            packet.ReadByte("Digest (3)");

            packet.ReadInt32("Int32 5");
            packet.StoreBeginList("DigestArray (4)");
            for (var i = 0; i < 7; i++)
                packet.ReadByte("Digest (4)", i);
            packet.StoreEndList();

            packet.ReadEnum<ClientVersionBuild>("Client Build", TypeCode.Int16);

            packet.StoreBeginList("DigestArray (5)");
            for (var i = 0; i < 8; i++)
                packet.ReadByte("Digest (5)", i);
            packet.StoreEndList();

            packet.ReadByte("Unk Byte 6");
            packet.ReadByte("Unk Byte 7");

            packet.ReadInt32("Client Seed");

            packet.StoreBeginList("DigestArray (6)");
            for (var i = 0; i < 2; i++)
                packet.ReadByte("Digest (6)", i);
            packet.StoreEndList();

            AddonHandler.ReadClientAddonsList(packet, packet.ReadInt32());
            packet.ReadByte("Mask"); // TODO: Seems to affect how the size is read
            var size = (packet.ReadByte() >> 4);
            packet.Store("Size", size);
            packet.Store("Account name", Encoding.UTF8.GetString(packet.ReadBytes(size)));
        }

        [Parser(Opcode.CMSG_AUTH_SESSION, ClientVersionBuild.V4_3_2_15211)]
        public static void HandleAuthSession432(Packet packet)
        {
            var sha = new byte[20];
            packet.ReadInt32("Int32 1");
            sha[12] = packet.ReadByte();
            packet.ReadInt32("Int32 2 ");
            packet.ReadInt32("Int32 3");
            sha[0] = packet.ReadByte();
            sha[2] = packet.ReadByte();
            sha[18] = packet.ReadByte();
            sha[7] = packet.ReadByte();
            sha[9] = packet.ReadByte();
            sha[19] = packet.ReadByte();
            sha[17] = packet.ReadByte();
            sha[6] = packet.ReadByte();
            sha[11] = packet.ReadByte();

            packet.ReadEnum<ClientVersionBuild>("Client Build", TypeCode.Int16);

            sha[15] = packet.ReadByte();

            packet.ReadInt64("Int64 4");
            packet.ReadByte("Unk Byte 5");
            packet.ReadByte("Unk Byte 6");
            sha[3] = packet.ReadByte();
            sha[10] = packet.ReadByte();

            packet.ReadInt32("Client Seed");

            sha[16] = packet.ReadByte();
            sha[4] = packet.ReadByte();
            packet.ReadInt32("Int32 7");
            sha[14] = packet.ReadByte();
            sha[8] = packet.ReadByte();
            sha[5] = packet.ReadByte();
            sha[1] = packet.ReadByte();
            sha[13] = packet.ReadByte();

            AddonHandler.ReadClientAddonsList(packet, packet.ReadInt32());
            var highBits = packet.ReadByte() << 5;
            var lowBits = packet.ReadByte() >> 3;
            var size = lowBits | highBits;
            packet.Store("Size", size);
            packet.Store("Account name", Encoding.UTF8.GetString(packet.ReadBytes(size)));
            packet.Store("Proof SHA-1 Hash", Utilities.ByteArrayToHexString(sha));
        }

        [Parser(Opcode.SMSG_AUTH_RESPONSE)]
        public static void HandleAuthResponse(Packet packet)
        {
            var code = packet.ReadEnum<ResponseCode>("Auth Code", TypeCode.Byte);

            switch (code)
            {
                case ResponseCode.AUTH_OK:
                {
                    ReadAuthResponseInfo(ref packet);
                    break;
                }
                case ResponseCode.AUTH_WAIT_QUEUE:
                {
                    if (packet.Length <= 6)
                    {
                        ReadQueuePositionInfo(ref packet);
                        break;
                    }

                    ReadAuthResponseInfo(ref packet);
                    ReadQueuePositionInfo(ref packet);
                    break;
                }
            }
        }

        public static void ReadAuthResponseInfo(ref Packet packet)
        {
            packet.ReadInt32("Billing Time Remaining");
            packet.ReadEnum<BillingFlag>("Billing Flags", TypeCode.Byte);
            packet.ReadInt32("Billing Time Rested");

            // Unknown, these two show the same as expansion payed for.
            // Eg. If account only has payed for Wotlk expansion it will show 2 for both.
            packet.ReadEnum<ClientType>("Account Expansion", TypeCode.Byte);
            if (ClientVersion.AddedInVersion(ClientVersionBuild.V4_0_3_13329))
                packet.ReadEnum<ClientType>("Account Expansion", TypeCode.Byte);
        }

        public static void ReadQueuePositionInfo(ref Packet packet)
        {
            packet.ReadInt32("Queue Position");

            packet.ReadByte("Unk Byte");
        }

        [Parser(Opcode.CMSG_PLAYER_LOGIN)]
        public static void HandlePlayerLogin(Packet packet)
        {
            var guid = packet.ReadGuid("GUID");
            LoginGuid = guid;
        }

        [Parser(Opcode.CMSG_PLAYER_LOGIN, ClientVersionBuild.V4_2_2_14545)]
        public static void HandlePlayerLogin422(Packet packet)
        {
            var bits = new bool[8];
            for (var i = 0; i < 8; ++i)
                bits[i] = packet.ReadBit();

            var bytes = new byte[8];
            if (bits[6]) bytes[5] = (byte)(packet.ReadByte() ^ 1);
            if (bits[0]) bytes[0] = (byte)(packet.ReadByte() ^ 1);
            if (bits[4]) bytes[3] = (byte)(packet.ReadByte() ^ 1);
            if (bits[1]) bytes[4] = (byte)(packet.ReadByte() ^ 1);
            if (bits[2]) bytes[7] = (byte)(packet.ReadByte() ^ 1);
            if (bits[5]) bytes[2] = (byte)(packet.ReadByte() ^ 1);
            if (bits[7]) bytes[6] = (byte)(packet.ReadByte() ^ 1);
            if (bits[3]) bytes[1] = (byte)(packet.ReadByte() ^ 1);

            LoginGuid = packet.StoreBitstreamGuid("GUID", bytes);
        }

        [Parser(Opcode.CMSG_PLAYER_LOGIN, ClientVersionBuild.V4_3_0_15005)]
        public static void HandlePlayerLogin430(Packet packet)
        {
            var bits = new bool[8];
            for (var i = 0; i < 8; ++i)
                bits[i] = packet.ReadBit();

            var bytes = new byte[8];
            if (bits[3]) bytes[4] = (byte)(packet.ReadByte() ^ 1);
            if (bits[7]) bytes[1] = (byte)(packet.ReadByte() ^ 1);
            if (bits[4]) bytes[7] = (byte)(packet.ReadByte() ^ 1);
            if (bits[6]) bytes[2] = (byte)(packet.ReadByte() ^ 1);
            if (bits[5]) bytes[6] = (byte)(packet.ReadByte() ^ 1);
            if (bits[1]) bytes[5] = (byte)(packet.ReadByte() ^ 1);
            if (bits[2]) bytes[3] = (byte)(packet.ReadByte() ^ 1);
            if (bits[0]) bytes[0] = (byte)(packet.ReadByte() ^ 1);

            LoginGuid = packet.StoreBitstreamGuid("GUID", bytes);
        }

        [Parser(Opcode.SMSG_CHARACTER_LOGIN_FAILED)]
        public static void HandleLoginFailed(Packet packet)
        {
            packet.ReadByte("Unk Byte");
        }

        [Parser(Opcode.SMSG_LOGOUT_RESPONSE)]
        public static void HandlePlayerLogoutResponse(Packet packet)
        {
            packet.ReadInt32("Unk Int32");
            packet.ReadByte("Reason");
            // From TC:
            // Reason 1: IsInCombat
            // Reason 2: InDuel or frozen by GM
            // Reason 3: Jumping or Falling
        }

        [Parser(Opcode.SMSG_LOGOUT_COMPLETE)]
        public static void HandleLogoutComplete(Packet packet)
        {
            LoggedInCharacter = null;
        }

        [Parser(Opcode.SMSG_REDIRECT_CLIENT)]
        public static void HandleRedirectClient(Packet packet)
        {
            var ip = packet.ReadIPAddress();
            packet.Store("IP Address", ip);
            packet.ReadUInt16("Port");
            packet.ReadInt32("Token");
            var hash = packet.ReadBytes(20);
            packet.Store("Address SHA-1 Hash", Utilities.ByteArrayToHexString(hash));
        }

        [Parser(Opcode.SMSG_REDIRECT_CLIENT, ClientVersionBuild.V4_2_2_14545)]
        public static void HandleRedirectClient422(Packet packet)
        {
            var hash = packet.ReadBytes(255);
            packet.Store("RSA Hash", Utilities.ByteArrayToHexString(hash));
            packet.ReadInt16("Int 16");
            packet.ReadEnum<UnknownFlags>("Unknown int32 flag", TypeCode.Int32);
            packet.ReadInt64("Int 64");
        }

        [Parser(Opcode.CMSG_REDIRECTION_FAILED)]
        public static void HandleRedirectFailed(Packet packet)
        {
            packet.ReadInt32("Token");
        }

        [Parser(Opcode.CMSG_REDIRECTION_AUTH_PROOF)]
        public static void HandleRedirectionAuthProof(Packet packet)
        {
            packet.ReadCString("Account");

            packet.ReadInt64("Unk Int64");

            var hash = packet.ReadBytes(20);
            packet.Store("Proof SHA-1 Hash", Utilities.ByteArrayToHexString(hash));
        }

        [Parser(Opcode.CMSG_REDIRECTION_AUTH_PROOF, ClientVersionBuild.V4_2_2_14545)]
        public static void HandleRedirectionAuthProof422(Packet packet)
        {
            var bytes = new byte[20];
            bytes[0] = packet.ReadByte();
            bytes[12] = packet.ReadByte();
            bytes[3] = packet.ReadByte();
            bytes[17] = packet.ReadByte();
            bytes[11] = packet.ReadByte();
            bytes[13] = packet.ReadByte();
            bytes[5] = packet.ReadByte();
            bytes[9] = packet.ReadByte();
            bytes[6] = packet.ReadByte();
            bytes[19] = packet.ReadByte();
            bytes[15] = packet.ReadByte();
            bytes[18] = packet.ReadByte();
            bytes[8] = packet.ReadByte();
            packet.ReadInt64("Unk long 1");
            bytes[2] = packet.ReadByte();
            bytes[1] = packet.ReadByte();
            packet.ReadInt64("Unk long 2");
            bytes[7] = packet.ReadByte();
            bytes[4] = packet.ReadByte();
            bytes[16] = packet.ReadByte();
            bytes[14] = packet.ReadByte();
            bytes[10] = packet.ReadByte();
            packet.Store("Proof RSA Hash", Utilities.ByteArrayToHexString(bytes));
        }

        [Parser(Opcode.SMSG_KICK_REASON)]
        public static void HandleKickReason(Packet packet)
        {
            packet.ReadEnum<KickReason>("Reason", TypeCode.Byte);

            if (!packet.CanRead())
                return;

            packet.ReadCString("Unk String");
        }

        [Parser(Opcode.SMSG_MOTD)]
        public static void HandleMessageOfTheDay(Packet packet)
        {
            var lineCount = packet.ReadInt32("Line Count");
            packet.StoreBeginList("Lines");
            for (var i = 0; i < lineCount; i++)
            {
                packet.ReadCString("Line", i);
            }
            packet.StoreEndList();
        }
    }
}
