﻿using System;
using System.Collections.Generic;
using PacketParser.Enums;
using PacketParser.Enums.Version;
using PacketParser.Misc;
using PacketParser.Processing;
using PacketDumper.Enums;
using Guid = PacketParser.DataStructures.Guid;
using PacketDumper.Misc;
using PacketParser.DataStructures;
using PacketParser.SQL;

namespace PacketDumper.Processing.SQLData
{
    public class NpcVendorStore : IPacketProcessor
    {
        public readonly TimeSpanDictionary<uint, NpcVendor> NpcVendors = new TimeSpanDictionary<uint, NpcVendor>();
        public bool Init(PacketFileProcessor file)
        {
            return Settings.SQLOutput.HasFlag(SQLOutputFlags.NpcVendor);
        }

        public void ProcessData(string name, int? index, Object obj, Type t)
        {
        }

        public void ProcessPacket(Packet packet)
        {
            if (Opcode.SMSG_LIST_INVENTORY == Opcodes.GetOpcode(packet.Opcode))
            {
                var guid = packet.GetData().GetNode<Guid>("GUID");

                NpcVendors.Add((uint)guid.GetEntry(), packet.GetNode<NpcVendor>("NpcVendorObject"), packet.TimeSpan);
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
            if (NpcVendors.IsEmpty())
                return String.Empty;

            var names = PacketFileProcessor.Current.GetProcessor<NameStore>();

            const string tableName = "npc_vendor";

            var rows = new List<QueryBuilder.SQLInsertRow>();
            foreach (var npcVendor in NpcVendors)
            {
                var comment = new QueryBuilder.SQLInsertRow();
                
                comment.HeaderComment = names.GetName(StoreNameType.Unit, (int)npcVendor.Key);
                rows.Add(comment);
                foreach (var vendorItem in npcVendor.Value.Item1.VendorItems)
                {
                    var row = new QueryBuilder.SQLInsertRow();
                    row.AddValue("entry", npcVendor.Key);
                    row.AddValue("item", vendorItem.ItemId);
                    row.AddValue("slot", vendorItem.Slot);
                    row.AddValue("maxcount", vendorItem.MaxCount);
                    row.AddValue("ExtendedCost", vendorItem.ExtendedCostId);
                    row.Comment = names.GetName(StoreNameType.Item, (int)vendorItem.ItemId, false);
                    rows.Add(row);
                }
            }

            return new QueryBuilder.SQLInsert(tableName, rows, 2).Build();
        }
    }
}
