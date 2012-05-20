﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text;
using WowPacketParser.Enums;
using WowPacketParser.Misc;
using WowPacketParser.Parsing;
using WowPacketParser.SQL;
using WowPacketParser.Store;
using WowPacketParser.Enums.Version;
using WowPacketParser.Processing;

namespace WowPacketParser.Loading
{
    using NameDict = OrderedDictionary;
    using IndexDict = Dictionary<int, OrderedDictionary>;
    public class SniffFile
    {
        public readonly string FileName;
        private readonly Statistics _stats;
        public readonly string LogPrefix;
        public LinkedList<IPacketProcessor> processors = new LinkedList<IPacketProcessor>();

        public SniffFile(string fileName, Tuple<int, int> number = null)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("fileName cannot be null, empty or whitespace.", "fileName");

            _stats = new Statistics();
            FileName = fileName;

            if (number == null)
                LogPrefix = string.Format("[{0}]", Path.GetFileName(fileName));
            else
                LogPrefix = string.Format("[{0}/{1} {2}]", number.Item1, number.Item2, Path.GetFileName(fileName));
        }

        public void Process()
        {
            _stats.SetStartTime(DateTime.Now);
            
            var reader = Reader.GetReader(FileName);
            Trace.WriteLine(string.Format("{0}: Processing packets (type {1})...", LogPrefix, reader.ToString()));

            try
            {
                var packetNum = 0;
                var packetCount = 0;

                // initialize processors
                IPacketProcessor proc = new TextFileOutput();
                if (proc.Init(this))
                    processors.AddLast(proc);
                proc = new SQLFileOutput();
                if (proc.Init(this))
                    processors.AddLast(proc);
                proc = new RawFileOutput();
                if (proc.Init(this))
                    processors.AddLast(proc);
                proc = new SplitRawFileOutput();
                if (proc.Init(this))
                    processors.AddLast(proc);

                Storage.ClearContainers();
                
                while (reader.CanRead())
                {
                    var packet = reader.Read(packetNum, FileName);

                    // read error
                    if (packet == null)
                        continue;

                    if (packetNum == 0)
                    {
                        // determine build version of currently read file
                        if (Settings.ClientBuild == ClientVersionBuild.Zero)
                        {
                            // check if version info given
                            if (reader.GetBuild() == ClientVersionBuild.Zero)
                                ClientVersion.SetVersion(packet.Time);
                            // or set version by timestamp
                            else
                                ClientVersion.SetVersion(packet.Time);
                        }
                        Trace.WriteLine(string.Format("{0}: Assumed version: {1}", LogPrefix, ClientVersion.VersionString));
                    }

                    ++packetNum;

                    // finish if read packet number reached max
                    if (Settings.ReaderFilterPacketNumHigh != 0 && packetNum > Settings.ReaderFilterPacketNumHigh)
                        break;

                    // skip packets if they were filtered out
                    if (packetNum < Settings.ReaderFilterPacketNumLow)
                        continue;

                    // check for filters
                    if (!CheckReadFilters(packet.Opcode))
                        continue;

                    ProcessPacket(packet);
                
                    ++packetCount;

                    // finish if read packet count reached max
                    if (Settings.ReaderFilterPacketsNum > 0 && packetCount == Settings.ReaderFilterPacketsNum)
                        break;
                }
                // finalize processors
                foreach (var procs in processors)
                {
                    procs.Finish();
                }

                _stats.SetEndTime(DateTime.Now);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("File {0} could not be parsed", FileName);
                Trace.WriteLine(ex.Data);
                Trace.WriteLine(ex.GetType());
                Trace.WriteLine(ex.Message);
                Trace.WriteLine(ex.StackTrace);
            }
            finally
            {
                reader.Dispose();
                Trace.WriteLine(string.Format("{0}: {1}", LogPrefix, _stats));
            }
        }

        private bool CheckReadFilters(int opc)
        {
            var opcodeName = Opcodes.GetOpcodeName(opc);
            if (Settings.ReaderFilterOpcode.Length > 0)
                if (!opcodeName.MatchesFilters(Settings.ReaderFilterOpcode))
                    return false;
            // check for ignore filters
            if (Settings.ReaderFilterIgnoreOpcode.Length > 0)
                if (opcodeName.MatchesFilters(Settings.ReaderFilterIgnoreOpcode))
                    return false;

            return true;
        }

        private void ProcessPacket(Packet packet)
        {
            // Parse the packet, read the data into StoreData tree
            Handler.Parse(packet);

            // Update statistics
            _stats.AddByStatus(packet.Status);

            ProcessElem(packet, "Packet");

            foreach (var proc in processors)
            {
                proc.ProcessPacket(packet);
            }

            // Close Writer, Stream - Dispose
            packet.ClosePacket();
        }

        public void ProcessElem(Object data, string name)
        {
            var t = data.GetType();
            foreach (var proc in processors)
            {
                proc.ProcessData(name, data, t);
            }
            if (t == typeof(Packet))
            {
                Packet packet = (Packet)data;
                ProcessElem(packet.GetData(), "PacketData");
            }
            else if (t == typeof(NameDict))
            {
                var itr = ((NameDict)data).GetEnumerator();
                while (itr.MoveNext())
                {
                    ProcessElem(itr.Value, (string)itr.Key);
                }
            }
            else if (t == typeof(IndexDict))
            {
                foreach (var itr in ((IndexDict)data))
                {
                    ProcessElem(itr.Value, name);
                }
            }
        }
    }
}
