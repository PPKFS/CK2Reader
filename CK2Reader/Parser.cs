using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

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
            FloatArray,
            DoubleArray,
            Double,
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
                {0x289E, "seed"}, //ironman only
                //{0x28E0, "no_idea2"},
                {0x2E9A, "player_shield"},
                {0x2AFB, "player_realm"},
                {0x2851, "rebel"},
                {0x27F2, "unit"},
                {0x28C8, "sub_unit"},
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
                {0x2F53, "peace_months"},
                {0x27C0, "current_income"},
                {0x27CB, "thismonthincome"},
                {0x27CA, "expense"},
                {0x2983, "variables"},
                {0x3151, "focus_date"},
                {0x273D, "unborn"},
                {0x2CA0, "known_father"},
                {0x2B6B, "primary"},
                {0x2EBA, "retinue_reinforce_rate"},
                {0x2967, "nickname"},
                {0x2EC4, "raised_liege_troops"},
                {0x28F4, "regent"},
                {0x2EE9, "moved_capital"},
                {0x27EC, "army"},
                {0x28AA, "previous"},
                {0x27F0, "home"},
                {0x27F4, "morale"}, //divide 1000
                {0x27FF, "leader"},
                {0x278E, "owner"},
                {0x2ED0, "retinue_type"},
                {0x2C87, "troops"},
                {0x27F3, "movement_progress"}, //divide
                {0x27BC, "location"},
                {0x2C89, "flank"},
                {0x2C8B, "flank_center"},
                {0x2F2A, "is_looter"},
                {0x27C6, "lastmonthexpensetable"},
                {0x2C8A, "flank_left"},
                {0x2DC6, "player_left"},
                {0x2814, "vassal"},
                {0x2C8C, "flank_right"},
                {0x2DC8, "player_right"},
                {0x2ABC, "base"},
                {0x2E4A, "attachments"},
                {0x30A9, "in_hiding"},
                {0x2DC7, "player_center"},
                {0x019D, "path"},
                {0x006B, "target"},
                {0x2C04, "knights"},
                {0x2ADA, "raid_prov"},
                {0x2822, "value"},
                {0x2B53, "offer_marrige_interaction"},
                {0x2C62, "extra_actor"},
                {0x2C63, "extra_recipient"},
                {0x284F, "actor"},
                {0x2850, "recipient"},
                {0x3009, "execute_on_action"},
                {0x3008, "instant_reply"},
                {0x293E, "ai_will_do"},
                {0x2ED5, "earmark"},
                {0x2EBF, "disband_on_peace"},
                {0x30D4, "maintenance_multiplier"},
                {0x2B5A, "educate_character_interaction"},
                {0x2EDE, "diplomatic_immunity"},
                {0x2BE1, "join_ambition_interaction"},
                {0x27ED, "navy"},
                {0x2845, "attrition"},
                {0x2932, "at_sea"},
                {0x30D2, "player_name"},
                {0x308B, "chronicle_collection"},
                {0x3088, "chronicle"},
                {0x3089, "chronicle_chapter"},
                {0x308A, "chronicle_entry"},
                {0x008D, "text"},
                {0x3090, "no_clue_again"}, //1346455619
                {0x01E4, "picture"},
                {0x0078, "year"},
                {0x3095, "chronicle_position"},
                {0x3096, "chapter_position"},
                {0x3097, "entry_position"},
                {0x309B, "chronicle_icon_lit"},
                {0x2842, "retreat"}, //I think
                {0x2F6A, "elector_funds"},
                {0x3121, "assim_culture"},
                {0x2E70, "married"},
                {0x2A1B, "historical"},
                {0x28A0, "death_date"},
                {0x2E51, "death_reason"},
                {0x2E52, "killer"},
                {0x2BD6, "old_holding"},
                {0x2E82, "occluded"},
                {0x2E65, "dynasty_named_title"},
                {0x2AF2, "liege"},
                {0x2954, "ruler"},
                {0x30E5, "is_dynamic"},
                {0x2875, "event"},
                {0x2882, "days"},
                {0x289D, "scope"},
                {0x29B0, "char"},
                {0x2995, "random"},
                {0x2898, "from"},
                {0x2791, "province"},
                {0x30CA, "saved_event_target"},
                {0x2DA0, "new_char"},
                {0x2CB1, "ruled_years"},
                {0x2953, "multiplier"},
                {0x2E30, "raised_days"},
                {0x2DA1, "truce"},
                {0x2BAA, "peace_offer"},
                {0x282D, "attacker"},
                {0x282E, "defender"},
                {0x30C3, "can_call_to_war"},
                {0x28F3, "succession"},
                {0x2F52, "looter_hostility_days"},
                {0x2A6A, "parent_scope"},
                {0x2B7A, "backer"},
                {0x2F62, "auto_invite"},
                {0x2F63, "pending_backer"},
                {0x29D5, "active"},
                {0x0085, "parent"},
                {0x2FBB, "was_heresy"},
                {0x2C17, "authority"},
                {0x2CD9, "original_parent"},
                {0x2F2F, "reformed"},
                {0x2F31, "original_reformed"},
                {0x2B02, "max_settlements"},
                {0x27A8, "build_time"},
                {0x2D5C, "owner_troops"},
                {0x2C88, "levy"},
                {0x28C6, "reinforce"},
                {0x27B2, "technology"},
                {0x2F24, "tech_levels"},
                {0x2F27, "lootable_gold"}, //divide for the next couple
                {0x2F28, "max_lootable_gold"},
                {0x2F29, "loot_protected_by_fort"},
                {0x2807, "building_construction"},
                {0x2DE3, "progress"}, //divide
                {0x27A0, "building"},
                {0x278F, "controller"},
                {0x2815, "first"},
                {0x2881, "months"},
                {0x2C7C, "timeperiod"},
                {0x2C80, "outbreak_id"},
                {0x2B03, "settlement_construction"},
                {0x2D5D, "owner_ships"},
                {0x2EF4, "tradepost"},
                {0x2919, "winter"},
                {0x2743, "holder"},
                {0x2747, "gender"},
                {0x2AA8, "law"},
                {0x2E75, "last_change"},
                {0x2783, "history"},
                {0x2DAB, "de_jure_law_changer"},
                {0x30DA, "de_jure_law_changes"},
                {0x2C40, "set_the_kings_peace"},
                {0x2DA8, "set_appoint_generals"},
                {0x2DBE, "set_allow_title_revokation"},
                {0x2DBF, "set_allow_free_infidel_revokation"},
                {0x29B4, "army_size_percentage"}, // divide
                {0x2BAD, "de_jure_liege"},
                {0x2E01, "normal_law_changer"},
                {0x2E02, "normal_law_change"},
                {0x30D8, "set_tribal_vassal_levy_control"},
                {0x30EC, "set_tribal_vassal_tax_income"},
                {0x2C41, "set_investiture"},
                {0x2E54, "pentarch"},
                {0x2C3D, "succ_law_changer"},
                {0x2E8B, "conquest_culture"},
                {0x2FBD, "grant"},
                {0x2E20, "usurp_date"},
                {0x2BCA, "nomination"},
                {0x2BC8, "voter"},
                {0x2BC9, "nominee"},
                {0x2D70, "law_vote"},
                {0x2E6A, "de_jure_ass_years"},
                {0x2E6B, "assimilating_liege"},
                {0x2DF4, "cannot_cancel_vote"},
                {0x2AEA, "adjective"},
                {0x3115, "vice_royalty_revokation"},
                {0x30C7, "set_allow_vice_royalties"},
                {0x30C4, "vice_royalty"},
                {0x2EF6, "custom_name"},
                {0x2EEC, "holding_dynasty"},
                {0x29C2, "settlement"},
                {0x2EEA, "dynamic"},
                {0x0056, "color"},
                {0x2B67, "landless"},
                {0x2B2C, "title_female"},
                {0x2B66, "foa"},
                {0x2F19, "temporary"},
                {0x2A2F, "rebels"},
                {0x2EDF, "major_revolt"},
                {0x283E, "siege_combat"},
                {0x2829, "attackers"},
                {0x007A, "day"},
                {0x2D58, "last_leader"},
                {0x2830, "losses"},
                {0x2D59, "tactic"},
                {0x2D5A, "tactic_day"},
                {0x283A, "phase"},
                {0x28F0, "adjacencies"},
                {0x283C, "land_combat"},
                {0x282A, "defenders"},
                {0x27AC, "terrain"},
                {0x2821, "casus_belli"},
                {0x29C0, "landed_title"},
                {0x2826, "add_defender"},
                {0x2824, "add_attacker"},
                {0x2827, "rem_defender"},
                {0x2828, "battle"},
                {0x282F, "result"},
                {0x2825, "rem_attacker"},
                {0x2E08, "attacker_participation"},
                {0x2E09, "defender_participation"},
                {0x2E2D, "defender_score"}, //divide
                {0x2E2C, "attacker_score"},
                {0x2E40, "vassal_liege"},
                {0x2B62, "thirdparty"},
                //{0x00F4, "dunno"}
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
            Consume("CK2bin");
            List<Field> fields = new List<Field>();
            while (CurrentToken < Data.Length)
                fields.Add(DecodeField(ReadField()));

            File.WriteAllLines("undefined_ids.txt", undefinedSet.Select(t => t+System.Environment.NewLine));
            File.WriteAllLines("undefined_ids_plain.txt", undefinedIDs.Select(t => String.Format("{{{0:X}, }},", t)));

            using(StreamWriter s = File.AppendText("undefined_ids.txt"))
                s.WriteLine("{0} IDs were undefined and {1} were defined.", undefinedSet.Count, idToString.Count);

            return PopulateGame(fields);
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

        public int Read32BitValue()
        {
            byte[] data = Read(4);
            return BitConverter.ToInt32(data, 0);
        }

        public float ReadFloat()
        {
            return 5;
        }

        private Field ReadField()
        {
            ushort fieldID = Read16BitValue();
            Field field = new Field() { data = null };

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
                if (!(Data[CurrentToken+6] == 0x01 && Data[CurrentToken+7] == 0x00) && (Peek16Bit(0x0C, 0x00) || Peek16Bit(0x14, 0x00))) //it's a list, of int-values or IDs (which are int values)
                {
                    List<int> values = new List<int>();
                    values.Add(Read32BitValue());
                    while (Peek16Bit(0x14, 0x00) || Peek16Bit(0x0C, 0x00)) //whilst we still have 32-bit values to read
                        values.Add(Read32BitValue());
                    field.dataType = FieldType.Array32Bit;
                    field.data = values;
                }
                else if (Peek16Bit(0x90, 0x01)) //list of 64-bit doubles
                {
                    List<float> values = new List<float>();
                    do
                        values.Add(BitConverter.ToInt64(Read(8), 0) / (float)Math.Pow(2, 15));
                    while (Peek16Bit(0x90, 0x01));
                    field.dataType = FieldType.DoubleArray;
                    field.data = values;
                }
                else if (Peek16Bit(0x0D, 0x00)) //list of 32-bit floats. NOTE it seems all values are 0???
                {
                    List<float> values = new List<float>();
                    do
                        values.Add(BitConverter.ToInt32(Read(4), 0) / (float)Math.Pow(2, 15));
                    while (Peek16Bit(0x0D, 0x00));
                    field.dataType = FieldType.FloatArray;
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
                    List<Field> fields = new List<Field>();
                    while (!Peek16Bit(0x04, 0x00)) //0x04 0x00 is the end of a nested data structure
                        fields.Add(ReadField());
                    field.dataType = FieldType.Object;
                    field.data = fields;
                    CurrentToken -= 2;
                }
                Consume(0x04);
                Consume(0x00);
            }
            else if (Peek16Bit(0x14, 0x00) || Peek16Bit(0x0C, 0x00)) //ID or a 32-bit value
            {
                field.data = Read32BitValue();
                field.dataType = FieldType.ID32Bit;
            }
            else if (Peek16Bit(0xE7, 0x01)) //64-bit floating number
            {
                field.data = BitConverter.ToInt64(Read(8), 0) / (double)Math.Pow(2, 15);
                field.dataType = FieldType.Double;
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
                throw new InvalidOperationException(String.Format("Found ID {0} and have no matching clause", field.id));
            return field;
        }

        private Field DecodeField(Field f)
        {
            Field decodedField = new Field();
            decodedField.idType = FieldType.String;
            decodedField.dataType = f.dataType;
            decodedField.data = f.data;
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
            if(f.dataType == FieldType.Object)
                 decodedField.data = ((List<Field>)f.data).Select(t => DecodeField(t)).ToList();
            return decodedField;
        }

        private CK2Game PopulateGame(List<Field> fields)
        {
            //at this point, all fields are decoded (string IDs and parsed data). Just need to map them.
            CK2Game game = new CK2Game();
            MapForEach(fields,
                new Dictionary<string, Action<object>>(){
                    {"version", val => game.Version = val.ToString()},
                    {"date", val => game.CurrentDate = DateTime.Parse((string)val)},
                    {"player", val => game.PlayerID = BuildIdentifier(val) },
                    {"savelocation", val => game.FilePath = val.ToString()}
                });
            
            return new CK2Game();
        }

        private Identifier BuildIdentifier(object data)
        {
            List<Field> fields = (List<Field>)data;
            Expect(fields, 2);
            Identifier id = new Identifier();
            id.ID = (int)GetFieldByID(fields, "id");
            id.Type = (int)GetFieldByID(fields, "type");
            return id;
        }

        private void Expect(List<Field> data, int count)
        {
            if (data.Count != count)
                throw new ArgumentOutOfRangeException();
        }

        private object GetFieldByID(List<Field> fieldList, string id)
        {
            return fieldList.FirstOrDefault(t => ((string)t.id) == id).data;
        }

        private void MapForEach(IEnumerable<Field> items, Dictionary<string, Action<object>> actions)
        {
            foreach(Field f in items)
                actions[(string)f.id](f.data);
        }
    }
}
