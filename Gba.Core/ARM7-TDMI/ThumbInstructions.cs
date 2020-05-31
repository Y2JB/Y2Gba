// Full disclosure - I got a huge amount of help and code from Gbe-Plus when writing the CPU emulation

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;


namespace Gba.Core
{

	public partial class Cpu
    {
		public void DecodeAndExecuteThumbInstruction(ushort rawInstruction)
        {
			DecodeThumbInstruction(rawInstruction, false);
		}


		public string PeekThumbInstruction(ushort rawInstruction)
        {
			peekString = "*UNKNOWN_INSTRUCTION*";
			DecodeThumbInstruction(rawInstruction, true);
			return peekString;
        } 


		void DecodeThumbInstruction(ushort rawInstruction, bool peek)
        {		
			if (((rawInstruction >> 13) == 0) && (((rawInstruction >> 11) & 0x7) != 0x3))
			{
				//THUMB_1
				MoveShiftedRegister(rawInstruction, peek);
			}

			else if (((rawInstruction >> 11) & 0x1F) == 0x3)
			{
				//THUMB_2
			}

			else if ((rawInstruction >> 13) == 0x1)
			{
				//THUMB_3
			}

			else if (((rawInstruction >> 10) & 0x3F) == 0x10)
			{
				//THUMB_4
			}

			else if (((rawInstruction >> 10) & 0x3F) == 0x11)
			{
				//THUMB_5
			}

			else if ((rawInstruction >> 11) == 0x9)
			{
				//THUMB_6
				load_pc_relative(rawInstruction, peek);
			}

			else if ((rawInstruction >> 12) == 0x5)
			{
				if ((rawInstruction & 0x200) != 0)
				{
					//THUMB_8
				}

				else
				{
					//THUMB_7
					LoadStoreRegOffset(rawInstruction, peek);
				}
			}

			else if (((rawInstruction >> 13) & 0x7) == 0x3)
			{
				//THUMB_9
			}

			else if ((rawInstruction >> 12) == 0x8)
			{
				//THUMB_10
			}

			else if ((rawInstruction >> 12) == 0x9)
			{
				//THUMB_11
			}

			else if ((rawInstruction >> 12) == 0xA)
			{
				//THUMB_12
			}

			else if ((rawInstruction >> 8) == 0xB0)
			{
				//THUMB_13
			}

			else if ((rawInstruction >> 12) == 0xB)
			{
				//THUMB_14
			}

			else if ((rawInstruction >> 12) == 0xC)
			{
				//THUMB_15
			}

			else if ((rawInstruction >> 12) == 13)
			{
				//THUMB_16
				ConditionalBranch(rawInstruction, peek);
			}

			else if ((rawInstruction >> 11) == 0x1C)
			{
				//THUMB_18
			}

			else if ((rawInstruction >> 11) >= 0x1E)
			{
				//THUMB_19
			}			
		}


		// THUMB.1
		void MoveShiftedRegister(ushort rawInstruction, bool peek)
		{
			// Bits 0-2
			byte destReg = (byte) (rawInstruction & 0x7);

			// Bits 3-5
			byte srcReg = (byte) ((rawInstruction >> 3) & 0x7);

			// Bits 6-10
			byte offset = (byte) ((rawInstruction >> 6) & 0x1F);

			// Bits 11-12
			byte op = (byte) ((rawInstruction >> 11) & 0x3);

			UInt32 result = GetRegisterValue(srcReg);
			byte shiftOut = 0;

			//Shift the register
			switch (op)
			{
				//LSL
				case 0x0:
					if (peek)
					{
						peekString = String.Format("LSL {0},{1} ({2:X})", GetRegisterName(destReg), GetRegisterName(srcReg), offset);
						return;
					}
					shiftOut = LogicalShiftLeft(ref result, offset);
					break;

				//LSR
				case 0x1:
					if (peek)
					{
						peekString = String.Format("LSR {0},{1} ({2:X})", GetRegisterName(destReg), GetRegisterName(srcReg), offset);
						return;
					}
					shiftOut = LogicalShiftRight(ref result, offset);
					break;

				//ASR
				case 0x2:
					if (peek)
					{
						peekString = String.Format("ASR {0},{1} ({2:X})", GetRegisterName(destReg), GetRegisterName(srcReg), offset);
						return;
					}
					shiftOut = ArithmeticShiftRight(ref result, offset);
					break;
			}

			SetRegisterValue(destReg, result);

			// Flags...

			if (result == 0) SetFlag(StatusFlag.Zero);
			else ClearFlag(StatusFlag.Zero);

			if ((result & 0x80000000) != 0) SetFlag(StatusFlag.Negative);
			else ClearFlag(StatusFlag.Negative);

			if (shiftOut == 1) SetFlag(StatusFlag.Carry);
			else if (shiftOut == 0) ClearFlag(StatusFlag.Carry);

			//Clock CPU and controllers - 1S
			//clock(reg.r15, false);
		}


		// THUMB.7 
		void LoadStoreRegOffset(ushort rawInstruction, bool peek)
		{
			// Bits 0-2
			byte srcDestReg = (byte) (rawInstruction & 0x7);

			// Bits 3-5
			byte baseReg = (byte) ((rawInstruction >> 3) & 0x7);

			// Bits 6-8
			byte offsetReg = (byte) ((rawInstruction >> 6) & 0x7);

			// Bits 10-11
			byte op = (byte) ((rawInstruction >> 10) & 0x3);

			UInt32 value = 0;
			UInt32 opAddr = GetRegisterValue(baseReg) + GetRegisterValue(offsetReg);

			//Perform Load-Store ops
			switch (op)
			{
				//STR
				case 0x0:

					if (peek)
					{
						peekString = String.Format("STR {0},{1}", GetRegisterName(srcDestReg), GetRegisterName(opAddr));
						return;
					}

					//Clock CPU and controllers - 1N
					//clock(reg.r15, true);

					//Clock CPU and controllers - 1N
					value = GetRegisterValue(srcDestReg);
					//mem->write_u32(op_addr, value);
					gba.Memory.WriteWord(opAddr, value);
					//clock(op_addr, true);

					break;

				//STRB
				case 0x1:

					if (peek)
					{
						peekString = String.Format("STRB {0},{1}", GetRegisterName(srcDestReg), GetRegisterName(opAddr));
						return;
					}
					//Clock CPU and controllers - 1N
					//clock(reg.r15, true);

					//Clock CPU and controllers - 1N
					value = GetRegisterValue(srcDestReg);
					value &= 0xFF;
					//mem_check_8(op_addr, value, false);
					gba.Memory.WriteByte(opAddr, (byte) value);
					//clock(op_addr, true);

					break;

				//LDR
				case 0x2:

					if (peek)
					{
						peekString = String.Format("LDR {0},[{1:X}]", GetRegisterName(srcDestReg), opAddr);
						return;
					}
					//Clock CPU and controllers - 1N
					//clock(reg.r15, true);

					//Clock CPU and controllers - 1I
					//mem_check_32(op_addr, value, true);
					value = gba.Memory.ReadWord(opAddr);
					//clock();

					//Clock CPU and controllers - 1S
					SetRegisterValue(srcDestReg, value);
					//clock((reg.r15 + 2), false);

					break;

				//LDRB
				case 0x3:

					if (peek)
					{
						peekString = String.Format("LDRB {0},[{1:X}]", GetRegisterName(srcDestReg), opAddr);
						return;
					}
					//Clock CPU and controllers - 1N
					//clock(reg.r15, true);

					//Clock CPU and controllers - 1I
					//mem_check_8(op_addr, value, true);
					value = gba.Memory.ReadByte(opAddr);
					//clock();

					//Clock CPU and controllers - 1S
					SetRegisterValue(srcDestReg, value);
					//clock((reg.r15 + 2), false);

					break;
			}
		}

		// THUMB.6 
		void load_pc_relative(ushort current_thumb_instruction, bool peek)
		{
			// Bits 0-7
			ushort offset = (ushort) (current_thumb_instruction & 0xFF);

			// Bits 8-10
			byte dest_reg = (byte) ((current_thumb_instruction >> 8) & 0x7);

			offset *= 4;
			UInt32 value = 0;
			UInt32 load_addr = (UInt32)((PC & ~0x2) + offset);

			if (peek)
			{
				peekString = String.Format("LDR {0},[{1:X}])", GetRegisterName(dest_reg), load_addr);
				return;
			}

			//Clock CPU and controllers - 1N
			//clock(reg.r15, true);

			//Clock CPU and controllers - 1I
			//mem_check_32(load_addr, value, true);
			value = gba.Memory.ReadWord(load_addr);
			//clock();

			//Clock CPU and controllers - 1S
			SetRegisterValue(dest_reg, value);
			//clock((reg.r15 + 2), false);
		}


		// THUMB.16 
		void ConditionalBranch(ushort rawInstruction, bool peek)
		{
			//Grab 8-bit offset - Bits 0-7
			byte offset = (byte) (rawInstruction & 0xFF);

			//Grab opcode - Bits 8-11
			byte op = (byte) ((rawInstruction >> 8) & 0xF);

			short jump_addr = 0;

			//Calculate jump address, convert 2's Complement
			if ((offset & 0x80) != 0)
			{
				offset--;
				offset = (byte) ~offset;

				jump_addr = (short)(offset * -2);
			}
			else
			{ 
				jump_addr = (short) (offset * 2); 
			}

			//Jump based on condition codes
			switch (op)
			{
				//BEQ
				case 0x0:
					if (peek)
					{
						peekString = String.Format("BEQ");
						return;
					}
					if (ZeroFlag)
                    {
						requestFlushPipeline = true;
					}
					break;

				//BNE
				case 0x1:
					if (peek)
					{
						peekString = String.Format("BNE");
						return;
					}
					if (ZeroFlag == false)
					{
						requestFlushPipeline = true;
					}
					break;

				//BCS
				case 0x2:
					if (peek)
					{
						peekString = String.Format("BCS");
						return;
					}
					if (CarryFlag)
					{
						requestFlushPipeline = true;
					}
					break;

				//BCC
				case 0x3:
					if (peek)
					{
						peekString = String.Format("BCC");
						return;
					}
					if (CarryFlag == false)
					{
						requestFlushPipeline = true;
					}
					break;

				//BMI
				case 0x4:
					if (peek)
					{
						peekString = String.Format("BMI");
						return;
					}
					if (NegativeFlag)
					{
						requestFlushPipeline = true;
					}
					break;

				//BPL
				case 0x5:
					if (peek)
					{
						peekString = String.Format("BPL");
						return;
					}
					if (NegativeFlag == false)
					{
						requestFlushPipeline = true;
					}
					break;

				//BVS
				case 0x6:
					if (peek)
					{
						peekString = String.Format("BVS");
						return;
					}
					if (OverflowFlag)
					{
						requestFlushPipeline = true;
					}
					break;

				//BVC
				case 0x7:
					if (peek)
					{
						peekString = String.Format("BVC");
						return;
					}
					if (OverflowFlag == false)
					{
						requestFlushPipeline = true;
					}
					break;

				//BHI
				case 0x8:
					if (peek)
					{
						peekString = String.Format("BHI");
						return;
					}
					if (CarryFlag && !ZeroFlag)
					{
						requestFlushPipeline = true;
					}
					break;

				//BLS
				case 0x9:
					if (peek)
					{
						peekString = String.Format("BLS");
						return;
					}
					if (ZeroFlag || !CarryFlag)
					{
						requestFlushPipeline = true;
					}
					break;

				//BGE
				case 0xA:
					{
						if (peek)
						{
							peekString = String.Format("BGE");
							return;
						}
						byte n = (byte) ((NegativeFlag) ? 1 : 0);
						byte v = (byte) ((OverflowFlag) ? 1 : 0);

						if (n == v) 
						{
							requestFlushPipeline = true;
						}
					}

					break;

				//BLT
				case 0xB:
					{
						if (peek)
						{
							peekString = String.Format("BLT");
							return;
						}
						byte n = (byte)((NegativeFlag) ? 1 : 0);
						byte v = (byte)((OverflowFlag) ? 1 : 0);

						if (n != v) 
						{
							requestFlushPipeline = true;
						}
					}

					break;

				//BGT
				case 0xC:
					{
						if (peek)
						{
							peekString = String.Format("BGT");
							return;
						}
						byte n = (byte)((NegativeFlag) ? 1 : 0);
						byte v = (byte)((OverflowFlag) ? 1 : 0);
						byte z = (byte)((ZeroFlag) ? 1 : 0);

						if ((z == 0) && (n == v))
						{
							requestFlushPipeline = true;
						}
					}

					break;

				//BLE
				case 0xD:
					{
						if (peek)
						{
							peekString = String.Format("BLE");
							return;
						}
						byte n = (byte)((NegativeFlag) ? 1 : 0);
						byte v = (byte)((OverflowFlag) ? 1 : 0);
						byte z = (byte)((ZeroFlag) ? 1 : 0);

						if ((z == 1) || (n != v))
						{
							requestFlushPipeline = true;
						}
					}

					break;

				//Undefined
				case 0xE:
					throw new InvalidOperationException("Invalid opcode");

				//SWI
				case 0xF:
					throw new NotImplementedException();
					//Process SWIs via HLE
					//TODO: Make and LLE version
					//process_swi((current_thumb_instruction & 0xFF));
					//if (config::use_bios) { return; }
					//break;
			}

			if (requestFlushPipeline)
			{
				//Clock CPU and controllers - 1N
				//clock(reg.r15, true);

				//Clock CPU and controllers - 2S 
				int newPc = (int) (PC + jump_addr);
				PC = (UInt32) newPc;
				//clock(reg.r15, false);
				//clock((reg.r15 + 2), false);
			}

			else
			{
				//Clock CPU and controllers - 1S
				//clock(reg.r15, false);
			}
		}
	}
}
