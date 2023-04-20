using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace Heroes3Editor.Models
{
    public class Hero
    {
        public string Name { get; }

        private readonly Game _game;

        public bool IsHOTAGame => _game.IsHOTA;
        public int BytePosition { get; }

        public int MoveCurrent
        {
            get => GetValue(HeroAttributesAddress.MovementCurrent);
            set => SetValue(value, HeroAttributesAddress.MovementCurrent);
        }

        public int MoveMax
        {
            get => GetValue(HeroAttributesAddress.MovementMax);
            set => SetValue(value, HeroAttributesAddress.MovementMax);
        }

        public short ManaCurrent
        {
            get => GetShortValue(HeroAttributesAddress.ManaCurrent);
            set => SetValue(value, HeroAttributesAddress.ManaCurrent);
        }
        /*
        public int Attack
        {
            get => GetValue(HeroAttributesAddress.Attack);
            set => SetValue(value, HeroAttributesAddress.Attack);
        }
        public int Defense
        {
            get => GetValue(HeroAttributesAddress.Defense);
            set => SetValue(value, HeroAttributesAddress.Defense);
        }
        public int SpellPower
        {
            get => GetValue(HeroAttributesAddress.SpellPower);
            set => SetValue(value, HeroAttributesAddress.SpellPower);
        }
        public int Knowledge
        {
            get => GetValue(HeroAttributesAddress.Knowledge);
            set => SetValue(value, HeroAttributesAddress.Knowledge);
        }*/

        public byte[] Attributes { get; } = new byte[4];
        public int NumOfSkills { get; private set; }
        public string[] Skills { get; } = new string[8];
        public byte[] SkillLevels { get; } = new byte[8];

        public ISet<string> Spells { get; } = new HashSet<string>();

        public string[] Creatures { get; } = new string[7];
        public int[] CreatureAmounts { get; } = new int[7];

        public ISet<string> WarMachines { get; } = new HashSet<string>();
        public string[] ArtifactInfo { get; } = new string[1];

        public IDictionary<string, string> EquippedArtifacts = new Dictionary<string, string>()
        {
            {"Helm", ""},
            {"Neck", ""},
            {"Armor", ""},
            {"Cloak", ""},
            {"Boots", ""},
            {"Weapon", ""},
            {"Shield", ""},
            {"LeftRing", ""},
            {"RightRing", ""},
            {"Item1", ""},
            {"Item2", ""},
            {"Item3", ""},
            {"Item4", ""},
            {"Item5", ""}
        };

        private const int ON = 0;
        private const int OFF = 255;

        public Hero(string name, Game game, int bytePosition)
        {
            Name = name;
            _game = game;
            BytePosition = bytePosition;

            for (int i = 0; i < 4; ++i)
            {
                Attributes[i] = _game.Bytes[BytePosition + Constants.HeroOffsets["Attributes"] + i];
            }

            NumOfSkills = _game.Bytes[BytePosition + Constants.HeroOffsets["NumOfSkills"]];
            for (int i = 0; i < 28; ++i)
            {
                var skillSlotIndex = _game.Bytes[BytePosition + Constants.HeroOffsets["SkillSlots"] + i];
                if (skillSlotIndex != 0)
                {
                    Skills[skillSlotIndex - 1] = Constants.Skills[i];
                    SkillLevels[skillSlotIndex - 1] = _game.Bytes[BytePosition + Constants.HeroOffsets["Skills"] + i];
                }
            }

            for (int i = 0; i < 70; ++i)
            {
                if (_game.Bytes[BytePosition + Constants.HeroOffsets["Spells"] + i] == 1)
                {
                    Spells.Add(Constants.Spells[i]);
                }
            }

            for (int i = 0; i < 7; ++i)
            {
                var code = _game.Bytes[BytePosition + Constants.HeroOffsets["Creatures"] + i * 4];
                if (code != OFF)
                {
                    Creatures[i] = Constants.Creatures[code];
                    var amountBytes = _game.Bytes.AsSpan().Slice(BytePosition + Constants.HeroOffsets["CreatureAmounts"] + i * 4, 4);
                    CreatureAmounts[i] = BinaryPrimitives.ReadInt16LittleEndian(amountBytes);
                }
                else
                {
                    CreatureAmounts[i] = 0;
                }
            }

            foreach (var warMachine in Constants.WarMachines.Names)
            {
                if (_game.Bytes[BytePosition + Constants.HeroOffsets[warMachine]] == Constants.WarMachines[warMachine])
                {
                    WarMachines.Add(warMachine);
                }
            }

            var gears = new List<string>(EquippedArtifacts.Keys);
            foreach (var gear in gears)
            {
                var code = _game.Bytes[BytePosition + Constants.HeroOffsets[gear]];
                if (code != OFF)
                {
                    EquippedArtifacts[gear] = Constants.Artifacts[code];
                }
            }

            Constants.AddHeroName(name);
        }

        public void UpdateAttribute(int i, byte value)
        {
            Attributes[i] = value;
            _game.Bytes[BytePosition + Constants.HeroOffsets["Attributes"] + i] = value;
        }

        public void UpdateSkill(int slot, string skill)
        {
            if (slot < 0 || slot > NumOfSkills)
            {
                return;
            }

            for (int i = 0; i < NumOfSkills; ++i)
            {
                if (Skills[i] == skill)
                {
                    return;
                }
            }

            byte skillLevel = 1;

            if (slot < NumOfSkills)
            {
                var oldSkill = Skills[slot];
                var oldSkillLevelPosition = BytePosition + Constants.HeroOffsets["Skills"] + Constants.Skills[oldSkill];
                skillLevel = _game.Bytes[oldSkillLevelPosition];
                _game.Bytes[oldSkillLevelPosition] = 0;
                _game.Bytes[BytePosition + Constants.HeroOffsets["SkillSlots"] + Constants.Skills[oldSkill]] = 0;
            }

            Skills[slot] = skill;
            SkillLevels[slot] = skillLevel;
            _game.Bytes[BytePosition + Constants.HeroOffsets["Skills"] + Constants.Skills[skill]] = skillLevel;
            _game.Bytes[BytePosition + Constants.HeroOffsets["SkillSlots"] + Constants.Skills[skill]] = (byte)(slot + 1);

            if (slot == NumOfSkills)
            {
                ++NumOfSkills;
                _game.Bytes[BytePosition + Constants.HeroOffsets["NumOfSkills"]] = (byte)NumOfSkills;
            }
        }

        public void UpdateSkillLevel(int slot, byte level)
        {
            if (slot < 0 || slot > NumOfSkills || level < 1 || level > 3)
            {
                return;
            }

            SkillLevels[slot] = level;
            _game.Bytes[BytePosition + Constants.HeroOffsets["Skills"] + Constants.Skills[Skills[slot]]] = level;
        }

        public void AddSpell(string spell)
        {
            if (!Spells.Add(spell))
            {
                return;
            }

            int spellPosition = BytePosition + Constants.HeroOffsets["Spells"] + Constants.Spells[spell];
            _game.Bytes[spellPosition] = 1;

            int spellBookPosition = BytePosition + Constants.HeroOffsets["SpellBook"] + Constants.Spells[spell];
            _game.Bytes[spellBookPosition] = 1;
        }

        public void RemoveSpell(string spell)
        {
            if (!Spells.Remove(spell))
            {
                return;
            }

            int spellPosition = BytePosition + Constants.HeroOffsets["Spells"] + Constants.Spells[spell];
            _game.Bytes[spellPosition] = 0;

            int spellBookPosition = BytePosition + Constants.HeroOffsets["SpellBook"] + Constants.Spells[spell];
            _game.Bytes[spellBookPosition] = 0;
        }

        public void UpdateCreature(int i, string creature)
        {
            if (Creatures[i] == null)
            {
                CreatureAmounts[i] = 1;
                UpdateCreatureAmount(i, 1);
            }

            Creatures[i] = creature;
            _game.Bytes[BytePosition + Constants.HeroOffsets["Creatures"] + i * 4] = Constants.Creatures[creature];
            _game.Bytes[BytePosition + Constants.HeroOffsets["Creatures"] + (i * 4) + 1] = ON;
            _game.Bytes[BytePosition + Constants.HeroOffsets["Creatures"] + (i * 4) + 2] = ON;
            _game.Bytes[BytePosition + Constants.HeroOffsets["Creatures"] + (i * 4) + 3] = ON;
        }

        public void UpdateCreatureAmount(int i, int amount)
        {
            var amountBytes = _game.Bytes.AsSpan().Slice(BytePosition + Constants.HeroOffsets["CreatureAmounts"] + i * 4, 4);
            BinaryPrimitives.WriteInt32LittleEndian(amountBytes, amount);
        }

        public void AddWarMachine(string warMachine)
        {
            if (!WarMachines.Add(warMachine))
            {
                return;
            }

            int position = BytePosition + Constants.HeroOffsets[warMachine];
            _game.Bytes[position] = Constants.WarMachines[warMachine];
            _game.Bytes[position + 1] = ON;
            _game.Bytes[position + 2] = ON;
            _game.Bytes[position + 3] = ON;
        }

        public void RemoveWarMachine(string warMachine)
        {
            if (!WarMachines.Remove(warMachine))
            {
                return;
            }

            int currentBytePos = BytePosition + Constants.HeroOffsets[warMachine];
            _game.Bytes[currentBytePos] = OFF;
            _game.Bytes[currentBytePos + 1] = OFF;
            _game.Bytes[currentBytePos + 2] = OFF;
            _game.Bytes[currentBytePos + 3] = OFF;
        }

        public void UpdateEquippedArtifact(string gear, string artifact)
        {
            int currentBytePos = BytePosition + Constants.HeroOffsets[gear];
            if (!artifact.Contains("None"))
            {
                EquippedArtifacts[gear] = artifact;
                _game.Bytes[currentBytePos] = Constants.Artifacts[artifact];
                _game.Bytes[currentBytePos + 1] = ON;
                _game.Bytes[currentBytePos + 2] = ON;
                _game.Bytes[currentBytePos + 3] = ON;
            }
            else
            {
                EquippedArtifacts[gear] = "";
                _game.Bytes[currentBytePos] = OFF;
                _game.Bytes[currentBytePos + 1] = OFF;
                _game.Bytes[currentBytePos + 2] = OFF;
                _game.Bytes[currentBytePos + 3] = OFF;
            }
        }

        //  NAME|ATTACK|DEFENSE|POWER|KNOWLEDGE|MORALE|LUCK|OTHER
        //   0  |   1  |   2   |  3  |    4    |   5  |  6 |  7
        public string[] UpdateArtifactInfo(string artifact)
        {
            if (null != artifact && !"None".Equals(artifact))
            {
                return Constants.ArtifactInfo[Constants.Artifacts[artifact]].Split("|");
            }
            return null;
        }

        private int GetValue(HeroAttributesAddress address)
        {
            return _game.GetIntValue(BytePosition + (int)address);
        }
        private void SetValue(int value, HeroAttributesAddress address)
        {
            _game.SetValue(BytePosition + (int)address, value);
        }

        private short GetShortValue(HeroAttributesAddress address)
        {
            return _game.GetShortValue(BytePosition + (int)address);
        }
        private void SetValue(short value, HeroAttributesAddress address)
        {
            _game.SetValue(BytePosition + (int)address, value);
        }
    }

    public enum HeroAttributesAddress
    {
        Attack = 69,
        Defense = 70,
        SpellPower = 71,
        Knowledge = 80,
        MovementCurrent = -134,
        MovementMax = -138,
        ManaCurrent = -122,
    }
}
