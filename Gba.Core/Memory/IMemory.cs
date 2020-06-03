using System;

namespace Gba.Core
{
    /*
    // NB: Thumb Word is 16 bits
    public interface IThumbMemoryReader
    {
        byte ReadByte(UInt32 address);
        ushort ReadWord(UInt32 address);
    }
    */

    public interface IMemoryReader
    {
        byte ReadByte(UInt32 address);
        ushort ReadHalfWord(UInt32 address);
        UInt32 ReadWord(UInt32 address);
    }

    public interface IMemoryWriter
    {
        void WriteByte(UInt32 address, byte value);
        void WriteHalfWord(UInt32 address, ushort value);
        void WriteWord(UInt32 address, UInt32 value);
    }


    public interface IMemoryReaderWriter : IMemoryReader, IMemoryWriter
    {
    }




}
