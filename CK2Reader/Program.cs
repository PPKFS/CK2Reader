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
            Lexer lexer = new Lexer(fileBytes);
            List<Token> tokens = lexer.Lex();
            CK2Game game = new CK2Game();
            
            //header - this is CK2bin.
            lexer.Consume("CK2bin");

            //version number and date in YYYY.MM.DD form.
            /*game.Version = lexer.ReadLengthPrefixedString();
            game.CurrentDate = DateTime.Parse(lexer.ReadLengthPrefixedString());
            lexer.ConsumeRecord(); //UNKNOWN 0300 0B00
            game.PlayerID = lexer.Read32BitValue();
            game.PlayerType = lexer.Read32BitValue();
            game.FilePath = lexer.ReadLengthPrefixedString();
            lexer.ConsumeRecord(); //UNKNOWN this one is a 1400 (32bit int), but I can't seem to find any equivalent value.
            lexer.ConsumeRecord(); //0C 00 EC C8 00 00 9A 2E No idea.
            //Some of the saves have a player shield here, but others don't (and I can't find a pattern).
            //As such, it's currently ignored - it's duplicated with the dynasty anyway.
            //end of shield data is FB 2A.
            lexer.ConsumeUntil(0xFB2A);
            lexer.ConsumeRecord();
            game.PlayerTitle = lexer.ReadLengthPrefixedString();*/
            Console.ReadLine();
        }
    }
}
