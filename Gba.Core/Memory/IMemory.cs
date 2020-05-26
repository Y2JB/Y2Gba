using System;

namespace Gba.Core
{
    // NB: Thumb Word is 16 bits
    public interface IThumbMemoryReader
    {
        byte ReadByte(UInt32 address);
        ushort ReadWord(UInt32 address);
    }


    public interface IArmMemoryReader
    {
        byte ReadByte(UInt32 address);
        ushort ReadHalfWord(UInt32 address);
        UInt32 ReadWord(UInt32 address);
    }

    public interface IArmMemoryWriter
    {
        void WriteByte(UInt32 address, byte value);
        void WriteHalfWord(UInt32 address, ushort value);
        void WriteWord(UInt32 address, UInt32 value);
    }


    public interface IArmMemoryReaderWriter : IArmMemoryReader, IArmMemoryWriter
    {
    }




}
