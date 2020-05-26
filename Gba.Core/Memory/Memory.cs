using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{ 
    public class Memory : IArmMemoryReaderWriter
    {
        GameboyAdvance gba;

        public Memory(GameboyAdvance gba)
        {
            this.gba = gba;
        }


        public byte ReadByte(UInt32 address)
        {
            if(address >= 0x08000000 && address <= 0x09FFFFFF)
            {
                return gba.Rom.ReadByte(address - 0x08000000);
            }
            throw new ArgumentException("Bad Memory Read");
        }


        public ushort ReadHalfWord(UInt32 address)
        {
            // NB: Little Endian
            return (ushort)((ReadByte((UInt32)(address + 1)) << 8) | ReadByte(address));
        }


        public UInt32 ReadWord(UInt32 address)
        {
            // NB: Little Endian
            return (UInt32)((ReadByte((UInt32)(address + 3)) << 24) | (ReadByte((UInt32)(address + 2)) << 16) | (ReadByte((UInt32)(address + 1)) << 8) | ReadByte(address));
        }


        public void WriteByte(UInt32 address, byte value)
        {
            throw new NotImplementedException();
        }
        public void WriteHalfWord(UInt32 address, ushort value)
        {
            throw new NotImplementedException();
        }
        public void WriteWord(UInt32 address, UInt32 value)
        {
            throw new NotImplementedException();
        }
    }
}
