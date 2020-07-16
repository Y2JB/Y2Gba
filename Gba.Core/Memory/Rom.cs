using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Gba.Core
{
    /*
     https://mgba-emu.github.io/gbatek/#gbacartridgeheader
     Address Bytes Expl.
      000h    4     ROM Entry Point  (32bit ARM branch opcode, eg. "B rom_start")
      004h    156   Nintendo Logo    (compressed bitmap, required!)
      0A0h    12    Game Title       (uppercase ascii, max 12 characters)
      0ACh    4     Game Code        (uppercase ascii, 4 characters)
      0B0h    2     Maker Code       (uppercase ascii, 2 characters)
      0B2h    1     Fixed value      (must be 96h, required!)
      0B3h    1     Main unit code   (00h for current GBA models)
      0B4h    1     Device type      (usually 00h) (bit7=DACS/debug related)
      0B5h    7     Reserved Area    (should be zero filled)
      0BCh    1     Software version (usually 00h)
      0BDh    1     Complement check (header checksum, required!)
      0BEh    2     Reserved Area    (should be zero filled)
      --- Additional Multiboot Header Entries ---
      0C0h    4     RAM Entry Point  (32bit ARM branch opcode, eg. "B ram_start")
      0C4h    1     Boot mode        (init as 00h - BIOS overwrites this value!)
      0C5h    1     Slave ID Number  (init as 00h - BIOS overwrites this value!)
      0C6h    26    Not used         (seems to be unused)
      0E0h    4     JOYBUS Entry Pt. (32bit ARM branch opcode, eg. "B joy_start")
  */
    public class Rom : IRom
    {        
        private byte[] romData;
        private UInt32[] rom32BitCached;
        private ushort[] rom16BitCached;

        public int RomSize { get { return romData.Length; } }

        public enum BackupType
        {
            BackupNone,
            EEPROM,
            SRAM,
            FLASH,
            FLASH512,
            FLASH1M
        }
        public BackupType SaveGameBackupType { get; private set; }

        const int Max_SRam_Size = 1024 * 64;
        private byte[] sRam;
        public byte[] SRam { get { return sRam; } }

        public Eeprom Eeprom { get; private set; }

        //private readonly int Header_Size = 0xC0;
        private readonly int RomNameOffset = 0x0A0;

        public string RomName { get; private set; }
        
        public string RomFileName { get; private set; }

        public UInt32 EntryPoint { get; private set; }

        public Rom(string fn)
        {
            RomFileName = fn;

            romData = new MemoryStream(File.ReadAllBytes(fn)).ToArray();

            // Cache the entire ROM along 32 bit boundaries. This lets us do fast access when advancing and refilling the CPU pipeline.
            Cache32BitRomValues();
            Cache16BitRomValues();

            DetectSaveType(fn);

            sRam = new byte[Max_SRam_Size];
            this.Eeprom = new Eeprom(this);

            RomName = Encoding.UTF8.GetString(romData, RomNameOffset, 12).TrimEnd((Char)0);
            RomName = RomName.Replace("/", String.Empty);

            EntryPoint = ReadWord(0);

            switch(SaveGameBackupType)
            {
                case BackupType.SRAM:
                    LoadSramData();
                    break;

                case BackupType.EEPROM:
                    Eeprom.Load();
                    break;
            }
        }


        void Cache16BitRomValues()
        {
            rom16BitCached = new ushort[(romData.Length >> 1)];
            for (UInt32 i = 0; i < romData.Length; i += 2)
            {
                rom16BitCached[i >> 1] = ReadHalfWord(i);
            }
        }


        void Cache32BitRomValues()
        {
            rom32BitCached = new UInt32[(romData.Length >> 2)];
            for(UInt32 i=0; i < romData.Length; i+= 4)
            {
                rom32BitCached[i >> 2] = ReadWord(i);
            }
        }


        void DetectSaveType(string fn)
        {
            // Bit hacky but there is no eary way to detect the save type. We search the entire ROM for some known strings. Works for 99% of games
            // https://dillonbeliveau.com/2020/06/05/GBA-FLASH.html
            using (StreamReader file = new StreamReader(fn))
            {
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    foreach (BackupType BackupType in Enum.GetValues(typeof(BackupType)))
                    {
                        if (Regex.Match(line, $"{BackupType}_V\\d\\d\\d").Success)
                        {
                            SaveGameBackupType = BackupType;
                            return;
                        }
                    }
                }

                SaveGameBackupType = BackupType.BackupNone;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadHalfWordFast(UInt32 address)
        {
            return rom16BitCached[(address >> 1)];
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt32 ReadWordFast(UInt32 address)
        {
            return rom32BitCached[(address >> 2)];
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadByte(UInt32 address)
        {
            return romData[address];            
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadHalfWord(UInt32 address)
        {
            // NB: Little Endian
            return (ushort)((ReadByte((UInt32)(address+1)) << 8) | ReadByte(address));
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public UInt32 ReadWord(UInt32 address)
        {
            // NB: Little Endian
            return (UInt32)((ReadByte((UInt32)(address + 3)) << 24) | (ReadByte((UInt32)(address + 2)) << 16) | (ReadByte((UInt32)(address + 1)) << 8) | ReadByte(address));
        }


        public void PersistSaveData()
        {
            switch (SaveGameBackupType)
            {
                case BackupType.SRAM:
                    SaveSramData();
                    break;

                case BackupType.EEPROM:
                    Eeprom.Save();
                    break;
            }
        }


        public void LoadSramData()
        {
            try
            {
                using (FileStream fs = File.Open(Path.ChangeExtension(RomFileName, "sav"), FileMode.Open))
                {
                    using (BinaryReader bw = new BinaryReader(fs))
                    {
                        bw.Read(sRam, 0, Max_SRam_Size);
                    }
                }
            }
            catch (FileNotFoundException)
            {
            }
        }


        public void SaveSramData()
        {
            using (FileStream fs = File.Open(Path.ChangeExtension(RomFileName, "sav"), FileMode.Create))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    bw.Write(sRam, 0, Max_SRam_Size);
                }
            }            
        }
    }
}
