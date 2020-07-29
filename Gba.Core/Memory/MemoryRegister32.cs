using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    public class MemoryRegister32 : IMemoryRegister32
    {
        public MemoryRegister32(Memory memory, UInt32 address, bool readable, bool writeable)
        {
            LoWord = new MemoryRegister16(memory, address, readable, writeable);
            HiWord = new MemoryRegister16(memory, address + 2, readable, writeable);

            if (readable)
            {
                memory.IoRegisters32Read.Add(address, this);
            }

            if (writeable)
            {
                memory.IoRegisters32Write.Add(address, this);
            }
        }

        // Allow inheritted classes to set up themselves
        public MemoryRegister32()
        {
        }

        //LSB
        public MemoryRegister16 LoWord { get; protected set; }
        //MSB
        public MemoryRegister16 HiWord { get; protected set; }


        public virtual UInt32 Value 
        { 
            get 
            { 
                return (UInt32)( (HiWord.HighByte.Value << 24) | (HiWord.LowByte.Value << 16) | (LoWord.HighByte.Value << 8) | LoWord.LowByte.Value); 
            }

            set 
            {
                LoWord.LowByte.Value = (byte) (value & 0x000000FF);
                LoWord.HighByte.Value = (byte) ((value & 0x0000FF00) >> 8);
                HiWord.LowByte.Value = (byte) ((value & 0x00FF0000) >> 16);
                HiWord.HighByte.Value = (byte) ((value & 0xFF000000) >> 24);
            }
        }

    }
}
