using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CK2Reader
{
    class Parser
    {
        public static string ByteArrayToString(byte[] ba)
       {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2} ", b);
            return hex.ToString();
        } 

        private enum FieldType
        {
            ID16Bit,
            ID32Bit,
            String,
            Object,
            Array32Bit,
            Array32BitFloat,
            Array64BitFloat,
            Float64Bit,
            Bool,
            Enum
        }

        private class Field
        {
            public object id;
            public object data;
            public FieldType idType;
            public FieldType dataType;
            public uint location;

            public string ToDataString()
            {
                if (data is List<Field>)
                    return String.Join(",", ((List<Field>)data).Select(f => f.ToDataString()));
                else if (data is List<uint>)
                    return String.Join(",", (List<uint>)data);
                else if (data is List<byte[]>)
                    return String.Join(",", ((List<byte[]>)data).Select(t => ByteArrayToString(t)));
                else if (data is byte[])
                    return ByteArrayToString((byte[])data);
                else
                    return data.ToString();
            }
        }

        private static Dictionary<ushort, string> idToString = new Dictionary<ushort,string>()
            {
                {0x0116, "version"},
                {0x279B, "date"},
                {0x292D, "player"},
                {0x2F7F, "savelocation"},
                {0x289E, "no_idea"},
                {0x28E0, "no_idea2"},
                {0x2E9A, "player_shield"},
                {0x2AFB, "player_realm"},
                {0x2851, "rebel"},
                {0x27F2, "unit"},
                {0x28C8, "subunit"},
                {0x2817, "start_date"},
                {0x28C2, "flags"},
                {0x2744, "dynasties"},
                {0x2717, "character"},
                {0x2EEB, "dyn_title"},
                {0x2BA7, "delayed_event"},
                {0x28B7, "relation"},
                {0x2A79, "active_ambition"},
                {0x313A, "active_focus"},
                {0x2A78, "active_plot"},
                {0x2A7A, "active_faction"},
                {0x000B, "id"},
                {0x278B, "religion"},
                {0x277E, "provinces"},
                {0x289A, "title"},
                {0x2820, "diplomacy"},
                {0x2839, "combat"},
                {0x2896, "war"},
                {0x282B, "active_war"},
                {0x282C, "previous_war"},
                {0x2C7F, "next_outbreak_id"},
                {0x2C7E, "disease"},
                {0x015F, "income_statistics"},
                {0x0160, "nation_size_statistics"},
                {0x2D36, "no_idea_again"},
                {0x2B35, "character_action"},
                {0x01A2, "checksum"},
                {0x00DA, "type"},
                {0x0118, "data"},
                {0x001B, "name"},
                {0x2785, "culture"},
                {0x2B2B, "coat_of_arms"},
                {0x2E7E, "decadence"},
                {0x2EDC, "set_coat_of_arms"},
                {0x2C70, "birth_name"},
                {0x2910, "female"},
                {0x2734, "birth_date"},
                {0x2725, "father"},
                {0x2726, "mother"},
                {0x2740, "attributes"},
                {0x2718, "traits"},
                {0x2823, "graphical_culture"},
                {0x2736, "dynasty"},
                {0x2A2C, "dna"},
                {0x2B57, "properties"},
                {0x2712, "fertility"}, //actually 10x less, float
                {0x299E, "health"}, //same
                {0x2F20, "consort"},
                {0x28A5, "prestige"}, //1000x smaller
                {0x274E, "piety"}, //same
                {0x2B2D, "job_title"},
                {0x2739, "wealth"}, //weird float I think
                {0x273E, "employer"},
                {0x2B2F, "host"},
                {0x292F, "estimated_monthly_income"}, //float?
                {0x2C2D, "estimated_yearly_income"},
                {0x2C2E, "estimated_yearly_peacetime_income"},
                {0x2A5F, "averaged_income"},
                {0x27BE, "ledger"},
                {0x27C9, "income"},
                {0x27C5, "lastmonthincometable"},
                {0x27C3, "lastmonthincome"},
                {0x2E00, "ambition_date"},
                {0x2B59, "guardian"},
                {0x2738, "spouse"},
                {0x300B, "designated_heir"},
                {0x29F7, "lover"},
                {0x2AFA, "demesne"},
                {0x279C, "capital"},
                {0x2D5E, "liege_troops"},
                {0x2C03, "light_infantry"},
                {0x27D5, "heavy_infantry"},
                {0x27D8, "archers"},
                {0x2F06, "my_liegelevy_contribution"},
                {0x2F2B, "military_techpoints"},
                {0x2F2C, "economy_techpoints"},
                {0x2F2D, "culture_techpoints"},
                {0x2F5C, "liegelevy_reinforcements"},
                {0x2930, "estimated_monthly_expense"},
                {0x27C4, "lastmonthexpense"},
                {0x2853, "action"},
                {0x2D39, "action_date"},
                {0x2D3B, "action_location"},
                {0x2C06, "galleys"},
                {0x2AF4, "claim"},
                {0x2C4B, "pressed"},
                {0x2A2E, "weak"},
                {0x2D60, "known_plots"},
                {0x2AE8, "is_bastard"},
                {0x287A, "modifier"},
                {0x28FF, "inherit"},
                {0x2D7E, "hidden"},
                {0x2DFA, "real_father"},
                {0x2A17, "is_prisoner"},
                {0x2A19, "imprisoned"},
                {0x2D84, "last_objective"},
                {0x2CA2, "spawned_bastards"},
                {0x010A, "score"}, //1000x
                {0x2C05, "pikemen"},
                {0x2C02, "light_cavalry"},
                {0x2F53,null},
                {0x27C0,null},
                {0x27CB,null},
                {0x27CA,null},
                {0x2983,null},
                {0x3151,null},
                {0x273D,null},
                {0x0,null},
                {0x2CA0,null},
                {0x2B6B,null},
                {0x2EBA,null},
                {0x2967,null},
                {0x2EC4,null},
                {0x28F4,null},
                {0x2EE9,null},
                {0x27EC,null},
                {0x28AA,null},
                {0x27F0,null},
                {0x27F4,null},
                {0x27FF,null},
                {0x278E,null},
                {0x2ED0,null},
                {0x2C87,null},
                {0x27F3,null},
                {0x27BC,null},
                {0x2C89,null},
                {0x2C8B,null},
                {0x2F2A,null},
                {0x27C6,null},
                {0x2C8A,null},
                {0x2DC6,null},
                {0x2814,null},
                {0x2C8C,null},
                {0x2DC8,null},
                {0x2ABC,null},
                {0x2E4A,null},
                {0x30A9,null},
                {0x2DC7,null},
                {0x19D,null},
                {0x6B,null},
                {0x2C04,null},
                {0x2ADA,null},
                {0x2822,null},
                {0x2B53,null},
                {0x2C62,null},
                {0x2C63,null},
                {0x284F,null},
                {0x2850,null},
                {0x3009,null},
                {0x3008,null},
                {0x293E,null},
                {0x2ED5,null},
                {0x2EBF,null},
                {0x30D4,null},
                {0x2B5A,null},
                {0x2EDE,null},
                {0x2BE1,null},
                {0x27ED,null},
                {0x2845,null},
                {0x2932,null},
                {0x30D2,null},
                {0x308B,null},
                {0x3088,null},
                {0x3089,null},
                {0x308A,null},
                {0x8D,null},
                {0x3090,null},
                {0x1E4,null},
                {0x78,null},
                {0x3095,null},
                {0x3096,null},
                {0x3097,null},
                {0x309B,null},
                {0x2842,null},
                {0x2F6A,null},
                {0x3121,null},
                {0x2E70,null},
                {0x2A1B,null},
                {0x28A0,null},
                {0x2E51,null},
                {0x2E52,null},
                {0x2BD6,null},
                {0x2E82,null},
                {0x2E65,null},
                {0x2AF2,null},
                {0x2954,null},
                {0x30E5,null},
                {0x2875,null},
                {0x2882,null},
                {0x289D,null},
                {0x29B0,null},
                {0x2995,null},
                {0x2898,null},
                {0x2791,null},
                {0x30CA,null},
                {0x2DA0,null},
                {0x2CB1,null},
                {0x2953,null},
                {0x2E30,null},
                {0x2DA1,null},
                {0x2BAA,null},
                {0x282D,null},
                {0x282E,null},
                {0x30C3,null},
                {0x28F3,null},
                {0x2F52,null},
                {0x2A6A,null},
                {0x2B7A,null},
                {0x2F62,null},
                {0x2F63,null},
                {0x29D5,null},
                {0x85,null},
                {0x2FBB,null},
                {0x2C17,null},
                {0x2CD9,null},
                {0x2F2F,null},
                {0x2F31,null},
                {0x2B02,null},
                {0x27A8,null},
                {0x2D5C,null},
                {0x2C88,null},
                {0x28C6,null},
                {0x27B2,null},
                {0x2F24,null},
                {0x2F27,null},
                {0x2F28,null},
                {0x2F29,null},
                {0x2807,null},
                {0x2DE3,null},
                {0x27A0,null},
                {0x278F,null},
                {0x2815,null},
                {0x2881,null},
                {0x2C7C,null},
                {0x2C80,null},
                {0x2B03,null},
                {0x2D5D,null},
                {0x2EF4,null},
                {0x2919,null},
                {0x2743,null},
                {0x2747,null},
                {0x2AA8,null},
                {0x2E75,null},
                {0x2783,null},
                {0x2DAB,null},
                {0x30DA,null},
                {0x2C40,null},
                {0x2DA8,null},
                {0x2DBE,null},
                {0x2DBF,null},
                {0x29B4,null},
                {0x2BAD,null},
                {0x2E01,null},
                {0x2E02,null},
                {0x30D8,null},
                {0x30EC,null},
                {0x2C41,null},
                {0x2E54,null},
                {0x2C3D,null},
                {0x2E8B,null},
                {0x2FBD,null},
                {0x2E20,null},
                {0x2BCA,null},
                {0x2BC8,null},
                {0x2BC9,null},
                {0x2D70,null},
                {0x2E6A,null},
                {0x2E6B,null},
                {0x2DF4,null},
                {0x2AEA,null},
                {0x3115,null},
                {0x30C7,null},
                {0x30C4,null},
                {0x2EF6,null},
                {0x2EEC,null},
                {0x29C2,null},
                {0x2EEA,null},
                {0x56,null},
                {0x2B67,null},
                {0x2B2C,null},
                {0x2B66,null},
                {0x2F19,null},
                {0x2A2F,null},
                {0x2EDF,null},
                {0x283E,null},
                {0x2829,null},
                {0x7A,null},
                {0x2D58,null},
                {0x2830,null},
                {0x2D59,null},
                {0x2D5A,null},
                {0x283A,null},
                {0x28F0,null},
                {0x283C,null},
                {0x282A,null},
                {0x27AC,null},
                {0x2821,null},
                {0x29C0,null},
                {0x2826,null},
                {0x2824,null},
                {0x2827,null},
                {0x2828,null},
                {0x282F,null},
                {0x2825,null},
                {0x2E08,null},
                {0x2E09,null},
                {0x2E2D,null},
                {0x2E2C,null},
                {0x2E40,null},
                {0x2B62,null},
                {0xF4,null}
            };

        public byte[] Data { get; private set; }
        public uint CurrentToken { get; private set; }

        private HashSet<string> undefinedSet = new HashSet<string>();
        private HashSet<ushort> undefinedIDs = new HashSet<ushort>();

        public Parser(byte[] bytes)
        {
            Data = bytes;
            CurrentToken = 0;
        }

        public CK2Game Parse()
        {
            CK2Game game = new CK2Game();
            Consume("CK2bin");
            List<Field> fields = new List<Field>();
            while (true)
            {
                if (CurrentToken >= Data.Count())
                    break;

                Field f = ReadField();
                fields.Add(DecodeField(f));
            }

            File.WriteAllLines("undefined_ids.txt", undefinedSet.Select(t => t+"\r\n"));
            StreamWriter s = File.AppendText("undefined_ids.txt");
            File.WriteAllLines("undefined_ids_plain.txt", undefinedIDs.Select(t => String.Format("{{{0:X}, }},", t)));
            s.WriteLine("{0} IDs were undefined and {1} were defined.", undefinedSet.Count, idToString.Count);
            s.Close();
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
            field.location = CurrentToken - 2;
            //0x000F (length-prefixed string) and 0x0017 (length-prefixed identifier string)
            //0x0014 (32-bit ID) and 0x000C (32-bit value)
            if(fieldID == 0x000F || fieldID == 0x0017)
            {
                field.id = (object)System.Text.Encoding.ASCII.GetString(Read(Read16BitValue()));
                field.idType = FieldType.String;
            }
            else if(fieldID == 0x0014 || fieldID == 0x000C)
            {
                field.id = Read32BitValue();
                field.idType = FieldType.ID32Bit;
            }
            else
            {
                field.id = fieldID;
                field.idType = FieldType.ID16Bit;
            }
            field.data = null;

            if (Peek16Bit(0xA2, 0x01)) //for some reason, there is a stray } at the end of the file.
            {
                field.id = (ushort)0x01A2;
                field.idType = FieldType.ID16Bit;
            }

            if (fieldID == 0x0003) //sometimes we have { } without an ID, for some reason. give it no ID and then move the pointer back.
            {
                CurrentToken -= 2;
                field.id = (ushort)0x0000;
                field.idType = FieldType.ID16Bit;
            }
            else
            {
                Consume(0x01);
                Consume(0x00); //equals
            }

            if (Peek16Bit(0x0F, 0x00) || Peek16Bit(0x17, 0x00)) //it's a length-prefixed string OR a length-prefixed special string
            {
                field.data = System.Text.Encoding.ASCII.GetString(Read(Read16BitValue()));
                field.dataType = FieldType.String;
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
                    field.dataType = FieldType.Array32Bit;
                    field.data = values;
                }
                else if (Peek16Bit(0x90, 0x01)) //list of 64-bit doubles
                {
                    List<byte[]> values = new List<byte[]>();
                    values.Add(Read(8));
                    while (Peek16Bit(0x90, 0x01))
                        values.Add(Read(8));
                    field.dataType = FieldType.Array64BitFloat;
                    field.data = values;
                }
                else if (Peek16Bit(0x0D, 0x00)) //list of 32-bit floats. NOTE it seems all values are 0???
                {
                    List<byte[]> values = new List<byte[]>();
                    values.Add(Read(4));
                    while (Peek16Bit(0x0D, 0x00))
                        values.Add(Read(4));
                    field.dataType = FieldType.Array32BitFloat;
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
                    field.dataType = FieldType.Object;
                    field.data = values;
                }
                else
                {
                    while (!Peek16Bit(0x04, 0x00)) //0x04 0x00 is the end of a nested data structure
                        fields.Add(ReadField());
                    field.dataType = FieldType.Object;
                    field.data = fields;
                    CurrentToken -= 2;
                }
                Consume(0x04);
                Consume(0x00);
            }
            else if (Peek16Bit(0x14, 0x00)) //ID
            {
                field.data = Read32BitValue();
                field.dataType = FieldType.ID32Bit;
            }
            else if (Peek16Bit(0x0C, 0x00)) //a 32-bit value
            {
                field.data = Read32BitValue();
                field.dataType = FieldType.ID32Bit;
            }
            else if (Peek16Bit(0xE7, 0x01)) //64-bit floating number
            {
                field.data = Read(8); //TODO: work out what this thing even is. Double?
                field.dataType = FieldType.Float64Bit;
            }
            else if (Peek16Bit(0x0E, 0x00)) //boolean - 01 for 'yes', 00 for 'no.
            {
                field.data = Read(1)[0] == 0x01;
                field.dataType = FieldType.Bool;
            }
            else if (Data[CurrentToken + 1] >= 0x27 && Data[CurrentToken + 1] <= 0x30) //enum values seem to be around this
            {
                field.data = Read16BitValue();
                field.dataType = FieldType.Enum;
            }
            if (field.data == null)
                System.Diagnostics.Debugger.Break();
            return field;
        }

        private Field DecodeField(Field f)
        {
            Field decodedField = new Field();
            decodedField.idType = FieldType.String;
            switch(f.idType)
            {
                case FieldType.ID16Bit:
                    string id;
                    idToString.TryGetValue((ushort)f.id, out id);
                    if (id == null)
                    {
                        if(!undefinedIDs.Contains((ushort)f.id))
                        {
                            undefinedSet.Add(String.Format("ID: {0:X} around location {1:X} with data {2}", f.id, f.location, f.ToDataString()));
                            undefinedIDs.Add((ushort)f.id);
                        }

                    }
                    decodedField.id = id;
                    break;
                case FieldType.ID32Bit:
                    decodedField.id = f.id.ToString();
                    break;
                case FieldType.String:
                    decodedField.id = f.id;
                    break;
            }

            switch(f.dataType)
            {
                case FieldType.Bool:
                case FieldType.String:
                    decodedField.data = f.data;
                    decodedField.dataType = f.dataType;
                    break;
                case FieldType.Object:
                    decodedField.data = ((List<Field>)f.data).Select(t => DecodeField(t)).ToList();
                    decodedField.dataType = f.dataType;
                    break;
                default:
                    break;
            }
            return decodedField;
        }
    }
}
