﻿using System;
using PacketParser.Misc;
using PacketParser.Enums;
using PacketParser.Enums.Version;
using PacketParser.Processing;
using PacketDumper.Enums;
using Guid = PacketParser.DataStructures.Guid;
using PacketDumper.Misc;
using PacketParser.DataStructures;

namespace PacketDumper.Processing.SQLData
{
    public class NpcEmoteStore : IPacketProcessor
    {
        public readonly TimeSpanMultiDictionary<Guid, EmoteType> Emotes = new TimeSpanMultiDictionary<Guid, EmoteType>();
        public bool Init(PacketFileProcessor file)
        {
            return Settings.SQLOutput.HasFlag(SQLOutputFlags.CreatureTemplate);
        }

        public void ProcessData(string name, int? index, Object obj, Type t, TreeNodeEnumerator constIter)
        {

        }

        public void ProcessPacket(Packet packet)
        {
            switch(Opcodes.GetOpcode(packet.Opcode))
            {
                case Opcode.SMSG_EMOTE:
                    var emote = packet.GetData().GetNode<EmoteType>("Emote ID");
                    var guid = packet.GetData().GetNode<Guid>("GUID");

                    if (guid.GetObjectType() == ObjectType.Unit)
                        Emotes.Add(guid, emote, packet.TimeSpan);
                    break;
            }
        }
        public void ProcessedPacket(Packet packet)
        {

        }

        public void Finish()
        {

        }

    }
}
