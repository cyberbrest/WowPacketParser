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
    public sealed partial class Packet : BinaryReader, ITreeNode
    {
        private static readonly bool SniffData = Settings.SQLOutput.HasAnyFlag(SQLOutputFlags.SniffData);
        private static readonly bool SniffDataOpcodes = Settings.SQLOutput.HasAnyFlag(SQLOutputFlags.SniffDataOpcodes);

        private static DateTime _firstPacketTime;

        [SuppressMessage("Microsoft.Reliability", "CA2000", Justification = "MemoryStream is disposed in ClosePacket().")]
        public Packet(byte[] input, int opcode, DateTime time, Direction direction, int number, string fileName)
            : base(new MemoryStream(input.Length), Encoding.UTF8)
        {
            this.BaseStream.Write(input, 0, input.Length);
            SetPosition(0);
            Opcode = opcode;
            Time = time;
            Direction = direction;
            Number = number;
            StoreData = new NamedTreeNode();
            StoreDataCache = StoreData;
            StoreIndexedLists = new LinkedList<Tuple<NamedTreeNode, IndexedTreeNode>>();
            StoreObjects = new Stack<Tuple<NamedTreeNode, LinkedList<Tuple<NamedTreeNode, IndexedTreeNode>>>>();
            FileName = fileName;
            Status = ParsedStatus.None;
            SubPacket = false;

            if (number == 0)
                _firstPacketTime = Time;

            TimeSpan = Time - _firstPacketTime;
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000", Justification = "MemoryStream is disposed in ClosePacket().")]
        public Packet(byte[] input, int opcode, Packet parent)
            : base(new MemoryStream(input.Length), Encoding.UTF8)
        {
            this.BaseStream.Write(input, 0, input.Length);
            SetPosition(0);
            Opcode = opcode;
            Time = parent.Time;
            Direction = parent.Direction;
            Number = parent.Number;
            StoreData = new NamedTreeNode();
            StoreDataCache = StoreData;
            StoreIndexedLists = new LinkedList<Tuple<NamedTreeNode, IndexedTreeNode>>();
            StoreObjects = new Stack<Tuple<NamedTreeNode, LinkedList<Tuple<NamedTreeNode, IndexedTreeNode>>>>();
            FileName = parent.FileName;
            Status = ParsedStatus.None;
            SubPacket = true;
            ParentOpcode = parent.Opcode;

            if (Number == 0)
                _firstPacketTime = Time;

            TimeSpan = Time - _firstPacketTime;
        }

        public int Opcode { get; private set; }
        public DateTime Time { get; private set; }
        public TimeSpan TimeSpan { get; private set; }
        public Direction Direction { get; private set; }
        public int Number { get; private set; }
        public string FileName { get; private set; }
        public ParsedStatus Status { get; set; }
        public string ErrorMessage = "";
        public bool SubPacket;
        public int ParentOpcode;

        public NamedTreeNode GetData()
        {
            return StoreData;
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
            if (BaseStream != null)
                BaseStream.Close();

            Dispose(true);
        }
        public NodeType GetNode<NodeType>(params string[] address)
        {
            NodeType ret;
            if (TryGetNode<NodeType>(out ret, address))
                return ret;
            throw new Exception(String.Format("Could not receive object of type {0} from address{1}", typeof(NodeType), address));
        }
        public bool TryGetNode<NodeType>(out NodeType ret, params string[] address)
        {
            return TryGetNode<NodeType>(out ret, address, 0);
        }
        public bool TryGetNode<NodeType>(out NodeType ret, string[] address, int addrIndex)
        {
            if (address.Length - 1 == addrIndex)
            {
                if (this is NodeType)
                {
                    ret = (NodeType)((Object)this);
                    return true;
                }
                ret = default(NodeType);
                return false;
            }
            return StoreData.TryGetNode<NodeType>(out ret, address, addrIndex);
        }
        public TreeNodeEnumerator GetTreeEnumerator()
        {
            return new TreeNodeEnumerator(this);
        }
    }
}
