using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK2Reader
{
    class Program
    {
        static void Main(string[] args)
        {
            byte[] fileBytes = File.ReadAllBytes("Clean.ck2");
            Parser p = new Parser(fileBytes);
            CK2Game game = p.Parse();
            Console.ReadLine();
        }
    }
}
