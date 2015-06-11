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
            byte[] fileBytes = File.ReadAllBytes("Ironman_Oman.ck2");
            Lexer lexer = new Lexer(fileBytes);
            List<Token> tokens = lexer.Lex();
            CK2Game game = new CK2Game();
            Console.ReadLine();
        }
    }
}
