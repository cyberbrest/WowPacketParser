﻿using PacketParser.Enums;
using PacketParser.SQL;
using PacketParser.Misc;

namespace PacketParser.DataStructures
{
    [DBTableName("creature_template")]
    public class UnitTemplate : ITextOutputDisabled
    {
        [DBFieldName("name")]
        public string Name;

        [DBFieldName("subname")]
        public string SubName;

        [DBFieldName("IconName")]
        public string IconName;

        [DBFieldName("type_flags")]
        public CreatureTypeFlag TypeFlags;

        public uint TypeFlags2;

        [DBFieldName("type")]
        public CreatureType Type;

        [DBFieldName("family")]
        public CreatureFamily Family;

        [DBFieldName("rank")]
        public CreatureRank Rank;

        [DBFieldName("KillCredit1")]
        public uint KillCredit1;

        [DBFieldName("KillCredit2")]
        public uint KillCredit2;

        public int UnkInt;

        [DBFieldName("PetSpellDataId")]
        public uint PetSpellData;

        [DBFieldName("modelid", Count = 4)]
        public uint[] DisplayIds;

        [DBFieldName("Health_mod")]
        public float Modifier1;

        [DBFieldName("Mana_mod")]
        public float Modifier2;

        [DBFieldName("RacialLeader")]
        public bool RacialLeader;

        [DBFieldName("questItem", Count = 6)]
        public uint[] QuestItems;

        [DBFieldName("movementId")]
        public uint MovementId;

        public ClientType Expansion;
    }
}
