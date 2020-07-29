using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    public class MemoryRegister8 : IMemoryRegister8
    {
        public MemoryRegister8(Memory memory, UInt32 address, bool readable, bool writeable)
        {
            if (readable)
            {
                memory.IoRegisters8Read.Add(address, this);
            }

            if (writeable)
            {
                memory.IoRegisters8Write.Add(address, this);
            }
        }

        protected byte reg;
        public virtual byte Value { get { return reg; } set { reg = value; } }
    }


    public class MemoryRegister8WithMask : MemoryRegister8
    {
        byte mask;

        public MemoryRegister8WithMask(Memory memory, UInt32 address, bool readable, bool writeable, byte mask) :
            base(memory, address, readable, writeable)
        {
            this.mask = mask;
        }

        public override byte Value
        {
            get
            {
                return base.Value;
            }

            set
            {
                base.Value = (byte) (value & mask);           
            }
        }
    }

  
    public class MemoryRegister8WithSetHook : MemoryRegister8
    {
        public MemoryRegister8WithSetHook(Memory memory, UInt32 address, bool readable, bool writeable) :
            base(memory, address, readable, writeable)
        {
        }

        public override byte Value
        {
            get
            {
                return base.Value;
            }

            set
            {
                byte oldValue = Value;

                base.Value = value;

                if (OnSet != null &&
                    oldValue != value)
                {
                    OnSet(oldValue, value);
                }
            }
        }
            
        public Action<byte, byte> OnSet { get; set; } 
    }

}