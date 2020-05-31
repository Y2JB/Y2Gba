using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{ 
    public class Memory : IArmMemoryReaderWriter
    {
        GameboyAdvance gba;

        byte[] ioReg = new byte[1024];

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
            if(address >= 0x04000000 && address <= 0x040003FE)
            {
                ioReg[address - 0x04000000] = value;
            }
        }


        public void WriteHalfWord(UInt32 address, ushort value)
        {
            WriteByte(address, (byte)(value & 0x00ff));
            WriteByte((ushort)(address + 1), (byte)((value & 0xff00) >> 8));
        }


        public void WriteWord(UInt32 address, UInt32 value)
        {
            WriteByte(address, (byte)(value & 0x00ff));
            WriteByte((ushort)(address + 1), (byte)((value & 0xff00) >> 8));
            WriteByte((ushort)(address + 2), (byte)((value & 0xff0000) >> 16));
            WriteByte((ushort)(address + 3), (byte)((value & 0xff000000) >> 24));
        }



		/****** Checks address before 32-bit reading/writing for special case scenarios ******/
		public void ReadWriteWord_Checked(UInt32 addr, ref UInt32 value, bool load_store)
		{
			//Assume normal operation until a special case occurs
			bool normal_operation = true;

			//Check for special case scenarios for read ops
			if (load_store)
			{
				//Misaligned LDR or SWP
				if (((addr & 0x1)!=0) || ((addr & 0x2)!= 0))
				{
					normal_operation = false;

					//Force alignment by word, then rotate right the read
					byte offset = (byte) ((addr & 0x3) * 8);
					value = ReadWord((UInt32) (addr & ~0x3));
					gba.Cpu.RotateRight(ref value, offset);
				}

				//Out of bounds unused memory
				if ((addr & ~0x3) >= 0x10000000)
				{
					normal_operation = false;

					//Read the opcode instruction at PC
					if (gba.Cpu.State == Cpu.CpuState.Arm) { value = ReadWord(gba.Cpu.R15); }
					else { value = (UInt32)(ReadHalfWord(gba.Cpu.R15) << 16) | ReadHalfWord(gba.Cpu.R15); }
				}

				//Return specific values when trying to read BIOS when PC is not within the BIOS
				if (((addr & ~0x3) <= 0x3FFF) && (gba.Cpu.R15 > 0x3FFF))
				{
					/*
					normal_operation = false;

					switch (bios_read_state)
					{
						case BIOS_STARTUP: value = 0xE129F000; break;
						case BIOS_IRQ_EXECUTE: value = 0xE25EF004; break;
						case BIOS_IRQ_FINISH: value = 0xE55EC002; break;
						case BIOS_SWI_FINISH: value = 0xE3A02004; break;
					}
					*/
				}

				//Special reads to I/O with some bits being unreadable
				switch (addr)
				{
					//Return only the readable halfword for the following addresses

					//DMAxCNT_L
					case 0x40000B8:
					case 0x40000C4:
					case 0x40000D0:
					case 0x40000DC:
						//value = (mem->read_u16_fast(addr + 2) << 16);
						value = (ushort) ((ReadHalfWord(addr + 2) << 16));
						normal_operation = false;
						break;
				}

				//Normal operation
				if (normal_operation) { value = ReadWord(addr); }
			}

			//Check for special case scenarios for write ops
			else
			{
				//Misaligned STR
				if ( ((addr & 0x1)!=0) || ((addr & 0x2)!=0))
				{
					normal_operation = false;

					//Force alignment by word, but that's all, no rotation
					WriteWord((UInt32)(addr & ~0x3), value);
				}

				//Normal operation
				else { WriteWord(addr, value); }
			}
		}
	}
}
