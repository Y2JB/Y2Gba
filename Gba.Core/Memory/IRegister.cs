using System;

namespace Gba.Core
{
    public interface IMemoryRegister8
    {
        public byte Value { get; set; } 
    }


    public interface IMemoryRegister16
    {
        public ushort Value { get; set;  }
    }

    public interface IMemoryRegister32
    {
        public UInt32 Value { get; set; }
    }
}
