using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK2Reader
{
    class Lexer
    {
        /*
         * Prefixes are either:
         * XX XX - some kind of special marker for the type of data. This one has a 01 00 immediately succeeding it.
         * 04 00 - end of a nested data structure
         * 0C 00 - a 32-bit value without a record separator following it
         * 0F 00 - normally denotes a length-prefixed string, but if part of an array, it will not be preceeded by a special marker
         * 03 00 - start of a nested data structure - this one DOES have a special marker following it for the type of nested structure.
         * 19 01 - double value without record separator
         * 0D 00 - 32-bit float, no record separator
         * 17 00 - not sure of the generality, but seems to be the start of a special string (e.g. job_spymaster) with ID
         * 0E 00 - a yes/no value
        */
        private static ushort[] SpecialPrefixes = new ushort[] { 0x0004, 0x000C, 0x000F, 0x0003, 0x0190, 0x000D, 0x0017, 0x000E, 0x0014 };

        public byte[] Data { get; private set; }
        public bool IsVerbose { get; set; }
        public uint CurrentToken { get; private set; }
        public Lexer(byte[] bytes)
        {
            Data = bytes;
            CurrentToken = 0;
            IsVerbose = true;
        }

        public List<Token> Lex()
        {
            //first, eat the header
            Consume("CK2bin");
            List<Token> tokens = new List<Token>();
            while(true)
            {
                if (CurrentToken >= Data.Count())
                    break;
                Token token = new Token();
                token.Prefix = Read16BitValue();
                
                ushort valType;
                if (token.Prefix == 0x0001 || token.Prefix == 0x2799)
                    continue; //if we read a record splitter (which usually occurs between length-prefixed values in a nested structure), ignore it
                              //also including this weird 99 27 flag I don't understand.

                
                if (!Array.Exists(SpecialPrefixes, x => x == token.Prefix)) 
                {
                    Consume(0x10);
                    Consume(0x00); //0100, =
                    valType = Read16BitValue();
                }
                else
                    valType = token.Prefix;

                switch(valType)
                {
                    case 0x000F: //length
                        token.HeaderValue = Read16BitValue();
                        token.Type = Token.TokenType.String;
                        token.Data = Read(token.HeaderValue);
                        break;
                    case 0x0003: //think it's 'start of nested object' and it lacks any header at all.
                        token.HeaderValue = 0x0300;
                        token.Type = Token.TokenType.Nested;
                        break;
                    case 0x0014: //32 bit value
                    case 0x000C: //32 bit value without a record separator (e.g. array value)
                    case 0x000D: //suspected float
                        token.HeaderValue = 0x0014;
                        token.Type = Token.TokenType.Int;
                        token.Data = Read(4);
                        break;
                    case 0x0004:
                        token.HeaderValue = 0x0004;
                        token.Type = Token.TokenType.NestedEnd;
                        token.Data = null;
                        break;
                    case 0x01E7: //double?
                    case 0x0190: //double without record separator
                        token.HeaderValue = 0x01E7;
                        token.Type = Token.TokenType.Double;
                        token.Data = Read(8);
                        break;
                    case 0x000E: //yes/no
                        token.HeaderValue = 0x000E;
                        token.Type = Token.TokenType.Bool;
                        token.Data = Read(1);
                        break;
                    case 0x279A: //suspected bool
                    case 0x2799:
                        token.HeaderValue = token.Prefix;
                        token.Type = Token.TokenType.SuspectedBool;
                        token.Data = token.Data = new byte[] { (byte)(valType & 0xFF), (byte)(valType >> 8) };
                        break;
                    case 0x0017:
                        token.HeaderValue = Read16BitValue();
                        token.Type = Token.TokenType.Identifier;
                        token.Data = ReadUntilEquals();
                        break;
                    default:
                        switch(token.Prefix)
                        {
                            case 0x00DA: //I forgot
                            case 0x28F3: //regular succession
                            case 0x2747: //gender succession
                                token.HeaderValue = token.Prefix;
                                token.Type = Token.TokenType.Enum;
                                token.Data = new byte[] { (byte)(valType & 0xFF), (byte)(valType >> 8)};
                                break;
                            default:
                                throw new InvalidOperationException(String.Format("Found a {0:X}, prefix {2:X} at {1:X} and have no idea what it is.", valType, CurrentToken, token.Prefix));
                        }
                        break;
                }
                tokens.Add(token);
            }
            return tokens;
        }

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        } 

        private bool ConsumeByte(byte expected)
        {
            if (Data[CurrentToken] != expected)
                return false;
            CurrentToken++;
            return true;
        }

        public void Consume(byte expected)
        {
            if (!ConsumeByte(expected))
                throw new InvalidOperationException(String.Format("Expected {0:X} and found {1:X} at location {2:X}", expected, Data[CurrentToken], CurrentToken));
        }

        public void Consume(string expected)
        {
            foreach (byte b in System.Text.Encoding.ASCII.GetBytes(expected.ToCharArray()))
            {
                if(!ConsumeByte(b))
                    throw new InvalidOperationException(String.Format("Expected {0:X} and found {1:X} at location {2:X}", expected, Data[CurrentToken], CurrentToken));
            }
        }

        public byte[] Read(uint bytes)
        {
            byte[] destination = new byte[bytes];
            Buffer.BlockCopy(Data, (int)CurrentToken, destination, 0, (int)bytes);
            CurrentToken += bytes;
            return destination;
        }

        public byte[] ReadUntilEquals()
        {
            List<byte> bytes = new List<byte>();
            uint old = CurrentToken;
            while (!(Data[CurrentToken] == 0x01 && Data[CurrentToken + 1] == 0x00))
                bytes.Add(Read(1)[0]);
            return bytes.ToArray();
        }

        public ushort Read16BitValue()
        {
            byte[] data = Read(2);
            return BitConverter.ToUInt16(data, 0);
        }
    }
}
