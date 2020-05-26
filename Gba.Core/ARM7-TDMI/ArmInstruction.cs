using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{

	/*
struct ARMInstructionInfo {
		uint32_t opcode;
		union ARMOperand op1;
		union ARMOperand op2;
		union ARMOperand op3;
		union ARMOperand op4;
		struct ARMMemoryAccess memory;
		int operandFormat;
		unsigned execMode : 1;
		bool traps : 1;
		bool affectsCPSR : 1;
		unsigned branchType : 3;
		unsigned condition : 4;
		unsigned mnemonic : 6;
		unsigned iCycles : 3;
		unsigned cCycles : 4;
		unsigned sInstructionCycles : 4;
		unsigned nInstructionCycles : 4;
		unsigned sDataCycles : 10;
		unsigned nDataCycles : 10;
};

 */

	public partial class ArmInstruction
    {
		public InstructionCatagory Catagory { get; set; }	
		public string Mnemonic { get; set; }
		public Action<UInt32> Handler { get; set; }


		// Ctor only inits the static data 
		public ArmInstruction(string mnemonic, InstructionCatagory catagory, Action<UInt32> handler)
		{
			Mnemonic = mnemonic;
			Catagory = catagory;
			Handler = handler;
		}


		// Our static instruction array has everything above this comment setup when the program starts and everything below is set up when the instruction is decoded


		public ConditionalExecution Conditional { get; set; }
		public UInt32 Operand { get; set; }

		
	}
}
