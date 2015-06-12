using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK2Reader
{
    class Parser
    {
        protected struct Field
        {
            public object id;
            public object data;
        }

        public byte[] Data { get; private set; }
        public uint CurrentToken { get; private set; }

        public Parser(byte[] bytes)
        {
            Data = bytes;
            CurrentToken = 0;
        }

        public CK2Game Parse()
        {
            CK2Game game = new CK2Game();
            Consume("CK2bin");
            while (true)
            {
                if (CurrentToken >= Data.Count())
                    break;
                Field field = ReadField();

            }
            return null;
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
                if (!ConsumeByte(b))
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

        public bool Peek16Bit(byte val1, byte val2)
        {
            if (Data[CurrentToken] == val1 && Data[CurrentToken + 1] == val2)
                Read(2);
            else
                return false;
            return true;
        }

        public ushort Read16BitValue()
        {
            byte[] data = Read(2);
            return BitConverter.ToUInt16(data, 0);
        }

        public uint Read32BitValue()
        {
            byte[] data = Read(4);
            return BitConverter.ToUInt32(data, 0);
        }

        private Field ReadField()
        {
            ushort fieldID = Read16BitValue();
            Field field = new Field();
            //0x000F (length-prefixed string) and 0x0017 (length-prefixed identifier string)
            //0x0014 (32-bit ID) and 0x000C (32-bit value)
            field.id = fieldID == 0x000F || fieldID == 0x0017 ?
                (object)System.Text.Encoding.ASCII.GetString(Read(Read16BitValue())) : 
                fieldID == 0x0014 || fieldID == 0x000C ? Read32BitValue() : fieldID;
            field.data = null;

            if(Peek16Bit(0xA2, 0x01)) //for some reason, there is a stray } at the end of the file.
                field.id = 0x01A2;

            if (fieldID == 0x0003) //sometimes we have { } without an ID, for some reason. give it no ID and then move the pointer back.
            {
                CurrentToken -= 2;
                field.id = 0x0000;
            }
            else
            {
                Consume(0x01);
                Consume(0x00); //equals
            }

            if (Peek16Bit(0x0F, 0x00) || Peek16Bit(0x17, 0x00)) //it's a length-prefixed string OR a length-prefixed special string
            {
                field.data = System.Text.Encoding.ASCII.GetString(Read(Read16BitValue()));
            }
            else if (Peek16Bit(0x03, 0x00)) //start of a nested data structure
            {
                List<Field> fields = new List<Field>();

                if (!(Data[CurrentToken+6] == 0x01 && Data[CurrentToken+7] == 0x00) && (Peek16Bit(0x0C, 0x00) || Peek16Bit(0x14, 0x00))) //it's a list, of int-values or IDs (which are int values)
                {
                    List<uint> values = new List<uint>();
                    values.Add(Read32BitValue());
                    while (Peek16Bit(0x14, 0x00) || Peek16Bit(0x0C, 0x00)) //whilst we still have 32-bit values to read
                        values.Add(Read32BitValue());
                    field.data = values;
                }
                else if (Peek16Bit(0x90, 0x01)) //list of 64-bit doubles
                {
                    List<byte[]> values = new List<byte[]>();
                    values.Add(Read(8));
                    while (Peek16Bit(0x90, 0x01))
                        values.Add(Read(8));
                    field.data = values;
                }
                else if (Peek16Bit(0x0D, 0x00)) //list of 32-bit floats. NOTE it seems all values are 0???
                {
                    List<byte[]> values = new List<byte[]>();
                    values.Add(Read(4));
                    while (Peek16Bit(0x0D, 0x00))
                        values.Add(Read(4));
                    field.data = values;
                }
                else if (Peek16Bit(0x14, 0x00) || Peek16Bit(0x0C, 0x00)) //list of e.g. relations. it's a list of 32-bit:{nested}.
                {
                    List<Field> values = new List<Field>();
                    do
                    {
                        CurrentToken -= 2;
                        values.Add(ReadField());
                    } while (Peek16Bit(0x14, 0x00) || Peek16Bit(0x0C, 0x00));
                    field.data = values;
                }
                else
                {
                    while (!Peek16Bit(0x04, 0x00)) //0x04 0x00 is the end of a nested data structure
                        fields.Add(ReadField());
                    field.data = fields;
                    CurrentToken -= 2;
                }
                Consume(0x04);
                Consume(0x00);
            }
            else if (Peek16Bit(0x14, 0x00)) //ID
                field.data = Read32BitValue();
            else if (Peek16Bit(0x0C, 0x00)) //a 32-bit value
            {
                uint val = Read32BitValue();
                field.data = val;
            }
            else if (Peek16Bit(0xE7, 0x01)) //64-bit floating number
                field.data = Read(8); //TODO: work out what this thing even is. Double?
            else if (Peek16Bit(0x0E, 0x00)) //boolean - 01 for 'yes', 00 for 'no.
                field.data = Read(1)[0] == 0x01;
            else if(Data[CurrentToken+1] >= 0x27 && Data[CurrentToken+1] <= 0x30) //enum values seem to be around this
            {
                /*(Peek16Bit(0x99, 0x27) || Peek16Bit(0xBF, 0x29) || Peek16Bit(0x55, 0x29) || Peek16Bit(0xBE, 0x29) ||
                Peek16Bit(0xC1, 0x29) || Peek16Bit(0x1E, 0x28) || Peek16Bit(0x0B, 0x2B) || Peek16Bit(0x38, 0x2C) || Peek16Bit(0x82, 0x30) ||
                Peek16Bit(0x69, 0x2F) || Peek16Bit(0xBD, 0x2F) || Peek16Bit(0x07, 0x2B)) //enum value*/
                field.data = Read16BitValue();
            }
            if (field.data == null)
                System.Diagnostics.Debugger.Break();
            return field;
        }
    }
}
