﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using Guid = WowPacketParser.Misc.Guid;
using WowPacketParser.Misc;
using WowPacketParser.Loading;
using System.Diagnostics;
namespace WowPacketParser.Processing
{
    public class RawFileOutput : IPacketProcessor
    {
        BinaryWriter  writer = null;
        IBinaryPacketWriter packetWriter = null;
        string _logPrefix;
        public bool Init(SniffFile file)
        {
            _logPrefix = file.LogPrefix;
            var fileName = file.LogPrefix;
            if (Settings.RawOutputType == "" || Settings.SplitRawOutput)
                return false;
            try
            {
                Trace.WriteLine(string.Format("{0}: Copying packets to raw format({1})...", _logPrefix, Settings.RawOutputType));

                packetWriter = BinaryPacketWriter.Get();
                var dumpFileName = Path.ChangeExtension(fileName, null) + "_rawdump." + Settings.RawOutputType;

                var fileStream = new FileStream(dumpFileName, FileMode.Create, FileAccess.Write, FileShare.None);
                writer = new BinaryWriter(fileStream, Encoding.ASCII);

                packetWriter.WriteHeader(writer);

            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.GetType());
                Trace.WriteLine(ex.Message);
                Trace.WriteLine(ex.StackTrace);
                return false;
            }
            return true;
        }
        public void ProcessPacket(Packet packet)
        {
            packetWriter.WritePacket(packet, writer);
        }
        public void Finish() 
        {
            if (writer != null)
                writer.Close();
        }
        public void ProcessData(string name, Object obj, Type t) {}
    }
}
