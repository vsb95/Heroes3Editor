using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.GZip;

namespace Heroes3Editor.Models
{
    public class Game
    {
        public bool IsHOTA { get; set; }
        public byte[] Bytes { get; }

        public IList<Hero> Heroes { get; } = new List<Hero>();

        public Resources Resources { get; }

        // CGM is supposed to be a GZip file, but GZipStream from .NET library throws a
        // "unsupported compression method" exception, which is why we use SharpZipLib.
        // Also CGM has incorrect CRC which every tool/library complains about.
        public Game(string file)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            using var fileStream = (new FileInfo(file)).OpenRead();
            using var gzipStream = new GZipInputStream(fileStream);
            using var memoryStream = new MemoryStream();
            gzipStream.CopyTo(memoryStream);
            Bytes = memoryStream.ToArray();
            var gameVersionMajor = Bytes[8];
            var gameVersionMinor = Bytes[12];

            if (gameVersionMajor >= 44 && gameVersionMinor >= 5)
            {
                SetHOTA();
            }
            else
            {
                SetClassic();
            }
            Constants.LoadAllArtifacts();
            Resources = new Resources(this);
        }

        public void SetHOTA()
        {
            IsHOTA = true;
            Constants.LoadHOTAItems();
            Constants.HeroOffsets["SkillSlots"] = 923;
        }
        public void SetClassic()
        {
            IsHOTA = false;
            Constants.HeroOffsets["SkillSlots"] = 41;
            Constants.RemoveHOTAReferenceCodes();
        }
        public void Save(string file)
        {
            using var fileStream = (new FileInfo(file)).OpenWrite();
            using var gzipStream = new GZipOutputStream(fileStream);
            using var memoryStream = new MemoryStream(Bytes);
            memoryStream.CopyTo(gzipStream);
        }

        public int GetIntValue(int startPosition)
        {
            if (startPosition < 0 || startPosition > Bytes.Length + 1)
            {
                return -1;
            }
            return BitConverter.ToInt32(Bytes, startPosition);
        }
        public short GetShortValue(int startPosition)
        {
            if (startPosition < 0 || startPosition > Bytes.Length + 1)
            {
                return -1;
            }
            return BitConverter.ToInt16(Bytes, startPosition);
        }
        public byte[] GetValue(int startPosition, int length)
        {
            byte[] result = new byte[length];
            for (int i = 0; i < length - 1; i++)
            {
                var t = Bytes[startPosition + i];
                result[i] = t;
            }
            return result;
        }

        public void SetValue(int startPosition, int value)
        {
            WriteBytes(startPosition, BitConverter.GetBytes(value));
        }

        public void SetValue(int startPosition, short value)
        {
            WriteBytes(startPosition, BitConverter.GetBytes(value));
        }

        private void WriteBytes(int startPosition, byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                Bytes[startPosition + i] = bytes[i];
            }
        }

        public int[] SearchValue(short value, int startPosition = 0, byte[] bytes = null, int endPosition = 0)
        {
            byte[] pattern = BitConverter.GetBytes(value);
            return SearchValue(pattern, startPosition, bytes, endPosition);
        }

        public int[] SearchValue(int value, int startPosition = 0, byte[] bytes = null, int endPosition = 0)
        {
            byte[] pattern = BitConverter.GetBytes(value);
            return SearchValue(pattern, startPosition, bytes, endPosition);
        }

        public int[] SearchValue(byte[] pattern, int startPosition = 0, byte[] bytes = null, int endPosition = 0)
        {
            bytes ??= Bytes;

            if (startPosition <= 0 || startPosition > Bytes.Length)
            {
                startPosition = Bytes.Length;
            }
            if (startPosition < pattern.Length)
            {
                startPosition = pattern.Length;
            }

            if (endPosition < 0)
            {
                endPosition = 0;
            }

            var newPositions = new List<int>();
            for (int i = startPosition - pattern.Length; i > endPosition; --i)
            {
                bool found = true;
                for (int j = 0; j < pattern.Length; ++j)
                {
                    if (bytes[i + j] != pattern[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    newPositions.Add(i);
                }
            }

            return newPositions.ToArray();
        }

        private static byte[] IntToBytes(int val)
        {
            byte[] intBytes = BitConverter.GetBytes(val);
            return intBytes;
        }
        public void FindResources(int wood, int ore, int gold)
        {
            var found = false;
            var woodIndex = 0;

            while (!found)
            {
                woodIndex = Search(IntToBytes(wood), woodIndex);
                if (woodIndex < 0)
                {
                    break;
                }
                var woodPre = GetIntValue(woodIndex);
                var mercuryPre = GetIntValue(woodIndex + 4);
                var orePre = GetIntValue(woodIndex + 8);
                var sulfurPre = GetIntValue(woodIndex + 12);
                var crystalPre = GetIntValue(woodIndex + 16);
                var gemsPre = GetIntValue(woodIndex + 20);
                var goldPre = GetIntValue(woodIndex + 24);

                if (woodPre == wood && orePre == ore && goldPre == gold)
                {
                    found = true;
                }
            }
            if (woodIndex >= 0)
            {
                Resources.SetIndex(woodIndex);
            }
        }

        public bool SearchHero(string name)
        {
            int startPosition = Bytes.Length;
            foreach (var hero in Heroes)
            {
                if (hero.Name == name && startPosition > hero.BytePosition)
                {
                    startPosition = hero.BytePosition - 1;
                }
            }

            var bytePosition = SearchHero(name, startPosition);
            if (bytePosition > 0)
            {
                Heroes.Add(new Hero(name, this, bytePosition));
                return true;
            }
            else
            {
                return false;
            }
        }

        private int SearchHero(string name, int startPosition)
        {
            byte[] pattern = new byte[13];
            Encoding.ASCII.GetBytes(name).CopyTo(pattern, 0);
            if (Regex.IsMatch(name, @"\p{IsCyrillic}"))
            {
                pattern = GetCyrrilicBytes(name, true);
            }

            return Search(pattern, startPosition);
        }

        public int Search(byte[] pattern, int startPosition = 0)
        {
            var result = SearchValue(pattern, startPosition);
            return result.Length > 0 ? result.First() : -1;
        }

        public byte[] GetCyrrilicBytes(string value, bool addZero)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding win1251 = Encoding.GetEncoding("windows-1251");
            List<byte> list = new List<byte>();
            list.AddRange(win1251.GetBytes(value));
            if (addZero)
            {
                list.Add(0);
            }
            return list.ToArray();
        }
    }
}
