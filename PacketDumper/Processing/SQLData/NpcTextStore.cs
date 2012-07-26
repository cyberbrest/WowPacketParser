using System;
using PacketParser.Misc;
using PacketParser.Enums;
using PacketParser.Enums.Version;
using PacketParser.Processing;
using PacketDumper.Enums;
using System.Collections.Generic;
using PacketParser.DataStructures;
using PacketDumper.Misc;
using PacketParser.SQL;

namespace PacketDumper.Processing.SQLData
{
    public class NpcTextStore : IPacketProcessor
    {
        public bool LoadOnDepend { get { return false; } }
        public Type[] DependsOn { get { return null; } }

        public ProcessPacketEventHandler ProcessAnyPacketHandler { get { return ProcessPacket; } }
        public ProcessedPacketEventHandler ProcessedAnyPacketHandler { get { return null; } }
        public ProcessDataEventHandler ProcessAnyDataHandler { get { return null; } }

        public readonly TimeSpanDictionary<uint, NpcText> NpcTexts = new TimeSpanDictionary<uint, NpcText>();
        public bool Init(PacketFileProcessor file)
        {
            return Settings.SQLOutput.HasFlag(SQLOutputFlags.NpcText);
        }

        public void ProcessPacket(Packet packet)
        {
            if (Opcode.SMSG_NPC_TEXT_UPDATE == Opcodes.GetOpcode(packet.Opcode))
            {
                var entry = packet.GetData().GetNode<KeyValuePair<int, bool>>("Entry");

                if (entry.Value) // entry is masked
                    return;

                NpcTexts.Add((uint)entry.Key, packet.GetData().GetNode<NpcText>("NpcTextObject"), packet.TimeSpan);
            }
        }

        public void Finish()
        {

        }

        public string Build()
        {
            if (NpcTexts.IsEmpty())
                return String.Empty;

            // Not TDB structure
            const string tableName = "npc_text";

            var rows = new List<QueryBuilder.SQLInsertRow>();
            foreach (var npcTextPair in NpcTexts)
            {
                var row = new QueryBuilder.SQLInsertRow();
                var npcText = npcTextPair.Value.Item1;

                row.AddValue("Id", npcTextPair.Key);

                for (var i = 0; i < npcText.Probabilities.Length; i++)
                    row.AddValue("Probability" + (i + 1), npcText.Probabilities[i]);

                for (var i = 0; i < npcText.Texts1.Length; i++)
                    row.AddValue("Text1_" + (i + 1), npcText.Texts1[i]);

                for (var i = 0; i < npcText.Texts2.Length; i++)
                    row.AddValue("Text2_" + (i + 1), npcText.Texts2[i]);

                for (var i = 0; i < npcText.Languages.Length; i++)
                    row.AddValue("Language" + (i + 1), npcText.Languages[i]);

                for (var i = 0; i < npcText.EmoteDelays[0].Length; i++)
                    for (var j = 0; j < npcText.EmoteDelays[1].Length; j++)
                        row.AddValue("EmoteDelay" + (i + 1) + "_" + (j + 1), npcText.EmoteDelays[i][j]);

                for (var i = 0; i < npcText.EmoteIds[0].Length; i++)
                    for (var j = 0; j < npcText.EmoteIds[1].Length; j++)
                        row.AddValue("EmoteId" + (i + 1) + "_" + (j + 1), npcText.EmoteDelays[i][j]);

                rows.Add(row);
            }

            return new QueryBuilder.SQLInsert(tableName, rows).Build();
        }
    }
}
