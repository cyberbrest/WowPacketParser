using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using WowPacketParser.Enums;
using WowPacketParser.Misc;

namespace WowPacketParser.Loading
{
    public sealed class KSnifferZackReader : IPacketReader
    {
        private readonly Regex HeaderRegex = new Regex("^(?<stoc>{SERVER}|{CLIENT})\\sPacket:\\s\\((?<opcode>0x[0-9a-fA-F]*)\\)\\s(?<name>[A-Z0-9_]*)\\sPacketSize = (?<size>[0-9]*)\\sTimeStamp = (?<time>[0-9]*)");

        private readonly StreamReader _reader;

        public KSnifferZackReader(string file)
        {
            _reader = new StreamReader(new FileStream(file, FileMode.Open), true);
        }

        public bool CanRead()
        {
            return _reader.EndOfStream;
        }

        public Packet Read(int number, string fileName)
        {
            _reader.ReadLine();

            var line = _reader.ReadLine();
            if (line == null || !HeaderRegex.IsMatch(line))
                return null;

            var match = HeaderRegex.Match(line);

            var opcode = Convert.ToInt32(match.Groups["opcode"].Value, 16);
            var length = Convert.ToInt32(match.Groups["size"].Value, 10);
            var time = Convert.ToDateTime(match.Groups["time"].Value);
            var direction = (match.Groups["stoc"].Value == "{CLIENT}" ?
                Direction.ClientToServer : Direction.ServerToClient);
            var data = _reader.ReadLine();

            var tokens = data.Split(' ');
            var arr = new byte[length];
            for (var i = 0; i < length; i++)
                arr[i] = byte.Parse(tokens[i], NumberStyles.HexNumber, null);

            return new Packet(arr, opcode, time, direction, number, fileName);
        }
        public void Dispose()
        {
            if (_reader != null)
            {
                _reader.BaseStream.Dispose();
                _reader.Dispose();
            }
        }

        public ClientVersionBuild GetBuild()
        {
            return ClientVersionBuild.Zero;
        }

        public DateTime? PeekDateTime()
        {
            var oldPos = _reader.BaseStream.Position;
            var p = Read(0, "");
            _reader.BaseStream.Position = oldPos;
            return p.Time;
        }
    }
}
