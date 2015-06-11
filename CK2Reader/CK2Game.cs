using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK2Reader
{
    class CK2Game
    {
        public string Version { get; set; }
        public DateTime CurrentDate { get; set; }
        public uint PlayerID { get; set; }
        public uint PlayerType { get; set; }
        public string PlayerTitle { get; set; }
        public string FilePath { get; set; }
    }
}
