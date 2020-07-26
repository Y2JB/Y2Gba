using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    public class MemoryRegister16 : IMemoryRegister16
    {
        public MemoryRegister16(Memory memory, UInt32 address, bool readable, bool writeable)
        {
            LowByte = new MemoryRegister8(memory, address, readable, writeable);
            HighByte = new MemoryRegister8(memory, address + 1, readable, writeable);

            if (readable)
            {
                memory.IoRegisters16Read.Add(address, this);
            }

            if (writeable)
            {
                memory.IoRegisters16Write.Add(address, this);
            }
        }


        public MemoryRegister16(Memory memory, UInt32 address, bool readable, bool writeable, byte highByteMask)
        {
            LowByte = new MemoryRegister8(memory, address, readable, writeable);
            HighByte = new MemoryRegister8WithMask(memory, address + 1, readable, writeable, highByteMask);

            if (readable)
            {
                memory.IoRegisters16Read.Add(address, this);
            }

            if (writeable)
            {
                memory.IoRegisters16Write.Add(address, this);
            }
        }


        public MemoryRegister16(Memory memory, UInt32 address, bool readable, bool writeable, IMemoryRegister8 lowByte, IMemoryRegister8 highByte)
        {
            LowByte = lowByte;
            HighByte = highByte;

            if (readable)
            {
                memory.IoRegisters16Read.Add(address, this);
            }

            if (writeable)
            {
                memory.IoRegisters16Write.Add(address, this);
            }
        }


        //LSB
        public IMemoryRegister8 LowByte { get; set; }

        //MSB
        public IMemoryRegister8 HighByte { get; set; }


        public virtual ushort Value
        {
            get
            {
                return (ushort)((HighByte.Value << 8) | LowByte.Value);
            }

            set
            {
                ushort oldValue = Value;

                HighByte.Value = (byte)(value >> 8);
                LowByte.Value = (byte)(value & 0x00FF);
            }
        }

        
    }

}
