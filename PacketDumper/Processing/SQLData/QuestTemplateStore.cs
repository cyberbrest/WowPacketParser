﻿using System;
using System.Collections.Generic;
using PacketParser.Enums;
using PacketParser.Enums.Version;
using PacketParser.Misc;
using PacketParser.Processing;
using PacketParser.SQL;
using PacketDumper.Enums;
using PacketParser.DataStructures;
using PacketDumper.Misc;

namespace PacketDumper.Processing.SQLData
{
    public class QuestTemplateStore : IPacketProcessor
    {
        public readonly TimeSpanDictionary<uint, QuestTemplate> QuestTemplates = new TimeSpanDictionary<uint, QuestTemplate>();
        public bool Init(PacketFileProcessor file)
        {
            return Settings.SQLOutput.HasFlag(SQLOutputFlags.QuestTemplate);
        }

        public void ProcessData(string name, int? index, Object obj, Type t)
        {
        }

        public void ProcessPacket(Packet packet)
        {
            if (Opcode.SMSG_QUEST_QUERY_RESPONSE == Opcodes.GetOpcode(packet.Opcode))
            {
                var entry = packet.GetData().GetNode<KeyValuePair<int, bool>>("Quest ID");

                if (entry.Value) // entry is masked
                    return;

                QuestTemplates.Add((uint)entry.Key, packet.GetNode<QuestTemplate>("QuestTemplateObject"), packet.TimeSpan);
            }
        }
        public void ProcessedPacket(Packet packet)
        {

        }

        public void Finish()
        {

        }

        public string Build()
        {
            if (QuestTemplates.IsEmpty())
                return String.Empty;

            var entries = QuestTemplates.Keys();
            var templatesDb = SQLDatabase.GetDict<uint, QuestTemplate>(entries, "Id");

            return SQLUtil.CompareDicts(QuestTemplates, templatesDb, StoreNameType.Quest, "Id");
        }
    }
}
