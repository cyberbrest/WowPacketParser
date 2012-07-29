using System;
using System.Collections.Generic;
using PacketParser.Enums;
using PacketParser.Enums.Version;
using PacketParser.Misc;
using PacketParser.Processing;
using PacketDumper.Enums;
using PacketDumper.Misc;
using PacketParser.DataStructures;
using PacketParser.SQL;

namespace PacketDumper.Processing.SQLData
{
    public class QuestPOIStore : IPacketProcessor
    {
        public bool LoadOnDepend { get { return false; } }
        public Type[] DependsOn { get { return null; } }

        public ProcessPacketEventHandler ProcessAnyPacketHandler { get { return ProcessPacket; } }
        public ProcessedPacketEventHandler ProcessedAnyPacketHandler { get { return null; } }
        public ProcessDataEventHandler ProcessAnyDataHandler { get { return null; } }

        // Quest POI (QuestId, Id)
        public readonly TimeSpanDictionary<Tuple<uint, uint>, QuestPOI> QuestPOIs = new TimeSpanDictionary<Tuple<uint, uint>, QuestPOI>();

        public bool Init(PacketFileProcessor file)
        {
            return Settings.SQLOutput.HasFlag(SQLOutputFlags.QuestPOI);
        }

        public void ProcessPacket(Packet packet)
        {
            if (packet.Status != ParsedStatus.Success)
                return;

            if (Opcode.SMSG_QUEST_POI_QUERY_RESPONSE == Opcodes.GetOpcode(packet.Opcode))
            {
                var quests = packet.GetData().GetNode<IndexedTreeNode>("Quests");

                foreach (var q in quests)
                {
                    var quest = q.Value;
                    var questId = quest.GetNode<Int32>("Quest ID");
                    var pois = quest.GetNode<IndexedTreeNode>("POIs");
                    foreach (var p in pois)
                    {
                        var poi = p.Value;
                        QuestPOIs.Add(new Tuple<uint, uint>((uint)questId, (uint)poi.GetNode<Int32>("POI Index")), poi.GetNode<QuestPOI>("QuestPOIObject"), packet.TimeSpan);
                    }
                }
            }
        }

        public void Finish()
        {

        }

        public string Build()
        {
            if (QuestPOIs.IsEmpty())
                return String.Empty;

            const string tableName1 = "quest_poi";
            const string tableName2 = "quest_poi_points";

            var names = PacketFileProcessor.Current.GetProcessor<NameStore>();
            // `quest_poi`
            var rows = new List<QueryBuilder.SQLInsertRow>();
            foreach (var quest in QuestPOIs)
            {
                var questPOI = quest.Value.Item1;

                var row = new QueryBuilder.SQLInsertRow();

                row.AddValue("questId", quest.Key.Item1);
                row.AddValue("id", quest.Key.Item2);
                row.AddValue("objIndex", questPOI.ObjectiveIndex);
                row.AddValue("mapid", questPOI.Map);
                row.AddValue("WorldMapAreaId", questPOI.WorldMapAreaId);
                row.AddValue("FloorId", questPOI.FloorId);
                row.AddValue("unk3", questPOI.UnkInt1);
                row.AddValue("unk4", questPOI.UnkInt2);
                row.Comment = names.GetName(StoreNameType.Quest, (int)quest.Key.Item1, false);

                rows.Add(row);
            }

            var result = new QueryBuilder.SQLInsert(tableName1, rows, 2).Build();

            // `quest_poi_points`
            rows = new List<QueryBuilder.SQLInsertRow>();
            foreach (var quest in QuestPOIs)
            {
                var questPOI = quest.Value.Item1;

                if (questPOI.Points != null) // Needed?
                    foreach (var point in questPOI.Points)
                    {
                        var row = new QueryBuilder.SQLInsertRow();

                        row.AddValue("questId", quest.Key.Item1);
                        row.AddValue("id", quest.Key.Item2);
                        row.AddValue("idx", point.Index); // Not on sniffs
                        row.AddValue("x", point.X);
                        row.AddValue("y", point.Y);
                        row.Comment = names.GetName(StoreNameType.Quest, (int)quest.Key.Item1, false);

                        rows.Add(row);
                    }
            }

            result += new QueryBuilder.SQLInsert(tableName2, rows, 2).Build();

            return result;
        }
    }
}
