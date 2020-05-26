using System;
using System.Collections.Generic;
using System.Text;


namespace Gba.Core
{




	public partial class Cpu
    {
		

		//Dictionary<InstructionCatagory, Func<UInt32, ArmInstruction>> catagorgyHandlers = new Dictionary<InstructionCatagory, Func<UInt32, ArmInstruction>>();



        public ArmInstruction DecodeInstruction(UInt32 rawInstruction)
        {

			// TODO: Extract conditional
			ConditionalExecution conditional = (ConditionalExecution)((rawInstruction & 0xF0000000) >> 28);

			// You can extract the instruction catagory by examining bits 20-27 & 4-7

			// The byte between bits 20 & 27
			int highbits = (int)((rawInstruction & 0x0FF00000) >> 20);

			int lowBits =  (int)((rawInstruction & 0x000000F0) >> 4);

			// We start by examining the top 3 bits of the high byte 
			int threeBitCode = ((highbits & 0xE0) >> 5);

			// 101 == Branch - B BL etc
			if (threeBitCode == 5)
            {
				return DecodeBranch(rawInstruction, conditional);
			}
			else if (threeBitCode == 0)
            {
				if(highbits == (UInt32) InstructionCatagory.Branch_Exchange)
                {
					return DecodeBranchExchange(rawInstruction, conditional);
				}

            }
			// 001 == DataProcessing - MOV, ADD, ORR, EOR etc 
			else if(threeBitCode == 1)
            {
				return DecodeDataProcessing(rawInstruction, conditional);
			}

			
			throw new ArgumentException("Unable to decode instruction");


/*
			if (Enum.IsDefined(typeof(InstructionCatagory), highbits) == false)
			{
				throw new ArgumentException("Unable to decode instruction");
            }

			InstructionCatagory catagory = (InstructionCatagory) highbits;
*/
        }


		// Also includes Branch & Link (L)
		ArmInstruction DecodeBranch(UInt32 rawInstruction, ConditionalExecution conditional)
        {
			// |_Cond__|1_0_1|L|___________________Offset______________________| B,BL,BLX

			UInt32 opCode  = (UInt32)((rawInstruction & 0x01000000 >> 24));
			UInt32 operand = (UInt32) (rawInstruction & 0x00FFFFFF);

			ArmInstruction instr;

			switch (opCode)
            {
				// B (Branch)
				case 0:
					//return new ArmInstruction("B", InstructionCatagory.Branch, ConditionalExecution.AL, () => PC = PC + 8 + operand * 4);
					instr = ArmInstructions[(int)Opcodes.B];
					break;

				// BL (Branch and Link)
				case 1:					
					instr = ArmInstructions[(int)Opcodes.BL];
					break;

				default:
					throw new ArgumentException("Failed to parse branch instruction");
            }

			instr.Conditional = conditional;
			instr.Operand = operand;
			return instr;
		}


		ArmInstruction DecodeBranchExchange(UInt32 rawInstruction, ConditionalExecution conditional)
		{
			// |_Cond__|0_0_0_1_0_0_1_0_1_1_1_1_1_1_1_1_1_1_1_1|0_0|L|1|__Rn___| BX,BLX
			return null;
		}

		ArmInstruction DecodeDataProcessing(UInt32 rawInstruction, ConditionalExecution conditional)
		{
			// |_Cond__|0_0_1|___Op__|S|__Rn___|__Rd___|_Shift_|___Immediate___| DataProc

			UInt32 opCode = (UInt32)((rawInstruction & 0x01E00000 >> 21));

			return null;
		}
		

	}
}
