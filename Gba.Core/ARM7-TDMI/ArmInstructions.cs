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
					}

					else
					{
						//ARM_10;
					}
				}

				else
				{
					//ARM_6
					psr_transfer(rawInstruction, peek);
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
						//ARM_7;
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
						}

						//ARM.7
						else
						{
							//ARM_7;
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
					}
				}

				//ARM.5
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
			
			}

			else if (((rawInstruction >> 24) & 0xF) == 0xF)
			{
				//ARM_13
				
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
						peekString = String.Format("B {0:X}", final_addr);
						return;
					}
					else
					{
						//Clock CPU and controllers - 1N
						//clock(reg.r15, true);

						// JB: -4 so the pipeline will pull in this instruction next
						PC = (final_addr);

						FlushPipeline();
						RefillPipeline();
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
						peekString = String.Format("BL {0:X}", final_addr);
						return;
					}
					else
					{
						//Clock CPU and controllers - 1N
						//clock(reg.r15, true);

						LR = PC - 4;
						PC = final_addr;
						//needs_flush = true;

						//Clock CPU and controllers - 2S
						//clock(reg.r15, false);
						//clock((reg.r15 + 4), false);
						return;
					};
			}

			throw new ArgumentException("Failed to decode instruction");
		}



		void DecodeBranchExchange(UInt32 current_arm_instruction, bool peek)
		{
			// |_Cond__|0_0_0_1_0_0_1_0_1_1_1_1_1_1_1_1_1_1_1_1|0_0|L|1|__Rn___| BX,BLX

			//Grab source register - Bits 0-2
			byte src_reg = (byte) (current_arm_instruction & 0xF);

			//Valid registers : 0-14
			if (src_reg <= 14)
			{
				if (peek)
				{
					peekString = String.Format("BX {0}", GetRegisterName(src_reg));
					return;
				}

				UInt32 result = GetRegisterValue(src_reg);
				byte op = (byte)((current_arm_instruction >> 4) & 0xF);

				//Switch to THUMB mode if necessary
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
				throw new ArgumentException("Invalid BX Instr");
			}
		}



		// ARM 5
		void DecodeDataProcessing(UInt32 current_arm_instruction, bool peek)
		{
			//Determine if an immediate value or a register should be used as the operand
			bool use_immediate = ((current_arm_instruction & 0x2000000)!=0) ? true : false;

			//Determine if condition codes should be updated
			bool set_condition = ((current_arm_instruction & 0x100000) != 0) ? true : false;

			byte op = (byte)((current_arm_instruction >> 21) & 0xF);

			//Grab source register
			byte src_reg = (byte)((current_arm_instruction >> 16) & 0xF);

			//Grab destination register
			byte dest_reg = (byte)((current_arm_instruction >> 12) & 0xF);

			//When use_immediate is 0, determine whether the register should be shifted by another register or an immediate
			bool shift_immediate = ((current_arm_instruction & 0x10) != 0) ? false : true;

			UInt32 result = 0;
			UInt32 input = GetRegisterValue(src_reg);
			UInt32 operand = 0;
			byte shift_out = 2;

			//Use immediate as operand
			if (use_immediate)
			{
				operand = (current_arm_instruction & 0xFF);
				byte offset = (byte)((current_arm_instruction >> 8) & 0xF);

				//Shift immediate - ROR special case - Carry flag not affected
				rotate_right_special(ref operand, offset);
			}

			//Use register as operand
			else
			{
				operand = GetRegisterValue(current_arm_instruction & 0xF);
				byte shift_type = (byte)((current_arm_instruction >> 5) & 0x3);
				byte offset = 0;

				//Shift the register-operand by an immediate
				if (shift_immediate)
				{
					offset = (byte)((current_arm_instruction >> 7) & 0x1F);
				}

				//Shift the register-operand by another register
				else
				{
					offset = (byte) (GetRegisterValue((UInt32)((current_arm_instruction >> 8) & 0xF)));

					if (src_reg == 15) { input += 4; }
					if ((current_arm_instruction & 0xF) == 15) { operand += 4; }

					//Valid registers to shift by are R0-R14
					if (((current_arm_instruction >> 8) & 0xF) == 0xF) 
					{
						throw new ArgumentException("Data Processing: Bad Shift");
					}
				}

				//Shift the register
				switch (shift_type)
				{
					//LSL
					case 0x0:
						if ((!shift_immediate) && (offset == 0)) { break; }
						else { shift_out = logical_shift_left(ref operand, offset); }
						break;

					//LSR
					case 0x1:
						if ((!shift_immediate) && (offset == 0)) { break; }
						else { shift_out = logical_shift_right(ref operand, offset); }
						break;

					//ASR
					case 0x2:
						if ((!shift_immediate) && (offset == 0)) { break; }
						else { shift_out = arithmetic_shift_right(ref operand, offset); }
						break;

					//ROR
					case 0x3:
						if ((!shift_immediate) && (offset == 0)) { break; }
						else { shift_out = rotate_right(ref operand, offset); }
						break;
				}

				//Clock CPU and controllers - 1I
				//clock();
			}

			//TODO - When op is 0x8 through 0xB, make sure Bit 20 is 1 (rather force it? Unsure)
			//TODO - 2nd Operand for TST/TEQ/CMP/CMN must be R0 (rather force it to be R0)
			//TODO - See GBATEK - S=1, with unused Rd bits=1111b

			//Clock CPU and controllers - 1N
			if (dest_reg == 15)
			{
				if (peek == false)
				{
					//clock(reg.r15, true);

					//When the set condition parameter is 1 and destination register is R15, change CPSR to SPSR
					if (set_condition)
					{
						CPSR = SPSR;
						set_condition = false;

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
						peekString = String.Format("AND {0},{1:X}", GetRegisterName(dest_reg), operand);
						return;
					}

					result = (input & operand);
					SetRegisterValue(dest_reg, result);

					//Update condition codes
					if (set_condition) { update_condition_logical(result, shift_out); }
					break;

				//XOR
				case 0x1:
					if (peek)
					{
						peekString = String.Format("EOR {0},{1:X}", GetRegisterName(dest_reg), operand);
						return;
					}

					result = (input ^ operand);
					SetRegisterValue(dest_reg, result);

					//Update condition codes
					if (set_condition) { update_condition_logical(result, shift_out); }
					break;

				//SUB
				case 0x2:
					if (peek)
					{
						peekString = String.Format("SUB {0},{1:X}", GetRegisterName(dest_reg), operand);
						return;
					}

					result = (input - operand);
					SetRegisterValue(dest_reg, result);

					//Update condtion codes
					if (set_condition) { update_condition_arithmetic(input, operand, result, false); }
					break;

				//RSB
				case 0x3:
					if (peek)
					{
						peekString = String.Format("RSB {0},{1:X}", GetRegisterName(dest_reg), operand);
						return;
					}

					result = (operand - input);
					SetRegisterValue(dest_reg, result);

					//Update condtion codes
					if (set_condition) { update_condition_arithmetic(operand, input, result, false); }
					break;

				//ADD
				case 0x4:
					if (peek)
					{
						peekString = String.Format("ADD {0},[{1:X},{2:X}]", GetRegisterName(dest_reg), input, operand);
						return;
					}

					result = (input + operand);		
					SetRegisterValue(dest_reg, result);

					//Update condtion codes
					if (set_condition) { update_condition_arithmetic(input, operand, result, true); }
					break;

				//ADC
				case 0x5:
					if (peek)
					{
						peekString = String.Format("ADC {0},{1:X}", GetRegisterName(dest_reg), operand);
						return;
					}

					//If no shift was performed, use the current Carry Flag for this math op
					if (shift_out == 2) { shift_out = (byte)(CarryFlag ? 1 : 0); }
					result = (input + operand + shift_out);
					SetRegisterValue(dest_reg, result);

					//Update condtion codes
					if (set_condition) { update_condition_arithmetic(input, (operand + shift_out), result, true); }
					break;

				//SBC
				case 0x6:
					if (peek)
					{
						peekString = String.Format("SBC {0},{1:X}", GetRegisterName(dest_reg), operand);
						return;
					}

					//If no shift was performed, use the current Carry Flag for this math op
					if (shift_out == 2) { shift_out = (byte)(CarryFlag ? 1 : 0); }

					result = (input - operand + shift_out - 1);

					if (peek)
					{
						peekString = String.Format("SBC {0},{1:X}", GetRegisterName(dest_reg), result);
						return;
					}


					SetRegisterValue(dest_reg, result);

					//Update condtion codes
					if (set_condition) { update_condition_arithmetic(input, (operand + shift_out - 1), result, false); }
					break;

				//RSC
				case 0x7:
					if (peek)
					{
						peekString = String.Format("RSC {0},{1:X}", GetRegisterName(dest_reg), operand);
						return;
					}

					//If no shift was performed, use the current Carry Flag for this math op
					if (shift_out == 2) { shift_out = (byte)(CarryFlag ? 1 : 0); }

					result = (operand - input + shift_out - 1);
					SetRegisterValue(dest_reg, result);

					//Update condtion codes
					if (set_condition) { update_condition_arithmetic((operand + shift_out - 1), input, result, false); }
					break;

				//TST
				case 0x8:
					if (peek)
					{
						peekString = String.Format("TST {0},{1:X}", GetRegisterName(src_reg), operand);
						return;
					}

					result = (input & operand);

					//Update condtion codes
					if (set_condition) { update_condition_logical(result, shift_out); }
					break;

				//TEQ
				case 0x9:
					if (peek)
					{
						peekString = String.Format("TEQ {0},{1:X}", GetRegisterName(src_reg), operand);
						return;
					}
					result = (input ^ operand);

					//Update condtion codes
					if (set_condition) { update_condition_logical(result, shift_out); }
					break;

				//CMP
				case 0xA:
					if (peek)
					{
						peekString = String.Format("CMP {0},{1:X}", GetRegisterName(src_reg), operand);
						return;
					}
					result = (input - operand);

					//Update condtion codes
					if (set_condition) { update_condition_arithmetic(input, operand, result, false); }
					break;

				//CMN
				case 0xB:
					if (peek)
					{
						peekString = String.Format("CMN {0},{1:X}", GetRegisterName(src_reg), operand);
						return;
					}

					result = (input + operand);

					//Update condtion codes
					if (set_condition) { update_condition_arithmetic(input, operand, result, true); }
					break;

				//ORR
				case 0xC:
					if (peek)
					{
						peekString = String.Format("ORR {0},{1:X}", GetRegisterName(src_reg), operand);
						return;
					}

					result = (input | operand);
					SetRegisterValue(dest_reg, result);

					//Update condtion codes
					if (set_condition) { update_condition_logical(result, shift_out); }
					break;

				//MOV
				case 0xD:
					if (peek)
					{
						peekString = String.Format("MOV {0},{1:X}", GetRegisterName(dest_reg), operand);
						return;
					}

					result = operand;
					SetRegisterValue(dest_reg, result);

					//Update condtion codes
					if (set_condition) { update_condition_logical(result, shift_out); }
					break;

				//BIC
				case 0xE:
					if (peek)
					{
						peekString = String.Format("BIC {0},{1:X}", GetRegisterName(dest_reg), operand);
						return;
					}

					result = (input & (~operand));
					SetRegisterValue(dest_reg, result);

					//Update condtion codes
					if (set_condition) { update_condition_logical(result, shift_out); }
					break;

				//MVN
				case 0xF:
					if (peek)
					{
						peekString = String.Format("MVN {0},{1:X}", GetRegisterName(dest_reg), operand);
						return;
					}

					result = ~operand;
					SetRegisterValue(dest_reg, result);

					//Update condtion codes
					if (set_condition) { update_condition_logical(result, shift_out); }
					break;
			}

			//Timings for PC as destination register
			if (dest_reg == 15)
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
		void psr_transfer(UInt32 current_arm_instruction, bool peek)
		{
			//Determine if an immediate or a register will be used as input (MSR only) - Bit 25
			bool use_immediate = ((current_arm_instruction & 0x2000000)!=0) ? true : false;

			//Determine access is for CPSR or SPSR - Bit 22
			byte psr = (byte) (((current_arm_instruction & 0x400000) != 0) ? 1 : 0);

			//Grab opcode
			byte op = (byte) (((current_arm_instruction & 0x200000) !=0) ? 1 : 0);

			switch (op)
			{
				//MRS
				case 0x0:
					{
						//Grab destination register - Bits 12-15
						byte dest_reg = (byte) ((current_arm_instruction >> 12) & 0xF);

						if (dest_reg == 15) 
						{ 
							throw new ArgumentException("Invalid Register"); 
						}

						// Store CPSR into destination register
						if (psr == 0) 
						{
							if (peek)
							{
								peekString = String.Format("MRS {0},CPSR", GetRegisterName(dest_reg));
								return;
							}
							SetRegisterValue(dest_reg, CPSR); 
						}
						// Store SPSR into destination register
						else 
						{
							if (peek)
							{
								peekString = String.Format("MRS {0},SPSR", GetRegisterName(dest_reg));
								return;
							}
							SetRegisterValue(dest_reg, SPSR); 
						}
					}
					break;

				//MSR
				case 0x1:
					{
						UInt32 input = 0;

						//Create Op Field mask
						UInt32 op_field_mask = 0;

						//Flag field - Bit 19
						if (((current_arm_instruction & 0x80000) != 0)) 
						{ 
							op_field_mask |= 0xFF000000; 
						}

						//Status field - Bit 18
						if (((current_arm_instruction & 0x40000) != 0))
						{
							op_field_mask |= 0x00FF0000;
							//std::cout << "CPU::Warning - ARM.6 MSR enabled access to Status Field \n";
						}

						//Extension field - Bit 17
						if (((current_arm_instruction & 0x20000) != 0))
						{
							op_field_mask |= 0x0000FF00;
							//std::cout << "CPU::Warning - ARM.6 MSR enabled access to Extension Field \n";
						}

						//Control field - Bit 15
						if (((current_arm_instruction & 0x10000) != 0)) 
						{ 
							op_field_mask |= 0x000000FF; 
						}

						//Use shifted 8-bit immediate as input
						if (use_immediate)
						{
							//Grab shift offset - Bits 8-11
							byte offset = (byte) ((current_arm_instruction >> 8) & 0xF);

							//Grab 8-bit immediate - Bits 0-7
							input = (current_arm_instruction) & 0xFF;

							rotate_right_special(ref input, offset);
						}

						//Use register as input
						else
						{
							//Grab source register - Bits 0-3
							byte src_reg = (byte) (current_arm_instruction & 0xF);

							if (src_reg == 15) 
							{ 
								//std::cout << "CPU::Warning - ARM.6 R15 used as Source Register \n"; 
							}

							input = GetRegisterValue(src_reg);
							input &= op_field_mask;
						}

						//Write into CPSR
						if (psr == 0)
						{
							if (peek)
							{
								peekString = String.Format("MSR CPSR");
								return;
							}

							CPSR &= ~op_field_mask;
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
							temp_spsr &= ~op_field_mask;
							temp_spsr |= input;
							SPSR = temp_spsr;
						}
					}
					break;
			}

			//Clock CPU and controllers - 1S
			//clock((reg.r15 + 4), false);
		}

		void DecodeSingleDataTransfer(UInt32 current_arm_instruction, bool peek)
        {
			//Grab Immediate-Offset flag - Bit 25
			byte offset_is_register = (byte) (((current_arm_instruction & 0x2000000) !=0) ? 1 : 0);

			//Grab Pre-Post bit - Bit 24
			byte pre_post = (byte)(((current_arm_instruction & 0x1000000) != 0) ? 1 : 0);

			//Grab Up-Down bit - Bit 23
			byte up_down = (byte)(((current_arm_instruction & 0x800000) != 0) ? 1 : 0);

			//Grab Byte-Word bit - Bit 22
			byte byte_word = (byte)(((current_arm_instruction & 0x400000) != 0) ? 1 : 0);

			//Grab Write-Back bit - Bit 21
			byte write_back = (byte)(((current_arm_instruction & 0x200000) != 0) ? 1 : 0);

			//Grab Load-Store bit - Bit 20
			byte load_store = (byte)(((current_arm_instruction & 0x100000) != 0) ? 1 : 0);

			//Grab the Base Register - Bits 16-19
			byte base_reg = (byte) ((current_arm_instruction >> 16) & 0xF);

			//Grab the Destination Register - Bits 12-15
			byte dest_reg = (byte) ((current_arm_instruction >> 12) & 0xF);

			UInt32 base_offset = 0;
			UInt32 base_addr = GetRegisterValue(base_reg);
			UInt32 value = 0;

			//Determine Offset - 12-bit immediate
			if (offset_is_register == 0) { base_offset = (current_arm_instruction & 0xFFF); }

			//Determine Offset - Shifted register
			else
			{
				//Grab register to use as offset - Bits 0-3
				byte offset_register = (byte) (current_arm_instruction & 0xF);
				base_offset = GetRegisterValue(offset_register);

				//Grab the shift type - Bits 5-6
				byte shift_type = (byte) ((current_arm_instruction >> 5) & 0x3);

				//Grab the shift offset - Bits 7-11
				byte shift_offset = (byte) ((current_arm_instruction >> 7) & 0x1F);

				//Shift the register
				switch (shift_type)
				{
					//LSL
					case 0x0:
						logical_shift_left(ref base_offset, shift_offset);
						break;

					//LSR
					case 0x1:
						logical_shift_right(ref base_offset, shift_offset);
						break;

					//ASR
					case 0x2:
						arithmetic_shift_right(ref base_offset, shift_offset);
						break;

					//ROR
					case 0x3:
						rotate_right(ref base_offset, shift_offset);
						break;
				}
			}

			//Increment or decrement before transfer if pre-indexing
			if (pre_post == 1)
			{
				if (up_down == 1) { base_addr += base_offset; }
				else { base_addr -= base_offset; }
			}

			//Clock CPU and controllers - 1N
			//clock(reg.r15, true);

			//Store Byte or Word
			if (load_store == 0)
			{
				if (peek)
				{
					peekString = String.Format("STR {0} , [{1},{2:X}]", GetRegisterName(dest_reg), GetRegisterName(base_reg), base_offset);
					return;
				}

				if (byte_word == 1)
				{
					value = GetRegisterValue(dest_reg);
					if (dest_reg == 15) { value += 4; }
					value &= 0xFF;

					//mem_check_8(base_addr, value, false);
					gba.Memory.WriteByte(base_addr, (byte)value);
				}

				else
				{
					value = GetRegisterValue(dest_reg);
					if (dest_reg == 15) { value += 4; }

					//mem->write_u32(base_addr, value);
					gba.Memory.WriteWord(base_addr, value);
				}

				//Clock CPU and controllers - 1N
				//clock(base_addr, true);
			}

			//Load Byte or Word
			else
			{
				if (peek)
				{
					peekString = String.Format("LDR {0} , [{1},{2:X}]", GetRegisterName(dest_reg), GetRegisterName(base_reg), base_offset);
					return;
				}

				if (byte_word == 1)
				{
					//Clock CPU and controllers - 1I
					//mem_check_8(base_addr, value, true);
					value = gba.Memory.ReadByte(base_addr);



					//clock();

					//Clock CPU and controllers - 1N
					//if (dest_reg == 15) { clock((reg.r15 + 4), true); }

					SetRegisterValue(dest_reg, value);
				}

				else
				{
					//Clock CPU and controllers - 1I
					//mem_check_32(base_addr, value, true);
					//base_addr += 8;
					gba.Memory.ReadWriteWord_Checked(base_addr, ref value, true);
					//value = gba.Memory.ReadWord(base_addr);

					//clock();

					//Clock CPU and controllers - 1N
					//if (dest_reg == 15) { clock((reg.r15 + 4), true); }

					SetRegisterValue(dest_reg, value);
				}
			}

			//Increment or decrement after transfer if post-indexing
			if (pre_post == 0)
			{
				if (up_down == 1) { base_addr += base_offset; }
				else { base_addr -= base_offset; }
			}

			//Write back into base register
			//Post-indexing ALWAYS does this. Pre-Indexing does this optionally
			if ((pre_post == 0) && (base_reg != dest_reg)) { SetRegisterValue(base_reg, base_addr); }
			else if ((pre_post == 1) && (write_back == 1) && (base_reg != dest_reg)) { SetRegisterValue(base_reg, base_addr); }

			//Timings for LDR - PC
			if ((dest_reg == 15) && (load_store == 1))
			{
				//Clock CPU and controllser - 2S
				//clock(reg.r15, false);
				//clock((reg.r15 + 4), false);
				//needs_flush = true;
			}

			//Timings for LDR - No PC
			else if ((dest_reg != 15) && (load_store == 1))
			{
				//Clock CPU and controllers - 1S
				//clock(reg.r15, false);
			}
		}


		// TODO: This will probably need to be unwound for speed!
		UInt32 ExtractValue(UInt32 value, int startBit, int bitCount)
        {
			UInt32 mask;
			mask = ((1U << bitCount) - 1U) << startBit;
			return (UInt32) ((value & mask) >> startBit);
		}


		/****** Updates the condition codes in the CPSR register after logical operations ******/
		void update_condition_logical(UInt32 result, byte shift_out)
		{
			//Negative flag
			if ((result & 0x80000000) != 0) { CPSR |= (UInt32) StatusFlag.Negative; }
			else { CPSR &= (UInt32)~StatusFlag.Negative; }

			//Zero flag
			if (result == 0) { CPSR |= (UInt32)StatusFlag.Zero; }
			else { CPSR &= (UInt32)~StatusFlag.Zero; }

			//Carry flag
			if (shift_out == 1) { CPSR |= (UInt32)StatusFlag.Carry; }
			else if (shift_out == 0) { CPSR &= (UInt32)~StatusFlag.Carry; }
		}



		/****** Updates the condition codes in the CPSR register after arithmetic operations ******/
		void update_condition_arithmetic(UInt32 input, UInt32 operand, UInt32 result, bool addition)
		{
			//Negative flag
			if ((result & 0x80000000)!= 0) { CPSR |= (UInt32)StatusFlag.Negative; }
			else { CPSR &= (UInt32)~StatusFlag.Negative; }

			//Zero flag
			if (result == 0) { CPSR |= (UInt32)StatusFlag.Zero; }
			else { CPSR &= (UInt32)~StatusFlag.Zero; }

			//Carry flag - Addition
			if ((operand > (0xFFFFFFFF - input)) && (addition)) { CPSR |= (UInt32)StatusFlag.Carry; }

			//Carry flag - Subtraction
			else if ((operand <= input) && (!addition)) { CPSR |= (UInt32)StatusFlag.Carry; }

			else { CPSR &= (UInt32)~StatusFlag.Carry; }

			//Overflow flag
			byte input_msb = (byte) (((input & 0x80000000)!=0) ? 1 : 0);
			byte operand_msb = (byte) (((operand & 0x80000000) != 0) ? 1 : 0);
			byte result_msb = (byte) (((result & 0x80000000) != 0) ? 1 : 0);

			if (addition)
			{
				if (input_msb != operand_msb) { CPSR &= (UInt32)~StatusFlag.Overflow; }

				else
				{
					if ((result_msb == input_msb) && (result_msb == operand_msb)) { CPSR &= (UInt32)~StatusFlag.Overflow; }
					else { CPSR |= (UInt32)StatusFlag.Overflow; }
				}
			}
			else
			{
				if (input_msb == operand_msb) { CPSR &= (UInt32)~StatusFlag.Overflow; }

				else
				{
					if (result_msb == operand_msb) { CPSR |= (UInt32)StatusFlag.Overflow; }
					else { CPSR &= (UInt32)~StatusFlag.Overflow; }
				}
			}
		}



		/****** Performs 32-bit logical shift left - Returns Carry Out ******/
		byte logical_shift_left(ref UInt32 input, byte offset)
		{
			byte carry_out = 0;

			if (offset > 0)
			{
				//Test for carry
				//Perform LSL #(n-1), if Bit 31 is 1, we know it will carry out
				UInt32 carry_test = input << (offset - 1);
				carry_out = (byte) (((carry_test & 0x80000000) != 0) ? 1 : 0);

				if (offset >= 32) { input = 0; }
				else { input <<= offset; }
			}

			//LSL #0
			//No shift performed, carry flag not affected, set it to something not 0 or 1 to check!
			else { carry_out = 2; }

			return carry_out;
		}

		/****** Performs 32-bit logical shift right - Returns Carry Out ******/
		byte logical_shift_right(ref UInt32 input, byte offset)
		{
			byte carry_out = 0;

			if (offset > 0)
			{
				//Test for carry
				//Perform LSR #(n-1), if Bit 0 is 1, we know it will carry out
				UInt32 carry_test = input >> (offset - 1);
				carry_out = (byte)(((carry_test & 0x1) != 0) ? 1 : 0);

				if (offset >= 32) { input = 0; }
				else { input >>= offset; }
			}

			//LSR #0
			//Same as LSR #32, input becomes zero, carry flag is Bit 31 of input
			else
			{
				carry_out = (byte) (((input & 0x80000000) != 0) ? 1 : 0);
				input = 0;
			}

			return carry_out;
		}

		/****** Performs 32-bit arithmetic shift right - Returns Carry Out ******/
		byte arithmetic_shift_right(ref UInt32 input, byte offset)
		{
			byte carry_out = 0;

			if (offset > 0)
			{
				byte high_bit = (byte) (((input & 0x80000000)!=0) ? 1 : 0);

				//Basically LSR, but bits become Bit 31
				for (int x = 0; x < offset; x++)
				{
					carry_out = (byte) (((input & 0x1) != 0) ? 1 : 0);
					input >>= 1;
					if (high_bit == 1) { input |= 0x80000000; }
				}
			}

			//ASR #0
			//Same as ASR #32, input becomes 0xFFFFFFFF or 0x0 depending on Bit 31 of input
			//Carry flag set to 0 or 1 depending on Bit 31 of input
			else
			{
				if ((input & 0x80000000) != 0) { input = 0xFFFFFFFF; carry_out = 1; }
				else { input = 0; carry_out = 0; }
			}

			return carry_out;
		}

		/****** Performs 32-bit rotate right ******/
		public byte rotate_right(ref UInt32 input, byte offset)
		{
			byte carry_out = 0;

			if (offset > 0)
			{
				//Perform ROR shift on immediate
				for (int x = 0; x < offset; x++)
				{
					carry_out = (byte) (input & 0x1);
					input >>= 1;

					if (carry_out != 0) 
					{ 
						input |= 0x80000000; 
					}
				}
			}

			//ROR #0
			//Same as RRX #1, which is similar to ROR #1, except Bit 31 now becomes the old carry flag
			else
			{
				byte old_carry = (byte) (CarryFlag ? 1 : 0);
				carry_out = (byte) (input & 0x1);
				input >>= 1;

				if (old_carry != 0) { input |= 0x80000000; }
			}

			return carry_out;
		}


		/****** Performs 32-bit rotate right - For ARM.5 Data Processing when Bit 25 is 1 ******/
		void rotate_right_special(ref UInt32 input, byte offset)
		{
			if (offset > 0)
			{
				//Perform ROR shift on immediate
				for (int x = 0; x < (offset * 2); x++)
				{
					byte carry_out = (byte) (input & 0x1);
					input >>= 1;

					if (carry_out != 0) { input |= 0x80000000; }
				}
			}
		}

	}
}
