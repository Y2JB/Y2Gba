using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    // I'm listing these in binary as it makes referning the docs a bit easier
    public enum InstructionCatagory : UInt32
    {
        Unkonwn = 0,

        Branch = 0b10100000,
        Branch_Exchange = 0b00010010,
        DataProcessing = 0b00100000,

    }

    public enum Opcodes : UInt32
    {
        // DataProcessing
        AND,
        EOR,
        SUB,
        RSB,
        ADD,
        ADC,
        SBC,
        RSC,
        TST,
        TEQ,
        CMP,
        CMN,
        ORR,
        MOV,
        BIC,
        MVN,

        // Branch
        B,
        BL,

        // Branch_Exchange


        OpCodeCount
    }

    public partial class Cpu
    {      
        public ArmInstruction[] ArmInstructions;
    
        void RegisterInstructions()
        {
            ArmInstructions = new ArmInstruction[(int)Opcodes.OpCodeCount];

            ArmInstructions[(int) Opcodes.B]    = new ArmInstruction("B   {0:X8}", InstructionCatagory.Branch, (nn) => Branch(nn));
            ArmInstructions[(int) Opcodes.BL]   = new ArmInstruction("BL  {0:X8}", InstructionCatagory.Branch, (nn) => BranchAndLink(nn));
        }


    }
}
