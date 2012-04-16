using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Collections.Generic;
using ICSharpCode.SharpZipLib.Zip.Compression;
using WowPacketParser.Enums;
using WowPacketParser.Store;
using WowPacketParser.Store.Objects;

namespace WowPacketParser.Misc
{
    public sealed partial class Packet : BinaryReader
    {
        private static readonly bool SniffData = Settings.SQLOutput.HasAnyFlag(SQLOutputFlags.SniffData);
        private static readonly bool SniffDataOpcodes = Settings.SQLOutput.HasAnyFlag(SQLOutputFlags.SniffDataOpcodes);

        private readonly Packet Parent;
        public string ErrorMessage = "";

        [SuppressMessage("Microsoft.Reliability", "CA2000", Justification = "MemoryStream is disposed in ClosePacket().")]
        public Packet(byte[] input, int opcode, DateTime time, Direction direction, int number, string fileName, Packet parent, string error = "")
            : base(new MemoryStream(input.Length), Encoding.UTF8)
        {
            this.BaseStream.Write(input, 0, input.Length);
            SetPosition(0);
            Opcode = opcode;
            Time = time;
            Direction = direction;
            Number = number;
            Writer = new StringWriter();
            FileName = fileName;
            Status = ParsedStatus.None;
            WriteToFile = true;
            Parent = parent != null ? (parent.Parent != null ? parent.Parent : parent) : null;
            if (Parent != null)
            {
                if (Parent.SubPackets == null)
                    Parent.SubPackets = new LinkedList<Packet>();
                Parent.SubPackets.AddFirst(this);
            }
        }

        public int Opcode { get; private set; }
        public DateTime Time { get; private set; }
        public Direction Direction { get; private set; }
        public int Number { get; private set; }
        public StringWriter Writer { get; private set; }
        public string FileName { get; private set; }
        public ParsedStatus Status { get; set; }
        public bool WriteToFile { get; private set; }
        public LinkedList<PacketData> Data;
        public LinkedList<Packet> SubPackets;

        public void AddSniffData(StoreNameType type, int id, string data)
        {
            if (type == StoreNameType.None)
                return;

            if (id == 0 && type != StoreNameType.Map)
                return; // Only maps can have id 0

            if (type == StoreNameType.Opcode && !SniffDataOpcodes)
                return; // Don't add opcodes if its config is not enabled

            if (type != StoreNameType.Opcode && !SniffData)
                return;

            var item = new SniffData
            {
                FileName = FileName,
                TimeStamp = Utilities.GetUnixTimeFromDateTime(Time),
                ObjectType = type,
                Id = id,
                Data = data,
                Number = Number,
            };

           Storage.SniffData.Add(item);
        }

        public void Inflate(int inflatedSize, int bytesToInflate)
        {
            var oldPos = Position;
            var decompress = ReadBytes(bytesToInflate);
            var tailData = ReadToEnd();
            this.BaseStream.SetLength(oldPos + inflatedSize + tailData.Length);
            
            var newarr = new byte[inflatedSize];
            try
            {
                var inflater = new Inflater();
                inflater.SetInput(decompress, 0, bytesToInflate);
                inflater.Inflate(newarr, 0, inflatedSize);
            }
            catch (ICSharpCode.SharpZipLib.SharpZipBaseException)
            {
                var inflater = new Inflater(true);
                inflater.SetInput(decompress, 0, bytesToInflate);
                inflater.Inflate(newarr, 0, inflatedSize);
            }
            SetPosition(oldPos);
            this.BaseStream.Write(newarr, 0, inflatedSize);
            this.BaseStream.Write(tailData, 0, tailData.Length);
            SetPosition(oldPos);
        }
            
        public void Inflate(int inflatedSize)
        {
            Inflate(inflatedSize, (int)(Length - Position));
        }

        public byte[] GetStream(long offset)
        {
            var pos = Position;
            SetPosition(offset);
            var buffer = ReadToEnd();
            SetPosition(pos);
            return buffer;
        }

        public long Position
        {
            get { return BaseStream.Position; }
        }

        public void SetPosition(long val)
        {
            BaseStream.Position = val;
        }

        public long Length
        {
            get { return BaseStream.Length; }
        }

        public bool CanRead()
        {
            return Position != Length;
        }

        public void ClosePacket()
        {
            Writer.Close();

// ReSharper disable ConditionIsAlwaysTrueOrFalse (/slap R#)
            if (BaseStream != null)
// ReSharper restore ConditionIsAlwaysTrueOrFalse
                BaseStream.Close();
            Dispose(true);
        }
    }
}