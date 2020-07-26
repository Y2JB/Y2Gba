using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    public class MemoryRegister32 : IMemoryRegister32
    {
        public MemoryRegister32(Memory memory, UInt32 address, bool readable, bool writeable)
        {
            Byte0 = new MemoryRegister8(memory, address, readable, writeable);
            Byte1 = new MemoryRegister8(memory, address + 1, readable, writeable);
            Byte2 = new MemoryRegister8(memory, address + 2, readable, writeable);
            Byte3 = new MemoryRegister8(memory, address + 3, readable, writeable);

            if (readable)
            {
                memory.IoRegisters32Read.Add(address, this);
            }

            if (writeable)
            {
                memory.IoRegisters32Write.Add(address, this);
            }
        }

        //LSB
        public MemoryRegister8 Byte0 { get; private set; }
        public MemoryRegister8 Byte1 { get; private set; }
        public MemoryRegister8 Byte2 { get; private set; }
        //MSB
        public MemoryRegister8 Byte3 { get; private set; }


        public UInt32 Value 
        { 
            get 
            { 
                return (UInt32)( (Byte3.Value << 24) | (Byte2.Value << 16) | (Byte1.Value << 8) | Byte0.Value); 
            }

            set 
            {
                Byte0.Value = (byte) (value & 0x000000FF);
                Byte1.Value = (byte) ((value & 0x0000FF00) >> 8);
                Byte2.Value = (byte) ((value & 0x00FF0000) >> 16);
                Byte3.Value = (byte) ((value & 0xFF000000) >> 24);
            }
        }

    }
}
