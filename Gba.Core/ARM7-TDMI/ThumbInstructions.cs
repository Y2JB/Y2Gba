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
				AddSubImmediate(rawInstruction, peek);
			}

			else if ((rawInstruction >> 13) == 0x1)
			{
				//THUMB_3
				MCASImmediate(rawInstruction, peek);
			}

			else if (((rawInstruction >> 10) & 0x3F) == 0x10)
			{
				//THUMB_4
				AluOps(rawInstruction, peek);
			}

			else if (((rawInstruction >> 10) & 0x3F) == 0x11)
			{
				//THUMB_5
				HiregAndBx(rawInstruction, peek);
			}

			else if ((rawInstruction >> 11) == 0x9)
			{
				//THUMB_6
				LoadPcRelative(rawInstruction, peek);
			}

			else if ((rawInstruction >> 12) == 0x5)
			{
				if ((rawInstruction & 0x200) != 0)
				{
					//THUMB_8
					throw new NotImplementedException();
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
				LoadStoreImmediateOffset(rawInstruction, peek);
			}

			else if ((rawInstruction >> 12) == 0x8)
			{
				//THUMB_10
				if (!peek) throw new NotImplementedException();
			}

			else if ((rawInstruction >> 12) == 0x9)
			{
				//THUMB_11
				if (!peek) throw new NotImplementedException();
			}

			else if ((rawInstruction >> 12) == 0xA)
			{
				//THUMB_12
				if (!peek) throw new NotImplementedException();
			}

			else if ((rawInstruction >> 8) == 0xB0)
			{
				//THUMB_13
				if (!peek) throw new NotImplementedException();
			}

			else if ((rawInstruction >> 12) == 0xB)
			{
				//THUMB_14
				PushPopRegisters(rawInstruction, peek);
			}

			else if ((rawInstruction >> 12) == 0xC)
			{
				//THUMB_15
				MultipleLoadStore(rawInstruction, peek);
			}

			else if ((rawInstruction >> 12) == 13)
			{
				//THUMB_16
				ConditionalBranch(rawInstruction, peek);
			}

			else if ((rawInstruction >> 11) == 0x1C)
			{
				//THUMB_18
				throw new NotImplementedException();
			}

			else if ((rawInstruction >> 11) >= 0x1E)
			{
				//THUMB_19
				LongBranchLink(rawInstruction, peek);
			}
			else
			{
				if (!peek) throw new ArgumentException("Unable to decode Thumb Instrction");
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
						peekString = String.Format("LSL {0},{1} (${2:X})", GetRegisterName(destReg), GetRegisterName(srcReg), offset);
						return;
					}
					shiftOut = LogicalShiftLeft(ref result, offset);
					break;

				//LSR
				case 0x1:
					if (peek)
					{
						peekString = String.Format("LSR {0},{1} (${2:X})", GetRegisterName(destReg), GetRegisterName(srcReg), offset);
						return;
					}
					shiftOut = LogicalShiftRight(ref result, offset);
					break;

				//ASR
				case 0x2:
					if (peek)
					{
						peekString = String.Format("ASR {0},{1} (${2:X})", GetRegisterName(destReg), GetRegisterName(srcReg), offset);
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

		// THUMB.2
		//__2_|_0___0___0___1___1_|_I,_Op_|___Rn/nn___|____Rs_____|____Rd_____|ADD/SUB
		void AddSubImmediate(ushort rawInstruction, bool peek)
		{
			//Grab destination register - Bits 0-2
			byte destReg = (byte) (rawInstruction & 0x7);

			//Grab source register - Bits 3-5
			byte srcReg = (byte) ((rawInstruction >> 3) & 0x7);

			//Grab the opcode - Bits 9-10
			byte op = (byte) ((rawInstruction >> 9) & 0x3);

			UInt32 input = GetRegisterValue(srcReg);
			UInt32 result = 0;
			UInt32 operand = 0;
			byte immReg = (byte) ((rawInstruction >> 6) & 0x7);

			//Perform addition or subtraction
			switch (op)
			{
				//Add with register as operand
				case 0x0:
					if (peek)
					{
						peekString = String.Format("ADD {0},{1}", GetRegisterName(destReg), GetRegisterName(immReg));
						return;
					}
					operand = GetRegisterValue(immReg);
					result = input + operand;
					break;

				//Subtract with register as operand
				case 0x1:
					if (peek)
					{
						peekString = String.Format("SUB {0},{1}", GetRegisterName(destReg), GetRegisterName(immReg));
						return;
					}
					operand = GetRegisterValue(immReg);
					result = input - operand;
					break;

				//Add with 3-bit immediate as operand
				case 0x2:
					if (peek)
					{
						peekString = String.Format("ADD {0},${1:X}", GetRegisterName(destReg), immReg);
						return;
					}
					operand = immReg;
					result = input + operand;
					break;

				//Subtract with 3-bit immediate as operand
				case 0x3:
					if (peek)
					{
						peekString = String.Format("SUB {0},${1:X}", GetRegisterName(destReg), immReg);
						return;
					}
					operand = immReg;
					result = input - operand;
					break;
			}

			SetRegisterValue(destReg, result);

			if ((op & 0x1) != 0) { UpdateFlagsForArithmeticOps(input, operand, result, false); }
			else { UpdateFlagsForArithmeticOps(input, operand, result, true); }

			//Clock CPU and controllers - 1S
			//clock(reg.r15, false);
		}



		// THUMB.4 
		void AluOps(ushort rawInstruction, bool peek)
		{
			//Grab destination register - Bits 0-2
			byte destReg = (byte) (rawInstruction & 0x7);

			//Grab source register - Bits 3-5
			byte srcReg = (byte) ((rawInstruction >> 3) & 0x7);

			//Grab opcode - Bits 6-9
			byte op = (byte) ((rawInstruction >> 6) & 0xF);

			UInt32 input = GetRegisterValue(destReg); //Still looks weird, but same as in THUMB.3
			UInt32 result = 0;
			UInt32 operand = GetRegisterValue(srcReg);
			byte shiftOut = 0;
			byte carryOut = (byte) (CarryFlag ? 1 : 0);

			//Perform ALU operations
			switch (op)
			{
				//AND
				case 0x0:
					if (peek)
					{
						peekString = String.Format("AND {0},{1}", GetRegisterName(destReg), GetRegisterName(srcReg));
						return;
					}

					result = (input & operand);

					if (result == 0) SetFlag(StatusFlag.Zero);
					else ClearFlag(StatusFlag.Zero);

					if ((result & 0x80000000) != 0) SetFlag(StatusFlag.Negative);
					else ClearFlag(StatusFlag.Negative);

					SetRegisterValue(destReg, result);

					//Clock CPU and controllers - 1S
					//clock(reg.r15, false);

					break;

				//XOR
				case 0x1:
					if (peek)
					{
						peekString = String.Format("EOR {0},{1}", GetRegisterName(destReg), GetRegisterName(srcReg));
						return;
					}

					result = (input ^ operand);

					if (result == 0) SetFlag(StatusFlag.Zero);
					else ClearFlag(StatusFlag.Zero);

					if ((result & 0x80000000) != 0) SetFlag(StatusFlag.Negative);
					else ClearFlag(StatusFlag.Negative);

					SetRegisterValue(destReg, result);

					//Clock CPU and controllers - 1S
					//clock(reg.r15, false);

					break;

				//LSL
				case 0x2:
					if (peek)
					{
						peekString = String.Format("LSL {0},{1}", GetRegisterName(destReg), GetRegisterName(srcReg));
						return;
					}

					operand &= 0xFF;
					if (operand != 0) { shiftOut = LogicalShiftLeft(ref input, (byte) operand); }
					result = input;

					if (result == 0) SetFlag(StatusFlag.Zero);
					else ClearFlag(StatusFlag.Zero);

					if ((result & 0x80000000) != 0) SetFlag(StatusFlag.Negative);
					else ClearFlag(StatusFlag.Negative);

					if (operand != 0)
					{
						if (shiftOut == 1) SetFlag(StatusFlag.Carry);
						else if (shiftOut == 0) ClearFlag(StatusFlag.Carry);
					}

					//Clock CPU and controllers - 1I
					//clock();

					SetRegisterValue(destReg, result);

					//Clock CPU and controllers - 1S
					//clock((reg.r15 + 2), false);

					break;

				//LSR
				case 0x3:
					if (peek)
					{
						peekString = String.Format("LSR {0},{1}", GetRegisterName(destReg), GetRegisterName(srcReg));
						return;
					}

					operand &= 0xFF;
					if (operand != 0) { shiftOut = LogicalShiftRight(ref input, (byte) operand); }
					result = input;

					if (result == 0) SetFlag(StatusFlag.Zero);
					else ClearFlag(StatusFlag.Zero);

					if ((result & 0x80000000) != 0) SetFlag(StatusFlag.Negative);
					else ClearFlag(StatusFlag.Negative);

					if (operand != 0)
					{
						if (shiftOut == 1) SetFlag(StatusFlag.Carry);
						else if (shiftOut == 0) ClearFlag(StatusFlag.Carry);
					}

					//Clock CPU and controllers - 1I
					//clock();

					SetRegisterValue(destReg, result);

					//Clock CPU and controllers - 1S
					//clock((reg.r15 + 2), false);

					break;

				//ASR
				case 0x4:
					if (peek)
					{
						peekString = String.Format("ASR {0},{1}", GetRegisterName(destReg), GetRegisterName(srcReg));
						return;
					}

					operand &= 0xFF;
					if (operand != 0) { shiftOut = ArithmeticShiftRight(ref input, (byte) operand); }
					result = input;

					if (result == 0) SetFlag(StatusFlag.Zero);
					else ClearFlag(StatusFlag.Zero);

					if ((result & 0x80000000) != 0) SetFlag(StatusFlag.Negative);
					else ClearFlag(StatusFlag.Negative);

					if (operand != 0)
					{
						if (shiftOut == 1) SetFlag(StatusFlag.Carry);
						else if (shiftOut == 0) ClearFlag(StatusFlag.Carry);
					}

					//Clock CPU and controllers - 1I
					//clock();

					SetRegisterValue(destReg, result);

					//Clock CPU and controllers - 1S
					//clock((reg.r15 + 2), false);

					break;

				//ADC
				case 0x5:
					if (peek)
					{
						peekString = String.Format("ADC {0},{1}", GetRegisterName(destReg), GetRegisterName(srcReg));
						return;
					}

					result = (input + operand + carryOut);
					UpdateFlagsForArithmeticOps(input, operand, result, true);

					SetRegisterValue(destReg, result);

					//Clock CPU and controllers - 1S
					//clock(reg.r15, false);

					break;

				//SBC
				case 0x6:
					if (peek)
					{
						peekString = String.Format("SBC {0},{1}", GetRegisterName(destReg), GetRegisterName(srcReg));
						return;
					}

					//Invert (NOT) carry
					if (carryOut != 0) 
					{ 
						carryOut = 0; 
					}
					else 
					{ 
						carryOut = 1; 
					}

					result = (input - operand - carryOut);
					UpdateFlagsForArithmeticOps(input, operand, result, false);

					SetRegisterValue(destReg, result);

					//Clock CPU and controllers - 1S
					//clock(reg.r15, false);

					break;

				//ROR
				case 0x7:
					if (peek)
					{
						peekString = String.Format("ROR {0},{1}", GetRegisterName(destReg), GetRegisterName(srcReg));
						return;
					}

					operand &= 0xFF;
					if (operand != 0) { shiftOut = RotateRight(ref input, (byte) operand); }
					result = input;

					if (result == 0) SetFlag(StatusFlag.Zero);
					else ClearFlag(StatusFlag.Zero);

					if ((result & 0x80000000) != 0) SetFlag(StatusFlag.Negative);
					else ClearFlag(StatusFlag.Negative);

					if (operand != 0)
					{
						if (shiftOut == 1) SetFlag(StatusFlag.Carry);
						else if (shiftOut == 0) ClearFlag(StatusFlag.Carry);
					}

					//Clock CPU and controllers - 1I
					//clock();

					SetRegisterValue(destReg, result);

					//Clock CPU and controllers - 1S
					//clock(reg.r15, false);

					break;

				//TST
				case 0x8:
					if (peek)
					{
						peekString = String.Format("TST {0},{1}", GetRegisterName(destReg), GetRegisterName(srcReg));
						return;
					}

					result = (input & operand);

					if (result == 0) SetFlag(StatusFlag.Zero);
					else ClearFlag(StatusFlag.Zero);

					if ((result & 0x80000000) != 0) SetFlag(StatusFlag.Negative);
					else ClearFlag(StatusFlag.Negative);

					//Clock CPU and controllers - 1S
					//clock(reg.r15, false);

					break;

				//NEG
				case 0x9:
					if (peek)
					{
						peekString = String.Format("NEG {0},{1}", GetRegisterName(destReg), GetRegisterName(srcReg));
						return;
					}

					input = 0;
					result = (input - operand);
					UpdateFlagsForArithmeticOps(input, operand, result, false);

					SetRegisterValue(destReg, result);

					//Clock CPU and controllers - 1S
					//clock(reg.r15, false);

					break;

				//CMP
				case 0xA:
					if (peek)
					{
						peekString = String.Format("CMP {0},{1}", GetRegisterName(destReg), GetRegisterName(srcReg));
						return;
					}

					result = (input - operand);

					UpdateFlagsForArithmeticOps(input, operand, result, false);

					//Clock CPU and controllers - 1S
					//clock(reg.r15, false);

					break;

				//CMN
				case 0xB:
					if (peek)
					{
						peekString = String.Format("CMN {0},{1}", GetRegisterName(destReg), GetRegisterName(srcReg));
						return;
					}

					result = (input + operand);
					UpdateFlagsForArithmeticOps(input, operand, result, true);

					//Clock CPU and controllers - 1S
					//clock(reg.r15, false);

					break;

				//ORR
				case 0xC:
					if (peek)
					{
						peekString = String.Format("ORR {0},{1}", GetRegisterName(destReg), GetRegisterName(srcReg));
						return;
					}

					result = (input | operand);

					if (result == 0) SetFlag(StatusFlag.Zero);
					else ClearFlag(StatusFlag.Zero);

					if ((result & 0x80000000) != 0) SetFlag(StatusFlag.Negative);
					else ClearFlag(StatusFlag.Negative);

					SetRegisterValue(destReg, result);

					//Clock CPU and controllers - 1S
					//clock(reg.r15, false);

					break;

				//MUL
				case 0xD:
					if (peek)
					{
						peekString = String.Format("MUL {0},{1}", GetRegisterName(destReg), GetRegisterName(srcReg));
						return;
					}

					result = (input * operand);

					if (result == 0) SetFlag(StatusFlag.Zero);
					else ClearFlag(StatusFlag.Zero);

					if ((result & 0x80000000) != 0) SetFlag(StatusFlag.Negative);
					else ClearFlag(StatusFlag.Negative);

					// TODO: Figure out what the carry flag should be for this opcode.
					// TODO: Figure out the timing for this opcode
					// XXXXCLOCKXXXX
					SetRegisterValue(destReg, result);
					break;

				//BIC
				case 0xE:
					if (peek)
					{
						peekString = String.Format("BIC {0},{1}", GetRegisterName(destReg), GetRegisterName(srcReg));
						return;
					}

					result = (input & ~operand);

					if (result == 0) SetFlag(StatusFlag.Zero);
					else ClearFlag(StatusFlag.Zero);

					if ((result & 0x80000000) != 0) SetFlag(StatusFlag.Negative);
					else ClearFlag(StatusFlag.Negative);

					SetRegisterValue(destReg, result);

					//Clock CPU and controllers - 1S
					//clock(reg.r15, false);

					break;

				//MVN
				case 0xF:
					if (peek)
					{
						peekString = String.Format("MVN {0},{1}", GetRegisterName(destReg), GetRegisterName(srcReg));
						return;
					}

					result = ~operand;

					if (result == 0) SetFlag(StatusFlag.Zero);
					else ClearFlag(StatusFlag.Zero);

					if ((result & 0x80000000) != 0) SetFlag(StatusFlag.Negative);
					else ClearFlag(StatusFlag.Negative);

					SetRegisterValue(destReg, result);

					//Clock CPU and controllers - 1S
					//clock(reg.r15, false);

					break;
			}
		}


		// THUMB.3 
		// __3_|_0___0___1_|__Op___|____Rd_____|_____________Offset____________|Immedi.
		// Mov, Cmp, Add, Subtract - immediate
		void MCASImmediate(ushort rawInstruction, bool peek)
		{
			// Bits 8-10
			byte destReg = (byte) ((rawInstruction >> 8) & 0x7);

			// Bits 11-12
			byte op = (byte) ((rawInstruction >> 11) & 0x3);

			UInt32 input = GetRegisterValue(destReg); //Looks weird but the source is also the destination in this instruction
			UInt32 result = 0;

			// Operand is an 8-bit immediate
			UInt32 operand = (UInt32) (rawInstruction & 0xFF);

			switch (op)
			{
				// MOV
				case 0x0:
					if (peek)
					{
						peekString = String.Format("MOV {0},${1:X}", GetRegisterName(destReg), operand);
						return;
					}

					result = operand;

					// Flags...

					if (result == 0) SetFlag(StatusFlag.Zero);
					else ClearFlag(StatusFlag.Zero);

					if ((result & 0x80000000) != 0) SetFlag(StatusFlag.Negative);
					else ClearFlag(StatusFlag.Negative);

					break;

				// CMP
				case 0x1:
					if (peek)
					{
						peekString = String.Format("CMP ${0:X}", operand);
						return;
					}
					result = (input - operand);
					UpdateFlagsForArithmeticOps(input, operand, result, false);
					break;

				// SUB
				case 0x3:
					if (peek)
					{
						peekString = String.Format("SUB {0},${1:X}", GetRegisterName(destReg), operand);
						return;
					}
					result = (input - operand);
					UpdateFlagsForArithmeticOps(input, operand, result, false);
					break;

				// ADD
				case 0x2:
					if (peek)
					{
						peekString = String.Format("ADD {0},${1:X}", GetRegisterName(destReg), operand);
						return;
					}
					result = (input + operand);
					UpdateFlagsForArithmeticOps(input, operand, result, true);
					break;
			}

			// Do not update the destination register if CMP is the operation!
			if (op != 1) SetRegisterValue(destReg, result);

			//Clock CPU and controllers - 1S
			//clock(reg.r15, false);
		}


		// THUMB.5 High Register Operations + Branch Exchange 
		// __5_|_0___1___0___0___0___1_|__Op___|Hd_|Hs_|____Rs_____|____Rd_____|HiReg/BX
		void HiregAndBx(ushort current_thumb_instruction, bool peek)
		{
			// Bits 0-2
			byte destReg = (byte) (current_thumb_instruction & 0x7);

			// Bits 3-5
			byte srcReg = (byte) ((current_thumb_instruction >> 3) & 0x7);

			// Source register MSB - Bit 6
			byte srMsb = (byte) ((current_thumb_instruction & 0x40) != 0 ? 1 : 0);

			// Destination register MSB - Bit 7
			byte drMsb = (byte) ((current_thumb_instruction & 0x80) != 0 ? 1 : 0);

			// Add MSB to source and destination registers
			if (srMsb != 0) srcReg |= 0x8;
			if (drMsb != 0) destReg |= 0x8;

			byte op = (byte) ((current_thumb_instruction >> 8) & 0x3);

			UInt32 input = GetRegisterValue(destReg); //Still looks weird, but same as in THUMB.3
			UInt32 result = 0;
			UInt32 operand = GetRegisterValue(srcReg);

			if ((op == 3) && (drMsb != 0))
			{
				throw new ArgumentException("THUMB.5 Using BX but MSBd is set");	
			}

			// Perform ops or branch - Only CMP affects flags!
			switch (op)
			{
				// ADD
				case 0x0:

					if (peek)
					{
						peekString = String.Format("ADD {0},${1:X}", GetRegisterName(destReg), operand);
						return;
					}

					// Destination is not PC
					if (destReg != 15)
					{
						result = (input + operand);
						SetRegisterValue(destReg, result);

						//Clock CPU and controllers - 1S
						//clock(reg.r15, false);
					}

					// Destination is PC
					else
					{
						// When the destination register is the PC, auto-align operand to half-word
						operand &= ~0x1U;

						//Clock CPU and controllers - 1N
						//clock(reg.r15, true);

						result = (input + operand);
						SetRegisterValue(destReg, result);
						requestFlushPipeline = true;

						//Clock CPU and controllers - 2S
						//clock(reg.r15, false);
						//clock((reg.r15 + 2), false);
					}
					break;

				// CMP
				case 0x1:

					if (peek)
					{
						peekString = String.Format("CMP ${0:X}", operand);
						return;
					}

					result = (input - operand);
					UpdateFlagsForArithmeticOps(input, operand, result, false);

					//Clock CPU and controllers - 1S
					//clock(reg.r15, false);

					break;

				// MOV
				case 0x2:
					if (peek)
					{
						peekString = String.Format("MOV {0},${1:X}", GetRegisterName(destReg), operand);
						return;
					}

					// Operand is not PC
					if (destReg != 15)
					{
						result = operand;
						SetRegisterValue(destReg, result);

						//Clock CPU and controllers - 1S
						//clock(reg.r15, false);
					}

					// Operand is PC
					else
					{
						// When the destination register is the PC, auto-align operand to half-word
						operand &= ~0x1U;

						//Clock CPU and controllers - 1N
						//clock(reg.r15, true);

						result = operand;
						SetRegisterValue(destReg, result);
						requestFlushPipeline = true;

						//Clock CPU and controllers - 2S
						//clock(reg.r15, false);
						//clock((reg.r15 + 2), false);
					}

					break;

				// BX
				case 0x3:
					if (peek)
					{
						peekString = String.Format("BX ${0:X}", operand);
						return;
					}
					// Switch to ARM mode if necessary
					if ((operand & 0x1) == 0)
					{
						State = CpuState.Arm;

						ClearFlag(StatusFlag.ThumbExecution);
						operand &= ~0x3U;
					}
					//Align operand to half-word
					else 
					{ 
						operand &= ~0x1U; 
					}

					//Clock CPU and controllers - 1N
					//clock(reg.r15, true);

					// Auto-align PC when using R15 as an operand
					if (srcReg == 15)
					{
						PC &= ~0x2U;
					}
					else 
					{ 
						PC = operand; 
					}

					//Clock CPU and controllers - 2S
					//clock(reg.r15, false);
					//clock((reg.r15 + 2), false);

					requestFlushPipeline = true;
					break;
			}
		}


		// THUMB.6 
		void LoadPcRelative(ushort current_thumb_instruction, bool peek)
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
				peekString = String.Format("LDR {0},[${1:X}])", GetRegisterName(dest_reg), load_addr);
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


		// THUMB.14
		//  _14_|_1___0___1___1_|Op_|_1___0_|_R_|____________Rlist______________|PUSH/POP
		void PushPopRegisters(ushort rawInstruction, bool peek)
		{
			// Get stack pointer & Link Regoster from current CPU mode
			UInt32 sp = SP;
			UInt32 lr = LR;

			// Bits 0-7 List of registers as a bitfield 
			byte registerList = (byte) (rawInstruction & 0xFF);

			// Bit 8
			bool pcLr = ((rawInstruction & 0x100)!=0) ? true : false;

			// Bit 11
			byte op = (byte) (((rawInstruction & 0x800)!=0) ? 1 : 0);

			byte nCount = 0;

			for (int x = 0; x < 8; x++)
			{
				if (((registerList >> x) & 0x1) != 0) 
				{ 
					nCount++; 
				}
			}

			//Perform push-pop ops
			switch (op)
			{
				// PUSH
				case 0x0:
					if (peek)
					{
						peekString = String.Format("PUSH {0}", RegisterListToString(registerList, 8));
						return;
					}

					//Clock CPU and controllers - 1N
					//clock(reg.r15, true);

					//Optionally store LR onto the stack
					if (pcLr)
					{
						sp -= 4;
						//mem_check_32(r13, lr, false);
						gba.Memory.WriteWord(sp, lr);
						LR = lr;

						//Clock CPU and controllers - 1S
						//clock(r13, false);
					}

					//Cycle through the register list
					for (int x = 7; x >= 0; x--)
					{
						if ((registerList & (1 << x)) != 0)
						{
							sp -= 4;
							UInt32 push_value = GetRegisterValue((UInt32)x);
							//mem_check_32(r13, push_value, false);
							gba.Memory.WriteWord(sp, push_value);

							//Clock CPU and controllers - (n)S
							if ((nCount - 1) != 0) 
							{ 
								//clock(r13, false); 
								nCount--; 
							}

							//Clock CPU and controllers - 1N
							else 
							{ 
								//clock(r13, true); x = 10; 
								break; 
							}
						}
					}

					break;

				// POP
				case 0x1:
					if (peek)
					{
						peekString = String.Format("POP {0}", RegisterListToString(registerList, 8));
						return;
					}

					//Clock CPU and controllers - 1N
					//clock(reg.r15, true);

					//Cycle through the register list
					for (int x = 0; x < 8; x++)
					{
						if ((registerList & 0x1) != 0)
						{
							UInt32 pop_value = 0;
							//mem_check_32(r13, pop_value, true);
							pop_value = gba.Memory.ReadWord(sp);
							SetRegisterValue((UInt32)x, pop_value);
							sp += 4;

							//Clock CPU and controllers - (n)S
							if (nCount > 1) 
							{ 
								//clock(r13, false); 
							}
						}

						registerList >>= 1;
					}

					//Optionally load PC from the stack
					if (pcLr)
					{
						//Clock CPU and controllers - 1I
						//clock();

						//Clock CPU and controllers - 1N
						//clock(reg.r15, true);

						//Clock CPU and controllers - 2S
						//mem_check_32(r13, reg.r15, true);
						PC = gba.Memory.ReadWord(sp);
						PC &= ~0x1U;
						sp += 4;
						requestFlushPipeline = true;

						//clock(reg.r15, false);
						//clock((reg.r15 + 2), false);
					}

					//If PC not loaded, last cycles are Internal then Sequential
					else
					{
						//Clock CPU and controllers - 1I
						//clock();

						//Clock CPU and controllers - 1S
						//clock((reg.r15 + 2), false);
					}

					break;
			}

			//Update stack pointer for current CPU mode
			SP = sp;
		}


		// THUMB.15
		// _15_|_1___1___0___0_|Op_|____Rb_____|____________Rlist______________|STM/LDM
		void MultipleLoadStore(ushort rawInstruction, bool peek)
		{
			// Register list - Bits 0-7
			byte registerList = (byte) (rawInstruction & 0xFF);

			// Bits 8-10
			byte baseReg = (byte) ((rawInstruction >> 8) & 0x7);

			// Bit 11
			byte op = (byte) (((rawInstruction & 0x800)!=0) ? 1 : 0);

			UInt32 baseAddr = GetRegisterValue(baseReg);
			UInt32 regValue = 0;
			byte nCount = 0;

			UInt32 oldBase = baseAddr;
			byte transferReg = 0xFF;
			bool writeBack = true;

			// Determine the first register in the Register List
			for (int x = 0; x < 8; x++)
			{
				if ((registerList & (1 << x)) != 0)
				{
					transferReg = (byte) x;
					x = 0xFF;
					break;
				}
			}


			// Get n_count
			for (int x = 0; x < 8; x++)
			{
				if (((registerList >> x) & 0x1) != 0) 
				{ 
					nCount++; 
				}
			}

			// Perform multi load-store ops
			switch (op)
			{
				//STMIA
				case 0x0:
					//If register list is not empty, store normally
					if (registerList != 0)
					{
						if (peek)
						{
							peekString = String.Format("STMIA {0}! {1}", GetRegisterName(baseReg), RegisterListToString(registerList, 8));
							return;
						}

						//Clock CPU and controllers - 1N
						//clock(reg.r15, true);

						//Cycle through the register list
						for (UInt32 x = 0; x < 8; x++)
						{
							if ((registerList & 0x1) != 0)
							{
								regValue = GetRegisterValue(x);

								//if ((x == transfer_reg) && (base_reg == transfer_reg)) { mem_check_32(base_addr, old_base, false); }
								//else { mem_check_32(base_addr, reg_value, false); }

								if ((x == transferReg) && (baseReg == transferReg))
								{
									gba.Memory.WriteWord(baseAddr, oldBase);
								}
								else
								{
									gba.Memory.WriteWord(baseAddr, regValue);
								}

								// Update base register
								baseAddr += 4;
								SetRegisterValue(baseReg, baseAddr);

								//Clock CPU and controllers - (n)S
								//if ((n_count - 1) != 0) { clock(base_addr, false); n_count--; }

								//Clock CPU and controllers - 1N
								//else { clock(base_addr, true); x = 10; break; }
							}

							registerList >>= 1;
						}
					}

					//Special case with empty list
					else
					{
						if (peek)
						{
							peekString = String.Format("STMIA {0}! (EMPTY)", GetRegisterName(baseReg));
							return;
						}

						//Store PC, then add 0x40 to base register
						//mem_check_32(base_addr, R15, false);
						gba.Memory.WriteWord(baseAddr, PC);
						baseAddr += 0x40;
						SetRegisterValue(baseReg, baseAddr);

						//Clock CPU and controllers - ???
						//TODO - find out what to do here...
					}

					break;

				//LDMIA
				case 0x1:
					//If register list is not empty, load normally
					if (registerList != 0)
					{
						if (peek)
						{
							peekString = String.Format("LDMIA {0}! {1}", GetRegisterName(baseReg), RegisterListToString(registerList, 8));
							return;
						}

						//Clock CPU and controllers - 1N
						//clock(reg.r15, true);

						// Walk the register list bitmask
						for (UInt32 x = 0; x < 8; x++)
						{
							if ((registerList & 1) != 0)
							{
								if ((x == transferReg) && (baseReg == transferReg)) { writeBack = false; }

								//mem_check_32(base_addr, reg_value, true);
								regValue = gba.Memory.ReadWord(baseAddr);
								SetRegisterValue(x, regValue);

								// Update base register
								baseAddr += 4;
								if (writeBack) 
								{ 
									SetRegisterValue(baseReg, baseAddr); 
								}

								//Clock CPU and controllers - (n)S
								//if (n_count > 1) { clock(base_addr, false); }
							}

							registerList >>= 1;
						}

						//Clock CPU and controllers - 1I
						//clock();

						//Clock CPU and controllers - 1S
						//clock((reg.r15 + 2), false);
					}

					//Special case with empty list
					else
					{
						if (peek)
						{
							peekString = String.Format("LDMIA {0}! (EMPTY)", GetRegisterName(baseReg));
							return;
						}

						//Load PC, then add 0x40 to base register
						//mem_check_32(base_addr, reg.r15, true);
						PC = gba.Memory.ReadWord(baseAddr);
						baseAddr += 0x40;
						SetRegisterValue(baseReg, baseAddr);

						//Clock CPU and controllers - ???
						//TODO - find out what to do here...
					}

					break;
			}
		}


		// THUMB.7 
		// __7_|_0___1___0___1_|__Op___|_0_|___Ro______|____Rb_____|____Rd_____|LDR/STR
		void LoadStoreRegOffset(ushort rawInstruction, bool peek)
		{
			// Bits 0-2
			byte srcDestReg = (byte)(rawInstruction & 0x7);

			// Bits 3-5
			byte baseReg = (byte)((rawInstruction >> 3) & 0x7);

			// Bits 6-8
			byte offsetReg = (byte)((rawInstruction >> 6) & 0x7);

			// Bits 10-11
			byte op = (byte)((rawInstruction >> 10) & 0x3);

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
					gba.Memory.WriteByte(opAddr, (byte)value);
					//clock(op_addr, true);

					break;

				//LDR
				case 0x2:
					if (peek)
					{
						peekString = String.Format("LDR {0},[${1:X}]", GetRegisterName(srcDestReg), opAddr);
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
						peekString = String.Format("LDRB {0},[${1:X}]", GetRegisterName(srcDestReg), opAddr);
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


		// THUMB.9
		// __9_|_0___1___1_|__Op___|_______Offset______|____Rb_____|____Rd_____|""{B}
		void LoadStoreImmediateOffset(ushort rawInstruction, bool peek)
		{
			// Bits 0-2
			byte srcDestReg = (byte) (rawInstruction & 0x7);

			// Bits 3-5
			byte baseReg = (byte) ((rawInstruction >> 3) & 0x7);

			// Bits 6-10
			ushort offset = (byte)((rawInstruction >> 6) & 0x1F);

			// Bits 11-12
			byte op = (byte) ((rawInstruction >> 11) & 0x3);

			UInt32 value = 0;
			UInt32 opAddr = GetRegisterValue(baseReg);

			switch (op)
			{
				// STR
				case 0x0:
					//Clock CPU and controllers - 1N
					value = GetRegisterValue(srcDestReg);
					offset <<= 2;
					opAddr += offset;
					//clock(reg.r15, true);

					if (peek)
					{
						peekString = String.Format("STR {0}, [{1} + ${2}]", GetRegisterName(srcDestReg), GetRegisterName(baseReg), offset);
						return;
					}

					//Clock CPU and controllers - 1N
					//mem_check_32(op_addr, value, false);
					gba.Memory.WriteWord(opAddr, value);
					//clock(op_addr, true);

					break;

				// LDR
				case 0x1:
					//Clock CPU and controllers - 1N
					offset <<= 2;
					opAddr += offset;
					//clock(reg.r15, true);

					if (peek)
					{
						peekString = String.Format("LDR {0}, [{1} + ${2}]", GetRegisterName(srcDestReg), GetRegisterName(baseReg), offset);
						return;
					}

					//Clock CPU and controllers - 1I
					//mem_check_32(op_addr, value, true);
					value = gba.Memory.ReadWord(opAddr);
					//clock();

					//Clock CPU and controllers - 1S
					SetRegisterValue(srcDestReg, value);
					//clock((reg.r15 + 2), false);

					break;

				// STRB
				case 0x2:
					//Clock CPU and controllers - 1N
					value = GetRegisterValue(srcDestReg);
					opAddr += offset;
					//clock(reg.r15, true);

					if (peek)
					{
						peekString = String.Format("STR {0}, [{1} + ${2}]", GetRegisterName(srcDestReg), GetRegisterName(baseReg), offset);
						return;
					}

					//Clock CPU and controllers - 1N
					//mem_check_8(op_addr, value, false);
					gba.Memory.WriteByte(opAddr, (byte) value);
					//clock(op_addr, true);

					break;

				// LDRB
				case 0x3:
					//Clock CPU and controllers - 1N
					opAddr += offset;
					//clock(reg.r15, true);

					if (peek)
					{
						peekString = String.Format("LDR {0}, [{1} + ${2}]", GetRegisterName(srcDestReg), GetRegisterName(baseReg), offset);
						return;
					}

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

		// THUMB.16 
		// _16_|_1___1___0___1_|_____Cond______|_________Signed_Offset_________|B{cond}
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
						peekString = String.Format("BEQ {0}", jump_addr);
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
						peekString = String.Format("BNE {0}", jump_addr);
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
						peekString = String.Format("BCS {0}", jump_addr);
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
						peekString = String.Format("BCC {0}", jump_addr);
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
						peekString = String.Format("BMI {0}", jump_addr);
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
						peekString = String.Format("BPL {0}", jump_addr);
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
						peekString = String.Format("BVS {0}", jump_addr);
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
						peekString = String.Format("BVC {0}", jump_addr);
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
						peekString = String.Format("BHI {0}", jump_addr);
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
						peekString = String.Format("BLS {0}", jump_addr);
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
							peekString = String.Format("BGE {0}", jump_addr);
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
							peekString = String.Format("BLT {0}", jump_addr);
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
							peekString = String.Format("BGT {0}", jump_addr);
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


		// THUMB.19 Long Branch with Link 
		// _19_|_1___1___1___1_|_H_|______________Offset_Low/High______________|BL,BLX
		void LongBranchLink(ushort rawInstruction, bool peek)
		{
			// Determine if this is the first or second instruction executed
			bool firstOp = (((rawInstruction >> 11) & 0x1F) == 0x1F) ? false : true;

			UInt32 labelAddr = 0;

			// Perform 1st 16-bit operation
			if (firstOp)
			{
				byte pre_bit = (byte) ((PC & 0x800000)!=0 ? 1 : 0);

				// Upper 11-bits of destination address
				labelAddr = (UInt32) ((rawInstruction & 0x7FF) << 12);

				// Add as a 2's complement to PC
				if ((labelAddr & 0x400000) != 0) { labelAddr |= 0xFF800000; }
				labelAddr += PC;

				if (peek)
				{
					peekString = String.Format("BL0 ${0:X}", labelAddr);
					return;
				}

				// Save label to LR
				SetRegisterValue(14, labelAddr);

				//Clock CPU and controllers - 1S
				//clock(reg.r15, false);
			}

			// Perform 2nd 16-bit operation
			else
			{
				// Address of the "next" instruction to place in LR, set Bit 0 to 1
				UInt32 nextInstrAddr = (PC - 2);
				nextInstrAddr |= 1;

				// Lower 11-bits of destination address
				labelAddr = R14;
				labelAddr += (UInt32) ((rawInstruction & 0x7FF) << 1);

				if (peek)
				{
					peekString = String.Format("BL1 ${0:X}", labelAddr);
					return;
				}

				//Clock CPU and controllers - 1N
				//clock(reg.r15, true);

				PC = labelAddr;
				PC &= ~0x1U;

				requestFlushPipeline = true;
				R14 = nextInstrAddr;

				//Clock CPU and controllers - 2S
				//clock(reg.r15, false);
				//clock((reg.r15 + 2), false);
			}
		}

	}
}
