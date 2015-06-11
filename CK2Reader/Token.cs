using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK2Reader
{
    class Token
    {
        public enum TokenType
        {
            String,
            Nested,
            NestedEnd,
            Int,
            Double,
            Bool,
            Identifier,
            Enum,
            SuspectedBool
        }

        public ushort Prefix { get; set; }

        public TokenType Type { get; set; }

        public ushort HeaderValue { get; set; }

        public byte[] Data { get; set; }
    }
}
