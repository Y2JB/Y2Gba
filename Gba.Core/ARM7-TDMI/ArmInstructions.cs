// Full disclosure - I got a huge amount of help and code from Gbe-Plus when writing the CPU emulation

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;


namespace Gba.Core
{

	public partial class Cpu
    {
		string peekString;

		public void DecodeAndExecuteArmInstruction(UInt32 rawInstruction)
        {
			DecodeArmInstruction(rawInstruction, false);
		}


		public string PeekArmInstruction(UInt32 rawInstruction)
        {
			peekString = "*UNKNOWN_INSTRUCTION*";
			DecodeArmInstruction(rawInstruction, true);
			return peekString;
        }


        void DecodeArmInstruction(UInt32 rawInstruction, bool peek)
        {
			// Extract conditional
			ConditionalExecution conditional = (ConditionalExecution)((rawInstruction & 0xF0000000) >> 28);

			// Conditional early out 
			if (peek == false && 
				conditional != ConditionalExecution.AL)
			{
				if (CondtionalHandlers[(int)conditional].Invoke() == false)
				{
					// Conditional return false, we do not execture the instruction
					return;
				}
			}

			if (((rawInstruction >> 8) & 0xFFFFF) == 0x12FFF)
			{
				//ARM_3
				DecodeBranchExchange(rawInstruction, peek);
			}

			else if (((rawInstruction >> 25) & 0x7) == 0x5)
			{
				//ARM_4
				DecodeBranchLink(rawInstruction, peek);
			}		

			else if ((rawInstruction & 0xD900000) == 0x1000000)
			{

				if (((rawInstruction & 0x80)!=0) && ((rawInstruction & 0x10) != 0) && ((rawInstruction & 0x2000000) == 0))
				{
					if (((rawInstruction >> 5) & 0x3) == 0)
					{
						//ARM_12;
						if (!peek) throw new NotImplementedException();
					}

					else
					{
						//ARM_10;
						HalfwordAndSignedDataTransfer(rawInstruction, peek);
					}
				}

				else
				{
					//ARM_6
					PsrTransfer(rawInstruction, peek);
				}
			}

			else if (((rawInstruction >> 26) & 0x3) == 0x0)
			{
				if (((rawInstruction & 0x80) != 0) && ((rawInstruction & 0x10) == 0))
				{
					//ARM.5
					if ((rawInstruction & 0x2000000) != 0)
					{
						//instruction_operation[pipeline_id] = ARM_5;
						DecodeDataProcessing(rawInstruction, peek);
					}

					//ARM.5
					else if (((rawInstruction & 0x100000) != 0) && (((rawInstruction >> 23) & 0x3) == 0x2))
					{
						//ARM_5
						DecodeDataProcessing(rawInstruction, peek);
					}

					//ARM.5
					else if (((rawInstruction >> 23) & 0x3) != 0x2)
					{
						// ARM_5
						DecodeDataProcessing(rawInstruction, peek);
					}

					//ARM.7
					else
					{
						if (!peek) throw new NotImplementedException();
					}
				}

				else if (((rawInstruction & 0x80) != 0) && ((rawInstruction & 0x10) != 0))
				{
					if (((rawInstruction >> 4) & 0xF) == 0x9)
					{
						//ARM.5
						if ((rawInstruction & 0x2000000) != 0)
						{
							//ARM_5
							DecodeDataProcessing(rawInstruction, peek);
						}

						//ARM.12
						else if (((rawInstruction >> 23) & 0x3) == 0x2)
						{
							//ARM_12;
							if (!peek) throw new NotImplementedException();
						}

						//ARM.7
						else
						{
							//ARM_7;
							if (!peek) throw new NotImplementedException();
						}
					}

					//ARM.5
					else if ((rawInstruction & 0x2000000) != 0)
					{
						//ARM_5
						DecodeDataProcessing(rawInstruction, peek);
					}

					//ARM.10
					else
					{
						//ARM_10;
						HalfwordAndSignedDataTransfer(rawInstruction, peek);
					}
				}

				else
				{
					//ARM_5
					DecodeDataProcessing(rawInstruction, peek);
				}
			}

			else if (((rawInstruction >> 26) & 0x3) == 0x1)
			{
				//ARM_9
				DecodeSingleDataTransfer(rawInstruction, peek);
			}

			else if (((rawInstruction >> 25) & 0x7) == 0x4)
			{
				//ARM_11
				BlockDataTransfer(rawInstruction, peek);
			}

			else if (((rawInstruction >> 24) & 0xF) == 0xF)
			{
				//ARM_13
				if (!peek) throw new NotImplementedException();
			}
		}


		void DecodeBranchLink(UInt32 current_arm_instruction, bool peek)
		{			
			//Grab offset
			UInt32 offset = (current_arm_instruction & 0xFFFFFF);
			offset <<= 2;

			//Grab opcode
			byte op = (byte) ((current_arm_instruction >> 24) & 0x1);

			UInt32 final_addr = PC;

			//Add offset as 2s complement if necessary
			if ((offset & 0x2000000) != 0) 
			{ 
				offset |= 0xFC000000; 
			}

			final_addr += offset;

			// JB: Cpu.PC = Cpu.PC + 8 + (offset * 4);
			//final_addr += 8;

			switch (op)
			{
				//Branch
				case 0x0:
					if (peek)
					{
						peekString = String.Format("B ${0:X}", final_addr);
						return;
					}
					else
					{
						//Clock CPU and controllers - 1N
						//clock(reg.r15, true);

						// JB: -4 so the pipeline will pull in this instruction next
						PC = (final_addr);

						requestFlushPipeline = true;
						//needs_flush = true;

						//Clock CPU and controllers - 2S
						//clock(reg.r15, false);
						//clock((reg.r15 + 4), false);
						return;
					}
					

				//Branch and Link
				case 0x1:
					if (peek)
					{
						peekString = String.Format("BL ${0:X}", final_addr);
						return;
					}
					else
					{
						//Clock CPU and controllers - 1N
						//clock(reg.r15, true);

						LR = PC - 4;
						PC = final_addr;

						requestFlushPipeline = true;
						//needs_flush = true;

						//Clock CPU and controllers - 2S
						//clock(reg.r15, false);
						//clock((reg.r15 + 4), false);
						return;
					};
			}

			throw new ArgumentException("Failed to decode instruction");
		}



		void DecodeBranchExchange(UInt32 rawinstruction, bool peek)
		{
			// |_Cond__|0_0_0_1_0_0_1_0_1_1_1_1_1_1_1_1_1_1_1_1|0_0|L|1|__Rn___| BX,BLX

			// Bits 0-2
			byte srcReg = (byte) (rawinstruction & 0xF);

			// R15 is not allowed
			if (srcReg <= 14)
			{
				if (peek)
				{
					peekString = String.Format("BX {0}", GetRegisterName(srcReg));
					return;
				}

				UInt32 result = GetRegisterValue(srcReg);
				byte op = (byte)((rawinstruction >> 4) & 0xF);

				// Arm instrctions are always aligned to a 2 or 4 byte boundary. Therefore the low bit of the
				// branch address is never used so it is used as the toggle bit for switching to Thumb mode. 
				if ((result & 0x1) != 0)
				{
					State = CpuState.Thumb;
					SetFlag(StatusFlag.ThumbExecution);
					result &= (UInt32)(~1U);
				}

				switch (op)
				{
					//Branch
					case 0x1:
						//Clock CPU and controllers - 1N
						//clock(reg.r15, true);

						PC = result;

						requestFlushPipeline = true;
						//needs_flush = true;

						//Clock CPU and controllers - 2S
						//clock(reg.r15, false);
						//clock((reg.r15 + 4), false);

						break;

					default:
						throw new ArgumentException("Invalid BX Instr");
				}
			}
			else
			{
				throw new ArgumentException("Invalid BX Instr, branch to R15 not allowed");
			}
		}


		// ARM 5
		void DecodeDataProcessing(UInt32 rawInstruction, bool peek)
		{
			//Determine if an immediate value or a register should be used as the operand
			bool use_immediate = ((rawInstruction & 0x2000000)!=0) ? true : false;

			//Determine if condition codes should be updated
			bool setCondition = ((rawInstruction & 0x100000) != 0) ? true : false;

			byte op = (byte)((rawInstruction >> 21) & 0xF);

			//Grab source register
			byte srcReg = (byte)((rawInstruction >> 16) & 0xF);

			//Grab destination register
			byte destReg = (byte)((rawInstruction >> 12) & 0xF);

			//When use_immediate is 0, determine whether the register should be shifted by another register or an immediate
			bool shiftImmediate = ((rawInstruction & 0x10) != 0) ? false : true;

			UInt32 result = 0;
			UInt32 input = GetRegisterValue(srcReg);
			UInt32 operand = 0;
			byte shiftOut = 2;

			//Use immediate as operand
			if (use_immediate)
			{
				operand = (rawInstruction & 0xFF);
				byte offset = (byte)((rawInstruction >> 8) & 0xF);

				//Shift immediate - ROR special case - Carry flag not affected
				RotateRightSpecial(ref operand, offset);
			}

			//Use register as operand
			else
			{
				operand = GetRegisterValue(rawInstruction & 0xF);
				byte shiftType = (byte)((rawInstruction >> 5) & 0x3);
				byte offset = 0;

				//Shift the register-operand by an immediate
				if (shiftImmediate)
				{
					offset = (byte)((rawInstruction >> 7) & 0x1F);
				}

				//Shift the register-operand by another register
				else
				{
					offset = (byte) (GetRegisterValue((UInt32)((rawInstruction >> 8) & 0xF)));

					if (srcReg == 15) { input += 4; }
					if ((rawInstruction & 0xF) == 15) { operand += 4; }

					//Valid registers to shift by are R0-R14
					if (((rawInstruction >> 8) & 0xF) == 0xF) 
					{
						throw new ArgumentException("Data Processing: Bad Shift");
					}
				}

				//Shift the register
				switch (shiftType)
				{
					//LSL
					case 0x0:
						if ((!shiftImmediate) && (offset == 0)) { break; }
						else { shiftOut = LogicalShiftLeft(ref operand, offset); }
						break;

					//LSR
					case 0x1:
						if ((!shiftImmediate) && (offset == 0)) { break; }
						else { shiftOut = LogicalShiftRight(ref operand, offset); }
						break;

					//ASR
					case 0x2:
						if ((!shiftImmediate) && (offset == 0)) { break; }
						else { shiftOut = ArithmeticShiftRight(ref operand, offset); }
						break;

					//ROR
					case 0x3:
						if ((!shiftImmediate) && (offset == 0)) { break; }
						else { shiftOut = RotateRight(ref operand, offset); }
						break;
				}

				//Clock CPU and controllers - 1I
				//clock();
			}

			//TODO - When op is 0x8 through 0xB, make sure Bit 20 is 1 (rather force it? Unsure)
			//TODO - 2nd Operand for TST/TEQ/CMP/CMN must be R0 (rather force it to be R0)
			//TODO - See GBATEK - S=1, with unused Rd bits=1111b

			//Clock CPU and controllers - 1N
			if (destReg == 15)
			{
				if (peek == false)
				{
					//clock(reg.r15, true);

					//When the set condition parameter is 1 and destination register is R15, change CPSR to SPSR
					if (setCondition)
					{
						CPSR = SPSR;
						setCondition = false;

						// Set the CPU mode
						switch ((CPSR & 0x1F))
						{
							case 0x10: Mode = CpuMode.User; break;
							case 0x11: Mode = CpuMode.FIQ; break;
							case 0x12: Mode = CpuMode.IRQ; break;
							case 0x13: Mode = CpuMode.Supervisor; break;
							case 0x17: Mode = CpuMode.Abort; break;
							case 0x1B: Mode = CpuMode.Undefined; break;
							case 0x1F: Mode = CpuMode.System; break;
								//default: std::cout << "CPU::ARM9::Warning - ARM.6 CPSR setting unknown CPU mode -> 0x" << std::hex << (reg.cpsr & 0x1F) << "\n";
						}

						// Switch to ARM or THUMB mode if necessary
						State = ((CPSR & 0x20) != 0) ? CpuState.Thumb : CpuState.Arm;
					}
				}
			}

			switch (op)
			{
				//AND
				case 0x0:
					if (peek)
					{
						peekString = String.Format("AND {0},${1:X}", GetRegisterName(destReg), operand);
						return;
					}

					result = (input & operand);
					SetRegisterValue(destReg, result);

					//Update condition codes
					if (setCondition) { UpdateFlagsForLogicOps(result, shiftOut); }
					break;

				//XOR
				case 0x1:
					if (peek)
					{
						peekString = String.Format("EOR {0},${1:X}", GetRegisterName(destReg), operand);
						return;
					}

					result = (input ^ operand);
					SetRegisterValue(destReg, result);

					//Update condition codes
					if (setCondition) { UpdateFlagsForLogicOps(result, shiftOut); }
					break;

				//SUB
				case 0x2:
					if (peek)
					{
						peekString = String.Format("SUB {0},${1:X}", GetRegisterName(destReg), operand);
						return;
					}

					result = (input - operand);
					SetRegisterValue(destReg, result);

					//Update condtion codes
					if (setCondition) { UpdateFlagsForArithmeticOps(input, operand, result, false); }
					break;

				//RSB
				case 0x3:
					if (peek)
					{
						peekString = String.Format("RSB {0},${1:X}", GetRegisterName(destReg), operand);
						return;
					}

					result = (operand - input);
					SetRegisterValue(destReg, result);

					//Update condtion codes
					if (setCondition) { UpdateFlagsForArithmeticOps(operand, input, result, false); }
					break;

				//ADD
				case 0x4:
					if (peek)
					{
						peekString = String.Format("ADD {0},[${1:X},${2:X}]", GetRegisterName(destReg), input, operand);
						return;
					}

					result = (input + operand);		
					SetRegisterValue(destReg, result);

					//Update condtion codes
					if (setCondition) { UpdateFlagsForArithmeticOps(input, operand, result, true); }
					break;

				//ADC
				case 0x5:
					if (peek)
					{
						peekString = String.Format("ADC {0},${1:X}", GetRegisterName(destReg), operand);
						return;
					}

					//If no shift was performed, use the current Carry Flag for this math op
					if (shiftOut == 2) { shiftOut = (byte)(CarryFlag ? 1 : 0); }
					result = (input + operand + shiftOut);
					SetRegisterValue(destReg, result);

					//Update condtion codes
					if (setCondition) { UpdateFlagsForArithmeticOps(input, (operand + shiftOut), result, true); }
					break;

				//SBC
				case 0x6:
					if (peek)
					{
						peekString = String.Format("SBC {0},${1:X}", GetRegisterName(destReg), operand);
						return;
					}

					//If no shift was performed, use the current Carry Flag for this math op
					if (shiftOut == 2) { shiftOut = (byte)(CarryFlag ? 1 : 0); }

					result = (input - operand + shiftOut - 1);

					if (peek)
					{
						peekString = String.Format("SBC {0},${1:X}", GetRegisterName(destReg), result);
						return;
					}


					SetRegisterValue(destReg, result);

					//Update condtion codes
					if (setCondition) { UpdateFlagsForArithmeticOps(input, (operand + shiftOut - 1), result, false); }
					break;

				//RSC
				case 0x7:
					if (peek)
					{
						peekString = String.Format("RSC {0},${1:X}", GetRegisterName(destReg), operand);
						return;
					}

					//If no shift was performed, use the current Carry Flag for this math op
					if (shiftOut == 2) { shiftOut = (byte)(CarryFlag ? 1 : 0); }

					result = (operand - input + shiftOut - 1);
					SetRegisterValue(destReg, result);

					//Update condtion codes
					if (setCondition) { UpdateFlagsForArithmeticOps((operand + shiftOut - 1), input, result, false); }
					break;

				//TST
				case 0x8:
					if (peek)
					{
						peekString = String.Format("TST {0},${1:X}", GetRegisterName(srcReg), operand);
						return;
					}

					result = (input & operand);

					//Update condtion codes
					if (setCondition) { UpdateFlagsForLogicOps(result, shiftOut); }
					break;

				//TEQ
				case 0x9:
					if (peek)
					{
						peekString = String.Format("TEQ {0},${1:X}", GetRegisterName(srcReg), operand);
						return;
					}
					result = (input ^ operand);

					//Update condtion codes
					if (setCondition) { UpdateFlagsForLogicOps(result, shiftOut); }
					break;

				//CMP
				case 0xA:
					if (peek)
					{
						peekString = String.Format("CMP {0},${1:X}", GetRegisterName(srcReg), operand);
						return;
					}
					result = (input - operand);

					//Update condtion codes
					if (setCondition) { UpdateFlagsForArithmeticOps(input, operand, result, false); }
					break;

				//CMN
				case 0xB:
					if (peek)
					{
						peekString = String.Format("CMN {0},${1:X}", GetRegisterName(srcReg), operand);
						return;
					}

					result = (input + operand);

					//Update condtion codes
					if (setCondition) { UpdateFlagsForArithmeticOps(input, operand, result, true); }
					break;

				//ORR
				case 0xC:
					if (peek)
					{
						peekString = String.Format("ORR {0},${1:X}", GetRegisterName(srcReg), operand);
						return;
					}

					result = (input | operand);
					SetRegisterValue(destReg, result);

					//Update condtion codes
					if (setCondition) { UpdateFlagsForLogicOps(result, shiftOut); }
					break;

				//MOV
				case 0xD:
					if (peek)
					{
						peekString = String.Format("MOV {0},${1:X}", GetRegisterName(destReg), operand);
						return;
					}

					result = operand;
					SetRegisterValue(destReg, result);

					//Update condtion codes
					if (setCondition) { UpdateFlagsForLogicOps(result, shiftOut); }
					break;

				//BIC
				case 0xE:
					if (peek)
					{
						peekString = String.Format("BIC {0},${1:X}", GetRegisterName(destReg), operand);
						return;
					}

					result = (input & (~operand));
					SetRegisterValue(destReg, result);

					//Update condtion codes
					if (setCondition) { UpdateFlagsForLogicOps(result, shiftOut); }
					break;

				//MVN
				case 0xF:
					if (peek)
					{
						peekString = String.Format("MVN {0},${1:X}", GetRegisterName(destReg), operand);
						return;
					}

					result = ~operand;
					SetRegisterValue(destReg, result);

					//Update condtion codes
					if (setCondition) { UpdateFlagsForLogicOps(result, shiftOut); }
					break;
			}

			//Timings for PC as destination register
			if (destReg == 15)
			{
				//Clock CPU and controllers - 2S
				//needs_flush = true;
				//clock(reg.r15, false);
				//clock((reg.r15 + 4), false);

				//Switch to THUMB mode if necessary
				if (((R15 & 0x1)!= 0) || (State == CpuState.Thumb))
				{
					State = CpuState.Thumb;
					CPSR |= 0x20;
					R15 &= (UInt32)(~1U);
				}

				else { R15 &= (UInt32)(~3U); }
			}

			//Timings for regular registers
			else
			{
				//Clock CPU and controllers - 1S
				//clock((reg.r15 + 4), false);
			}
		}

		/****** ARM.6 PSR Transfer ******/
		void PsrTransfer(UInt32 rawInstruction, bool peek)
		{
			//Determine if an immediate or a register will be used as input (MSR only) - Bit 25
			bool useImmediate = ((rawInstruction & 0x2000000)!=0) ? true : false;

			//Determine access is for CPSR or SPSR - Bit 22
			byte psr = (byte) (((rawInstruction & 0x400000) != 0) ? 1 : 0);

			//Grab opcode
			byte op = (byte) (((rawInstruction & 0x200000) !=0) ? 1 : 0);

			switch (op)
			{
				//MRS
				case 0x0:
					{
						//Grab destination register - Bits 12-15
						byte destReg = (byte) ((rawInstruction >> 12) & 0xF);

						if (destReg == 15) 
						{ 
							throw new ArgumentException("Invalid Register"); 
						}

						// Store CPSR into destination register
						if (psr == 0) 
						{
							if (peek)
							{
								peekString = String.Format("MRS {0},CPSR", GetRegisterName(destReg));
								return;
							}
							SetRegisterValue(destReg, CPSR); 
						}
						// Store SPSR into destination register
						else 
						{
							if (peek)
							{
								peekString = String.Format("MRS {0},SPSR", GetRegisterName(destReg));
								return;
							}
							SetRegisterValue(destReg, SPSR); 
						}
					}
					break;

				//MSR
				case 0x1:
					{
						UInt32 input = 0;

						//Create Op Field mask
						UInt32 opFieldMask = 0;

						//Flag field - Bit 19
						if (((rawInstruction & 0x80000) != 0)) 
						{ 
							opFieldMask |= 0xFF000000; 
						}

						//Status field - Bit 18
						if (((rawInstruction & 0x40000) != 0))
						{
							opFieldMask |= 0x00FF0000;
							//std::cout << "CPU::Warning - ARM.6 MSR enabled access to Status Field \n";
						}

						//Extension field - Bit 17
						if (((rawInstruction & 0x20000) != 0))
						{
							opFieldMask |= 0x0000FF00;
							//std::cout << "CPU::Warning - ARM.6 MSR enabled access to Extension Field \n";
						}

						//Control field - Bit 15
						if (((rawInstruction & 0x10000) != 0)) 
						{ 
							opFieldMask |= 0x000000FF; 
						}

						//Use shifted 8-bit immediate as input
						if (useImmediate)
						{
							//Grab shift offset - Bits 8-11
							byte offset = (byte) ((rawInstruction >> 8) & 0xF);

							//Grab 8-bit immediate - Bits 0-7
							input = (rawInstruction) & 0xFF;

							RotateRightSpecial(ref input, offset);
						}

						//Use register as input
						else
						{
							//Grab source register - Bits 0-3
							byte srcReg = (byte) (rawInstruction & 0xF);

							if (srcReg == 15) 
							{ 
								//std::cout << "CPU::Warning - ARM.6 R15 used as Source Register \n"; 
							}

							input = GetRegisterValue(srcReg);
							input &= opFieldMask;
						}

						//Write into CPSR
						if (psr == 0)
						{
							if (peek)
							{
								peekString = String.Format("MSR CPSR");
								return;
							}

							CPSR &= ~opFieldMask;
							CPSR |= input;
						
							//Set the CPU mode accordingly
							switch ((CPSR & 0x1F))
							{
								case 0x10: Mode = CpuMode.User; break;
								case 0x11: Mode = CpuMode.FIQ; break;
								case 0x12: Mode = CpuMode.IRQ; break;
								case 0x13: Mode = CpuMode.Supervisor; break;
								case 0x17: Mode = CpuMode.Abort; break;
								case 0x1B: Mode = CpuMode.Undefined; break;
								case 0x1F: Mode = CpuMode.System; break;
							}

							if (ThumbFlag)
							{
								//std::cout << "CPU::Warning - ARM.6 Setting THUMB mode\n";
								State = CpuState.Thumb;
								PC &= ~0x1U;
								//needs_flush = true;
							}
						}
						//Write into SPSR
						else
						{
							if (peek)
							{
								peekString = String.Format("MSR SPSR");
								return;
							}

							UInt32 temp_spsr = SPSR;
							temp_spsr &= ~opFieldMask;
							temp_spsr |= input;
							SPSR = temp_spsr;
						}
					}
					break;
			}

			//Clock CPU and controllers - 1S
			//clock((reg.r15 + 4), false);
		}


		// ARM_9
		void DecodeSingleDataTransfer(UInt32 current_arm_instruction, bool peek)
        {
			// Bit 25
			byte offsetIsRegister = (byte) (((current_arm_instruction & 0x2000000) !=0) ? 1 : 0);

			// Bit 24
			byte prePost = (byte)(((current_arm_instruction & 0x1000000) != 0) ? 1 : 0);

			// Bit 23
			byte upDown = (byte)(((current_arm_instruction & 0x800000) != 0) ? 1 : 0);

			// Bit 22
			byte byteWord = (byte)(((current_arm_instruction & 0x400000) != 0) ? 1 : 0);

			// Bit 21
			byte writeBack = (byte)(((current_arm_instruction & 0x200000) != 0) ? 1 : 0);

			// Bit 20
			byte loadStore = (byte)(((current_arm_instruction & 0x100000) != 0) ? 1 : 0);

			// Bits 16-19
			byte baseReg = (byte) ((current_arm_instruction >> 16) & 0xF);

			// Bits 12-15
			byte destReg = (byte) ((current_arm_instruction >> 12) & 0xF);

			UInt32 baseOffset = 0;
			UInt32 baseAddr = GetRegisterValue(baseReg);
			UInt32 value = 0;

			// Offset is a 12-bit immediate value
			if (offsetIsRegister == 0) { baseOffset = (current_arm_instruction & 0xFFF); }

			// Offset is shifted register
			else
			{
				// Bits 0-3
				byte offsetRegister = (byte) (current_arm_instruction & 0xF);
				baseOffset = GetRegisterValue(offsetRegister);

				// Bits 5-6
				byte shiftType = (byte) ((current_arm_instruction >> 5) & 0x3);

				// Bits 7-11
				byte shiftOffset = (byte) ((current_arm_instruction >> 7) & 0x1F);

				//Shift the register
				switch (shiftType)
				{
					//LSL
					case 0x0:
						LogicalShiftLeft(ref baseOffset, shiftOffset);
						break;

					//LSR
					case 0x1:
						LogicalShiftRight(ref baseOffset, shiftOffset);
						break;

					//ASR
					case 0x2:
						ArithmeticShiftRight(ref baseOffset, shiftOffset);
						break;

					//ROR
					case 0x3:
						RotateRight(ref baseOffset, shiftOffset);
						break;
				}
			}

			//Increment or decrement before transfer if pre-indexing
			if (prePost == 1)
			{
				if (upDown == 1) { baseAddr += baseOffset; }
				else { baseAddr -= baseOffset; }
			}

			//Clock CPU and controllers - 1N
			//clock(reg.r15, true);

			//Store Byte or Word
			if (loadStore == 0)
			{
				if (peek)
				{
					peekString = String.Format("STR {0} , [{1},${2:X}]", GetRegisterName(destReg), GetRegisterName(baseReg), baseOffset);
					return;
				}

				if (byteWord == 1)
				{
					value = GetRegisterValue(destReg);
					if (destReg == 15) { value += 4; }
					value &= 0xFF;

					//mem_check_8(base_addr, value, false);
					gba.Memory.WriteByte(baseAddr, (byte)value);
				}

				else
				{
					value = GetRegisterValue(destReg);
					if (destReg == 15) { value += 4; }

					//mem->write_u32(base_addr, value);
					gba.Memory.WriteWord(baseAddr, value);
				}

				//Clock CPU and controllers - 1N
				//clock(base_addr, true);
			}

			//Load Byte or Word
			else
			{
				if (peek)
				{
					peekString = String.Format("LDR {0} , [{1},${2:X}]", GetRegisterName(destReg), GetRegisterName(baseReg), baseOffset);
					return;
				}

				if (byteWord == 1)
				{
					//Clock CPU and controllers - 1I
					//mem_check_8(base_addr, value, true);
					value = gba.Memory.ReadByte(baseAddr);

					//clock();

					//Clock CPU and controllers - 1N
					//if (destReg == 15) { clock((reg.r15 + 4), true); }

					SetRegisterValue(destReg, value);
				}

				else
				{
					//Clock CPU and controllers - 1I
					//mem_check_32(base_addr, value, true);
					//base_addr += 8;
					gba.Memory.ReadWriteWord_Checked(baseAddr, ref value, true);
					//value = gba.Memory.ReadWord(base_addr);

					//clock();

					//Clock CPU and controllers - 1N
					//if (destReg == 15) { clock((reg.r15 + 4), true); }

					SetRegisterValue(destReg, value);
				}
			}

			//Increment or decrement after transfer if post-indexing
			if (prePost == 0)
			{
				if (upDown == 1) { baseAddr += baseOffset; }
				else { baseAddr -= baseOffset; }
			}

			//Write back into base register
			//Post-indexing ALWAYS does this. Pre-Indexing does this optionally
			if ((prePost == 0) && (baseReg != destReg)) { SetRegisterValue(baseReg, baseAddr); }
			else if ((prePost == 1) && (writeBack == 1) && (baseReg != destReg)) { SetRegisterValue(baseReg, baseAddr); }

			//Timings for LDR - PC
			if ((destReg == 15) && (loadStore == 1))
			{
				//Clock CPU and controllser - 2S
				//clock(reg.r15, false);
				//clock((reg.r15 + 4), false);
				//needs_flush = true;
			}

			//Timings for LDR - No PC
			else if ((destReg != 15) && (loadStore == 1))
			{
				//Clock CPU and controllers - 1S
				//clock(reg.r15, false);
			}
		}



		// ARM.10 
		void HalfwordAndSignedDataTransfer(UInt32 rawInstruction, bool peek)
		{
			//TODO - Timings

			// Bit 24
			Byte prePost = (byte) (((rawInstruction & 0x1000000)!=0) ? 1 : 0);

			// Bit 23
			Byte upDown = (byte)(((rawInstruction & 0x800000) != 0) ? 1 : 0);

			// Bit 22
			Byte offsetIsRegister = (byte)(((rawInstruction & 0x400000) != 0) ? 1 : 0);

			// Bit 21
			Byte writeBack = (byte)(((rawInstruction & 0x200000) != 0) ? 1 : 0);

			// Bit 20
			Byte loadStore = (byte)(((rawInstruction & 0x100000) != 0) ? 1 : 0);

			// Bits 16-19
			Byte baseReg = (byte) ((rawInstruction >> 16) & 0xF);

			// Bits 12-15
			Byte destReg = (byte) ((rawInstruction >> 12) & 0xF);

			// Bits 5-6
			Byte op = (byte) ((rawInstruction >> 5) & 0x3);

			// Write-Back is always enabled for Post-Indexing
			if (prePost == 0) { writeBack = 1; }

			UInt32 baseOffset = 0;
			UInt32 baseAddr = GetRegisterValue(baseReg);
			UInt32 value = 0;

			//Determine offset if offset is a register
			if (offsetIsRegister == 0)
			{
				//Register is Bits 0-3
				baseOffset = GetRegisterValue((rawInstruction & 0xF));

				if ((rawInstruction & 0xF) == 15) 
				{ 
					throw new ArgumentException("ARM.10 Offset Register is PC"); 
				}
			}

			//Determine offset if offset is immediate
			else
			{
				//Upper 4 bits are Bits 8-11
				baseOffset = (rawInstruction >> 8) & 0xF;
				baseOffset <<= 4;

				//Lower 4 bits are Bits 0-3
				baseOffset |= (rawInstruction & 0xF);
			}

			//Increment or decrement before transfer if pre-indexing
			if (prePost == 1)
			{
				if (upDown == 1) { baseAddr += baseOffset; }
				else { baseAddr -= baseOffset; }
			}

			//Perform Load or Store ops
			switch (op)
			{
				//Load-Store unsigned halfword
				case 0x1:					
					//Store halfword
					if (loadStore == 0)
					{
						if (peek)
						{
							peekString = String.Format("STR-(h) {0} , [{1},${2:X}]", GetRegisterName(destReg), GetRegisterName(baseReg), baseOffset);
							return;
						}

						value = GetRegisterValue(destReg);

						//If PC is the Destination Register, add 4
						if (destReg == 15) 
						{ 
							value += 4; 
						}

						value &= 0xFFFF;
						//mem->write_u16(base_addr, value);
						gba.Memory.WriteHalfWord(baseAddr, (ushort) value);
					}

					//Load halfword
					else
					{
						if (peek)
						{
							peekString = String.Format("LDR-(h) {0} , [{1},${2:X}]", GetRegisterName(destReg), GetRegisterName(baseReg), baseOffset);
							return;
						}

						//value = mem->read_u16(base_addr);
						value = gba.Memory.ReadHalfWord(baseAddr);
						SetRegisterValue(destReg, value);
					}
					break;

				//Load signed byte (sign extended)
				case 0x2:
					if (peek)
					{
						peekString = String.Format("LDR-(sb) {0} , [{1},${2:X}]", GetRegisterName(destReg), GetRegisterName(baseReg), baseOffset);
						return;
					}

					value = gba.Memory.ReadByte(baseAddr);

					if ((value & 0x80) != 0) 
					{ 
						value |= 0xFFFFFF00; 
					}
					SetRegisterValue(destReg, value);
					break;

				//Load signed halfword (sign extended)
				case 0x3:
					if (peek)
					{
						peekString = String.Format("LDR-(sh) {0} , [{1},${2:X}]", GetRegisterName(destReg), GetRegisterName(baseReg), baseOffset);
						return;
					}

					//value = mem->read_u16(base_addr);
					value = gba.Memory.ReadHalfWord(baseAddr);

					if ((value & 0x8000) != 0) 
					{ 
						value |= 0xFFFF0000; 
					}
					SetRegisterValue(destReg, value);
					break;

				//SWP
				default:
					//std::cout << "This is actually ARM.12 - Single Data Swap\n";
					return;
			}

			//Increment or decrement after transfer if post-indexing
			if (prePost == 0)
			{
				if (upDown == 1) { baseAddr += baseOffset; }
				else { baseAddr -= baseOffset; }
			}

			//Write-back into base register
			if ((writeBack == 1) && (baseReg != destReg)) 
			{
				SetRegisterValue(baseReg, baseAddr);
			}
		}


		// ARM.11 
		void BlockDataTransfer(UInt32 rawInstruction, bool peek)
		{
			//TODO - Clock cycles

			// Bit 24
			byte prePost = (byte) (((rawInstruction & 0x1000000)!=0) ? 1 : 0);

			// Bit 23
			byte upDown = (byte) (((rawInstruction & 0x800000) != 0) ? 1 : 0);

			// Bit 22
			byte psr = (byte) (((rawInstruction & 0x400000) != 0) ? 1 : 0);

			// Bit 21
			byte writeBack = (byte) (((rawInstruction & 0x200000) != 0) ? 1 : 0);

			// Bit 20
			byte loadStore = (byte) (((rawInstruction & 0x100000) != 0) ? 1 : 0);

			// Bits 16-19
			byte baseReg = (byte) ((rawInstruction >> 16) & 0xF);

			// Bits 0-15
			byte registerList = (byte) (rawInstruction & 0xFFFF);

			//Warnings
			//if (base_reg == 15) { std::cout << "CPU::Warning - ARM.11 R15 used as Base Register \n"; }

			// Force USR mode if PSR bit is set
			CpuMode tempMode = Mode;
			if (psr != 0) 
			{ 
				Mode = CpuMode.User; 
			}

			UInt32 baseAddr = GetRegisterValue(baseReg);
			UInt32 oldBase = baseAddr;
			byte transferReg = 0xFF;



			if (peek)
			{
				if (loadStore == 0)
				{

					peekString = String.Format("PUSH {0}", RegisterListToString(registerList, 16));
					return;

				}
				else
				{

					peekString = String.Format("POP {0}", RegisterListToString(registerList, 16));
					return;
				}
			}
			

			// Find out the first register in the Register List			
			for (int x = 0; x < 16; x++)
			{
				if ((registerList & (1 << x)) != 0)
				{
					transferReg = (byte) x;
					x = 0xFF;
					break;
				}
			}

			// Load-Store with an ascending stack order, Up-Down = 1
			if ((upDown == 1) && (registerList != 0))
			{
				for (int x = 0; x < 16; x++)
				{
					if ((registerList & (1 << x)) != 0)
					{
						// Increment before transfer if pre-indexing
						if (prePost == 1) 
						{ 
							baseAddr += 4; 
						}

						//Store registers
						if (loadStore == 0)
						{
							if ((x == transferReg) && (baseReg == transferReg)) 
							{ 
								//mem->write_u32(base_addr, old_base); 
								gba.Memory.WriteWord(baseAddr, oldBase);
							}
							else 
							{ 
								//mem->write_u32(base_addr, get_reg(x));
								gba.Memory.WriteWord(baseAddr, GetRegisterValue((UInt32) x));
							}
						}

						//Load registers
						else
						{
							if ((x == transferReg) && (baseReg == transferReg)) 
							{ 
								writeBack = 0; 
							}

							SetRegisterValue((UInt32) x, gba.Memory.ReadWord(baseAddr));
							if (x == 15) 
							{ 
								requestFlushPipeline = true; 
							}
						}

						// Increment after transfer if post-indexing
						if (prePost == 0) 
						{ 
							baseAddr += 4; 
						}
					}

					// Write back the into base register
					if (writeBack == 1) 
					{ 
						SetRegisterValue(baseReg, baseAddr); 
					}
				}
			}

			// Load-Store with a descending stack order, Up-Down = 0
			else if ((upDown == 0) && (registerList != 0))
			{
				for (int x = 15; x >= 0; x--)
				{
					if ((registerList & (1 << x)) != 0)
					{
						// Decrement before transfer if pre-indexing
						if (prePost == 1) { baseAddr -= 4; }

						// Store registers
						if (loadStore == 0)
						{
							if ((x == transferReg) && (baseReg == transferReg)) 
							{
								//mem->write_u32(base_addr, old_base); 
								gba.Memory.WriteWord(baseAddr, oldBase);
							}
							else 
							{ 
								//mem->write_u32(base_addr, get_reg(x));
								gba.Memory.WriteWord(baseAddr, GetRegisterValue((UInt32) x));
							}
						}

						// Load registers
						else
						{
							if ((x == transferReg) && (baseReg == transferReg)) { writeBack = 0; }
							SetRegisterValue((UInt32) x, gba.Memory.ReadWord(baseAddr));
							if (x == 15) 
							{ 
								requestFlushPipeline = true; 
							}
						}

						// Decrement after transfer if post-indexing
						if (prePost == 0)
						{ 
							baseAddr -= 4; 
						}
					}

					//Write back the into base register
					if (writeBack == 1)
					{ 
						SetRegisterValue(baseReg, baseAddr);
					}
				}
			}

			//Special case, empty RList
			else
			{
				//Load R15
				if (loadStore == 0) 
				{ 
					//mem->write_u32(base_addr, reg.r15);
					gba.Memory.WriteWord(baseAddr, PC);
				}

				//Store R15
				else
				{
					//reg.r15 = mem->read_u32(base_addr);
					PC = gba.Memory.ReadWord(baseAddr);
					requestFlushPipeline = true;
				}

				//Add 0x40 to base address if ascending stack, writeback into base register
				if (upDown == 1) 
				{ 
					SetRegisterValue(baseReg, (baseAddr + 0x40)); 
				}

				//Subtract 0x40 from base address if descending stack, writeback into base register
				else 
				{ 
					SetRegisterValue(baseReg, (baseAddr - 0x40)); 
				}

				//std::cout << "CPU::Warning - ARM.11 Instruction uses empty register list \n";
			}


			// Restore CPU mode if PSR bit is set
			if (psr != 0) 
			{ 
				Mode = tempMode; 
			}
		}



	}
}
