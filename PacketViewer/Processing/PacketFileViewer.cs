﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using PacketParser.Enums;
using PacketParser.Enums.Version;
using PacketParser.Processing;
using PacketParser.Misc;
using PacketParser.Loading;
using PacketParser.DataStructures;
using PacketParser.Parsing;

using System.ComponentModel;

using PacketViewer.DataStructures;
using PacketViewer.Forms;
using System.Threading;

namespace PacketViewer.Processing
{
    public class PacketFileViewer : PacketFileProcessor, IDisposable
    {
        public PacketFileTab Tab;

        private IPacketReader reader;
        private string readerType = "";

        private const int minPacketsForProgressUpdate = 600;

        public PacketFileViewer(string fileName, Tuple<int, int> number = null, PacketFileTab tab = null)
            : base(fileName, number)
        {
            Tab = tab;
            readerType = Reader.GetReaderTypeByFileName(fileName);
            reader = Reader.GetReader(readerType, fileName);
        }

        public string GetFileInfoString()
        {
            return "File Info: " + FileName + "  Version: " + reader.GetBuild().ToString() +" Expansion: " + ClientVersion.GetExpansion(reader.GetBuild()).ToString() + " ReaderType: " + readerType;
        }

        public void Process(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            Current = this;
            ClientVersion.SetVersion(reader.GetBuild());
            var packetNum = 0;
            // initialize processors
            InitProcessors();

            var packets = new List<PacketEntry>();
            bool first = true;

            uint oldPct = 0;
            uint progressCheckPackets = 0;
            worker.ReportProgress((int)oldPct);
            while (reader.CanRead())
            {
                ++progressCheckPackets;
                var packet = reader.Read(packetNum, FileName);

                // read error
                if (packet == null)
                    continue;

                ++packetNum;

                if (worker.CancellationPending == true)
                {
                    e.Cancel = true;
                    return;
                }

                ProcessPacket(packet, packets);

                if (progressCheckPackets >= minPacketsForProgressUpdate)
                {
                    var newPct = reader.GetProgress();
                    if (newPct != oldPct)
                    {
                        worker.ReportProgress((int)newPct);
                        oldPct = newPct;
                        if (Tab != null)
                        {
                            AddPackets(packets);
                            if (first)
                            {
                                first = false;
                                packets = new List<PacketEntry>(packets.Count * 3);
                            }
                            else
                                packets.Clear();
                        }
                    }
                    progressCheckPackets = 0;
                }
            }
            AddPackets(packets);

            FinishProcessors();

            reader.Dispose();
            worker.ReportProgress(100);
            GC.Collect();
        }

        delegate void AddPacketCallback(List<PacketEntry> packets);

        public void AddPackets(List<PacketEntry> packets)
        {
            if (Tab.InvokeRequired)
            {
                AddPacketCallback d = new AddPacketCallback(Tab.AddPackets);
                Tab.Invoke(d, new object[] { packets });
            }
            else
            {
                Tab.AddPackets(packets);
            }
        }

        private void ProcessPacket(Packet packet, List<PacketEntry> packets)
        {
            // Parse the packet, read the data into StoreData tree
            Handler.Parse(packet);

            ProcessData(packet);

            var packetEntry = new PacketEntry
            {
                Number = (uint)packet.Number,
                Length = (ushort)packet.Length,
                Sec = (uint)packet.TimeSpan.TotalSeconds,
                Time = packet.Time,
                Opcode = (ushort)packet.Opcode,
                OpcodeString = Opcodes.GetOpcodeName(packet.Opcode),
                ParsedPacket = GetProcessor<TextBuilder>().LastPacket
            };

            packets.Add(packetEntry);

            // Close Writer, Stream - Dispose
            packet.ClosePacket();
        }

        public void Dispose()
        {
            Tab = null;
        }
    }
}
