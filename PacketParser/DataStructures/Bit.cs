using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PacketParser.DataStructures
{
    public struct Bit
    {
        private bool _val;
        public Bit(bool v)
        {
            _val = v;
        }
        public override string ToString()
        {
            return (_val == false) ? "0" : "1";
        }
        public static implicit operator bool(Bit p)
        {
            return p._val;
        }
    }
}
