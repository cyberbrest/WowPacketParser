using WowPacketParser.Misc;

namespace WowPacketParser.Enums.Version.V5_3_0_16981
{
    public static class Opcodes_5_3_0
    {
        public static BiDictionary<Opcode, int> Opcodes()
        {
            return Opcs;
        }

        private static readonly BiDictionary<Opcode, int> Opcs = new BiDictionary<Opcode, int>
        {
            {Opcode.CMSG_AUTH_SESSION, 0x09F1},
            {Opcode.CMSG_REDIRECTION_AUTH_PROOF, 0x1AA1},
            {Opcode.SMSG_ADDON_INFO, 0x0C80},
            {Opcode.SMSG_AUTH_CHALLENGE, 0x0221},
            {Opcode.SMSG_AUTH_RESPONSE, 0x0890},
            {Opcode.SMSG_CHAR_ENUM, 0x0FDD},
            {Opcode.SMSG_GUILD_ROSTER, 0x0C0A},
            {Opcode.SMSG_REALM_SPLIT, 0x0F89},
            {Opcode.SMSG_REDIRECT_CLIENT, 0x0328},
            {Opcode.SMSG_UPDATE_OBJECT, 0x0C65},
        };
    }
}