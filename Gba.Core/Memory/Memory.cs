﻿using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Gba.Core
{ 
    public class Memory : IMemoryReaderWriter
    {
        GameboyAdvance gba;

        byte[] ioReg = new byte[1024];

		// External Work Ram
		// Memory transfers to and from EWRAM are 16 bits wide and thus consume more cycles than necessary for 32 bit accesses
		byte[] EWRam = new byte[1024 * 256];

		// Internal Work Ram
		byte[] IWRam = new byte[1024 * 32];

		// VRam 96K
		byte[] vram = new byte[1024 * 96];
		public byte[] VRam { get { return vram; } }

		byte[] oamRam = new byte[1024];
		public byte[] OamRam {  get { return oamRam;  } }


		public Dictionary<UInt32, IMemoryRegister8> IoRegisters8Read { get; private set; }
		public Dictionary<UInt32, IMemoryRegister16> IoRegisters16Read { get; private set; }
		public Dictionary<UInt32, IMemoryRegister32> IoRegisters32Read { get; private set; }

		public Dictionary<UInt32, IMemoryRegister8> IoRegisters8Write { get; private set; }
		public Dictionary<UInt32, IMemoryRegister16> IoRegisters16Write { get; private set; }
		public Dictionary<UInt32, IMemoryRegister32> IoRegisters32Write { get; private set; }

		public Memory(GameboyAdvance gba)
        {
            this.gba = gba;
			
			IoRegisters8Read = new Dictionary<uint, IMemoryRegister8>();
			IoRegisters16Read = new Dictionary<uint, IMemoryRegister16>();
			IoRegisters32Read = new Dictionary<uint, IMemoryRegister32>();

			IoRegisters8Write = new Dictionary<uint, IMemoryRegister8>();
			IoRegisters16Write = new Dictionary<uint, IMemoryRegister16>();
			IoRegisters32Write = new Dictionary<uint, IMemoryRegister32>();
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public byte ReadByte(UInt32 address)
        {			
			// ROM
			//if (address >= 0x08000000 && address <= 0x0DFFFFFF)
			// E???????
			if (address >= 0x08000000 && address <= 0x9FFFEFF)
		    {
				address &= 0x1FFFFFF;
				if (address < gba.Rom.RomSize)
				{
					return gba.Rom.ReadByte(address);
				}
				else
				{
					//throw new NotImplementedException();
					return 0;
					//return open_bus(cpu->pc);
				}				
			}
			// BIOS
			else if (address >= 0x00000000 && address <= 0x00003FFF)
			{
				return gba.Bios.ReadByte(address);
			}
			// IO Registers
			else if (address >= 0x04000000 && address <= 0x040003FE)
			{
				IMemoryRegister8 register;
				if (IoRegisters8Read.TryGetValue(address, out register))
				{
					return register.Value;
				}
				else
				{					
					// TODO: this should throw?
					return ioReg[address - 0x04000000];				
				}
			}
			// Fast Cpu linked RAM
			else if (address >= 0x03000000 && address <= 0x03FFFFFF)
			{
				address &= 0x7FFF;
				return IWRam[address];
			}
			// RAM
			else if (address >= 0x02000000 && address <= 0x02FFFFFF)
			{
				address &= 0x3FFFF;
				return EWRam[address];
			}
			// Palette Ram
			else if (address >= 0x05000000 && address <= 0x050003FF)
			{
				// TODO : Mirroring!
				return gba.LcdController.Palettes.PaletteRam[address - 0x05000000];
			}
			// VRam
			//else if (address >= 0x06000000 && address <= 0x06017FFF)
			else if (address >= 0x06000000 && address <= 0x06FFFFFF)
			{
				address = address & (0x17FFF | (~address & 0x10000) >> 1);
				return vram[address];
			}
			// OAM Ram
			else if (address >= 0x07000000 && address <= 0x07FFFFFF)
			{
				address &= 0x3FF;
				return OamRam[address];
			}

			else if ((address >= 0xD000000) && (address <= 0xDFFFFFF))
            {
				if(address == 0xDFFFF00)
                {
					return 1;
                }
				return 0xFF;
            }

			// Flash & SRAM
			else if (address >= 0x0E000000 && address <= 0x0E00FFFF)
			{
				// SRAM
				if (gba.Rom.SaveGameBackupType == Rom.BackupType.SRAM)
				{
					return gba.Rom.SRam[address - 0xE000000];
				}
				// Flash
				else
				{

					// Specifically to get Pokemon Emerald to run without flash ram support ....
					// In your memory bus, simply return the following values on 8 - bit reads to the specified addresses.
					// 8 - bit read address  Value Meaning
					// 0x0E000000  0x62    Sanyo manufacturer ID
					// 0x0E000001  0x13    Sanyo device ID
					if (address == 0x0E000000) return 0x62;
					else if (address == 0x0E000001) return 0x13;

					// If a game does not support SRAM, it should return 0xFF if an access is attempted. Some games try this as a form of copy protection!
					return 0xFF;				
				}				
			}

			else
			{
				gba.LogMessage(String.Format("BAD MEMORY READ - Unknown address {0:X}", address));
				// Bad memory reads resolve to an Open Bus read. See Gbatek section on 'unpredictable things'
				// The open bus value is resolved in the 'checked' functions below
				return 0;
				/*
				int instructionPtr = gba.Cpu.NextPipelineInsturction + 2;
				if (instructionPtr >= Cpu.Pipeline_Size) instructionPtr -= Cpu.Pipeline_Size;

				if (gba.Cpu.State == Cpu.CpuState.Arm)
				{
					// Accessing unused memory at 00004000h - 01FFFFFFh, and 10000000h - FFFFFFFFh(and 02000000h - 03FFFFFFh when RAM is disabled via 
					// Port 4000800h) returns the recently pre-fetched opcode.For ARM code this is simply:
					// WORD = [$+8]

					// This is untested!					
					UInt32 byteNumber = address % 4;
					byte b = (byte)(gba.Cpu.InstructionPipeline[instructionPtr] >> (int)(byteNumber * 8));
					return (byte) (b & 0xFF);
				}
				else
				{
					// There are multiple cases for resiolving this for thuimb. FOr now i've gone with the main one:
					// For THUMB code in Main RAM, Palette Memory, VRAM, and Cartridge ROM this is:
					// LSW = [$+4], MSW = [$+4]
					// When code is executing in Palette ram (wtf) and other crazy places, we will need to revisit this

					UInt32 byteNumber = address % 2;
					byte b = (byte)(gba.Cpu.InstructionPipeline[instructionPtr] >> (int)(byteNumber * 8));
					return (byte)(b & 0xFF);
				}

				throw new ArgumentException("Bad Memory Read");
				*/
			}
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ushort ReadHalfWord(UInt32 address)
		{
			IMemoryRegister16 register;
			if (IoRegisters16Read.TryGetValue(address, out register))
			{
				return register.Value;
			}
			else
			{
				return (ushort)((ReadByte((UInt32)(address + 1)) << 8) | ReadByte(address));
			}
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public UInt32 ReadWord(UInt32 address)
		{
			IMemoryRegister32 register;
			if (IoRegisters32Read.TryGetValue(address, out register))
			{
				return register.Value;
			}
			else
			{
				return (UInt32)((ReadByte((UInt32)(address + 3)) << 24) | (ReadByte((UInt32)(address + 2)) << 16) | (ReadByte((UInt32)(address + 1)) << 8) | ReadByte(address));
			}
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteByte(UInt32 address, byte value)
		{
			// Fast Cpu linked RAM
			if (address >= 0x03000000 && address <= 0x03FFFFFF)
			{
				address &= 0x7FFF;
				IWRam[address] = value;
			}
			// RAM
			else if (address >= 0x02000000 && address <= 0x02FFFFFF)
			{
				address &= 0x3FFFF;				
				EWRam[address] = value;
			}
			else if (address >= 0x04000000 && address <= 0x040003FE)
			{
				IMemoryRegister8 register;
				if (IoRegisters8Write.TryGetValue(address, out register))
				{
					register.Value = value;
				}
				else
				{
					
					//else if (address == 0x4000204 || address == 0x4000205)
					//{
					// Configure Waitstate
					//	throw new NotImplementedException("Wait State");
					//}
					//else
					{
						ioReg[address - 0x04000000] = value;
						//throw new NotImplementedException();
					}
				}
			}
			// Palette Ram
			//else if (address >= 0x05000000 && address <= 0x050003FF)
			else if (address >= 0x05000000 && address <= 0x05FFFFFF)
			{
				//gba.LogMessage(String.Format("Palette Ram Write Addr {0:X}  Val {1:X}", address, value));
				gba.LcdController.Palettes.UpdatePaletteByte(address - 0x05000000, value);
			}

			// VRam
			//else if (address >= 0x06000000 && address <= 0x06017FFF)
			else if (address >= 0x06000000 && address <= 0x06FFFFFF)
			{
				address = address & (0x17FFF | (~address & 0x10000) >> 1);
				VRam[address] = value;
			}
			// OAM Ram
			else if (address >= 0x07000000 && address <= 0x07FFFFFF)
			{
				address &= 0x3FF;
				OamRam[address] = value;
			}
			else if (address >= 0x08000000 && address <= 0x09FFFFFF)
			{
				// Attempted ROM write...do nothing
				gba.LogMessage(String.Format("INVALID - Attempted ROM Write {0:X}", address));
			}
			else if (address >= 0x0E000000 && address <= 0x0E00FFFF)
			{
				// SRAM
				if (address >= 0xE000000 && address <= 0xE007FFF)
				{
					gba.Rom.SRam[address - 0xE000000] = value;
				}
			}
			else
			{
				gba.LogMessage(String.Format("BAD MEMORY WRITE - Unknown address {0:X}", address));

				//throw new ArgumentException("Bad Memory Write");
			}
		}


		// TODO: 8, 16 & 32 bit reg classes contained in a 8,16,32 bit mem map dictionaries 


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteHalfWord(UInt32 address, ushort value)
		{
			IMemoryRegister16 register;
			if (IoRegisters16Write.TryGetValue(address, out register))
			{				
				register.Value = value;
			}
			else
			{
				WriteByte(address, (byte)(value & 0x00ff));
				WriteByte((address + 1), (byte)((value & 0xff00) >> 8));
			}
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void WriteWord(UInt32 address, UInt32 value)
		{
			IMemoryRegister32 register;
			if (IoRegisters32Write.TryGetValue(address, out register))
			{
				register.Value = value;
			}
			else
			{
				WriteByte(address, (byte)(value & 0x00ff));
				WriteByte((address + 1), (byte)((value & 0xff00) >> 8));
				WriteByte((address + 2), (byte)((value & 0xff0000) >> 16));
				WriteByte((address + 3), (byte)((value & 0xff000000) >> 24));
			}
		}



		/****** Checks address before 32-bit reading/writing for special case scenarios ******/
		public void ReadWriteWord_Checked(UInt32 addr, ref UInt32 value, bool read)
		{
			//Assume normal operation until a special case occurs
			bool normal_operation = true;

			//Check for special case scenarios for read ops
			if (read)
			{
				//Misaligned LDR or SWP
				if (((addr & 0x1) != 0) || ((addr & 0x2) != 0))
				{
					normal_operation = false;

					//Force alignment by word, then rotate right the read
					byte offset = (byte)((addr & 0x3) * 8);
					value = ReadWord((UInt32)(addr & ~0x3));
					gba.Cpu.RotateRight(ref value, offset);
				}

				//Out of bounds unused memory
				if ((addr & ~0x3) >= 0x10000000)
				{
					normal_operation = false;

					// JB: This is simulating open bus

					//Read the opcode instruction at PC
					if (gba.Cpu.State == Cpu.CpuState.Arm) { value = ReadWord(gba.Cpu.PC); }
					else { value = (UInt32)(ReadHalfWord(gba.Cpu.PC) << 16) | ReadHalfWord(gba.Cpu.PC); }
				}

				//Return specific values when trying to read BIOS when PC is not within the BIOS
				if (((addr & ~0x3) <= 0x3FFF) && (gba.Cpu.R15 > 0x3FFF))
				{
					normal_operation = false;

					switch (gba.Bios.State)
					{
						case Bios.Status.STARTUP: value = 0xE129F000; break;
						case Bios.Status.IRQ_EXECUTE: value = 0xE25EF004; break;
						case Bios.Status.IRQ_FINISH: value = 0xE55EC002; break;
						case Bios.Status.SWI_FINISH: value = 0xE3A02004; break;
					}
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
						value = (ushort)((ReadHalfWord(addr + 2) << 16));
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
				if (((addr & 0x1) != 0) || ((addr & 0x2) != 0))
				{
					normal_operation = false;

					//Force alignment by word, but that's all, no rotation
					WriteWord((UInt32)(addr & ~0x3), value);
				}

				//Normal operation
				else { WriteWord(addr, value); }
			}
		}


		/****** Checks address before 16-bit reading/writing for special case scenarios ******/
		public void ReadWriteHalfWord_Checked(UInt32 addr, ref UInt32 value, bool read)
		{
			//Assume normal operation until a special case occurs
			bool normal_operation = true;

			//Check for special case scenarios for read ops
			if (read)
			{
				//Misaligned LDR
				if ((addr & 0x1) != 0)
				{
					normal_operation = false;

					//Force alignment by halfword					
					value = ReadHalfWord((UInt32)(addr & ~0x1));
				}

				//Out of bounds unused memory
				if ((addr & ~0x1) >= 0x10000000)
				{
					normal_operation = false;

					// JB: This is simulating open bus

					//Read the opcode instruction at PC
					value = ReadHalfWord(gba.Cpu.PC);
				}

				//Return 0 for certain readable I/O and Write-Only
				switch (addr)
				{
					case 0x4000010:
					case 0x4000012:
					case 0x4000014:
					case 0x4000016:
					case 0x4000018:
					case 0x400001A:
					case 0x400001C:
					case 0x400001E:
					case 0x4000020:
					case 0x4000022:
					case 0x4000024:
					case 0x4000026:
					case 0x4000028:
					case 0x400002A:
					case 0x400002C:
					case 0x400002E:
					case 0x4000030:
					case 0x4000032:
					case 0x4000034:
					case 0x4000036:
					case 0x4000038:
					case 0x400003A:
					case 0x400003C:
					case 0x400003E:
					case 0x4000040:
					case 0x4000042:
					case 0x4000044:
					case 0x4000046:
					case 0x400004C:
					case 0x400004E:
					case 0x4000054:
					case 0x4000056:
					case 0x4000058:
					case 0x400005A:
					case 0x400005C:
					case 0x400005E:
					case 0x4000066:
					case 0x400006E:
					case 0x4000076:
					case 0x400007A:
					case 0x400007E:
					case 0x4000086:
					case 0x400008A:
					case 0x400008C:
					case 0x400008E:
					case 0x40000A8:
					case 0x40000AA:
					case 0x40000AC:
					case 0x40000AE:
					case 0x40000E0:
					case 0x40000E2:
					case 0x40000E4:
					case 0x40000E6:
					case 0x40000E8:
					case 0x40000EA:
					case 0x40000EC:
					case 0x40000EE:
					case 0x40000F0:
					case 0x40000F2:
					case 0x40000F4:
					case 0x40000F6:
					case 0x40000F8:
					case 0x40000FA:
					case 0x40000FC:
					case 0x40000FE: value = 0; normal_operation = false; break;
				}


				//Return specific values when trying to read BIOS when PC is not within the BIOS
				if (((addr & ~0x1) <= 0x3FFF) && (gba.Cpu.PC > 0x3FFF))
				{
					normal_operation = false;

					switch (gba.Bios.State)
					{
						case Bios.Status.STARTUP: value = 0xF000; break;
						case Bios.Status.IRQ_EXECUTE: value = 0xF004; break;
						case Bios.Status.IRQ_FINISH: value = 0xC002; break;
						case Bios.Status.SWI_FINISH: value = 0x2004; break;
					}
				}


				//Normal operation
				if (normal_operation)
				{
					value = ReadHalfWord(addr);
				}
			}

			//Check for special case scenarios for write ops
			else
			{
				//Misaligned STR
				if ((addr & 0x1) != 0)
				{
					normal_operation = false;

					//Force alignment by word, but that's all, no rotation
					WriteHalfWord((UInt32)(addr & ~0x1), (ushort)value);
				}

				//Normal operation
				else
				{
					WriteHalfWord(addr, (ushort)value);
				}
			}
		}

		/****** Checks address before 8-bit reading/writing for special case scenarios ******/
		public void ReadWriteByte_Checked(UInt32 addr, ref UInt32 value, bool read)
		{
			//Assume normal operation until a special case occurs
			bool normal_operation = true;

			//Check for special case scenarios for read ops
			if (read)
			{
				//Unused 8-bit reads
				if (addr >= 0x10000000)
				{
					// JB: This is simulating open bus
					normal_operation = false;
					value = (gba.Cpu.State == Cpu.CpuState.Arm) ? ReadByte(gba.Cpu.PC + (addr & 0x3)) : ReadByte(gba.Cpu.PC + (addr & 0x1));
				}

				//Return specific values when trying to read BIOS when PC is not within the BIOS
				else if ((addr <= 0x3FFF) && (gba.Cpu.PC > 0x3FFF))
				{
					normal_operation = false;

					switch (gba.Bios.State)
					{
						case Bios.Status.STARTUP: value = 0xE129F000; break;
						case Bios.Status.IRQ_EXECUTE: value = 0xE25EF004; break;
						case Bios.Status.IRQ_FINISH: value = 0xE55EC002; break;
						case Bios.Status.SWI_FINISH: value = 0xE3A02004; break;
					}

					value >>= (int)(8 * (addr & 0x3));
					value &= 0xFF;
				}

				if (normal_operation)
				{
					value = ReadByte(addr);
				}
			}

			//Check for special case scenarios for write ops
			else
			{

				//Check if the address is anywhere near VRAM first
				if ((addr >= 0x5000000) && (addr < 0x8000000))
				{
					throw new ArgumentException("Jon Look into this!");
					// JB From GbaTek:
					// Video Memory (BG, OBJ, OAM, Palette) can be written to in 16bit and 32bit units only. 
					// Attempts to write 8bit data (by STRB opcode) won’t work
					UInt32 bgMode = gba.LcdController.DisplayControlRegister.BgMode;

					//Ignore 8-bit writes to OBJ VRAM (BG Modes 0-2)
					if ((addr >= 0x6010000) && (addr <= 0x6017FFF) && (bgMode <= 2)) { return; }

					//Ignore 8-bit writes to OBJ VRAM (BG Modes 3-5)
					else if ((addr >= 0x6014000) && (addr <= 0x6017FFF) && (bgMode > 2)) { return; }

					//Ignore 8-bit writes to OAM
					else if ((addr >= 0x7000000) && (addr <= 0x70003FF)) { return; }

					/* JB Not sure about these doubled up byte writes!
					//Special write to BG data (BG Modes 0-2)
					else if ((addr >= 0x6000000) && (addr <= 0x600FFFF) && ((mem->memory_map[DISPCNT] & 0x3) <= 2))
					{
						mem->write_u16(addr, ((value << 8) | value));
					}

					//Special write to BG data (BG Modes 3-5)
					else if ((addr >= 0x6000000) && (addr <= 0x6013FFF) && ((mem->memory_map[DISPCNT] & 0x3) > 2))
					{
						mem->write_u16(addr, ((value << 8) | value));
					}

					//Special write to Palette data
					else if ((addr >= 0x5000000) && (addr <= 0x50003FF))
					{
						mem->write_u16(addr, ((value << 8) | value));
					}
				*/
				}

				//Normal operation
				else { WriteByte(addr, (byte)value); }
			}
		}
	}
}
