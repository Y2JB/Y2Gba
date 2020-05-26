using System;
using Gba.Core;

namespace GbaDebugger
{
    // We bolt onto the instruction the state it had when it executed - pc, operand etc
    public class StoredInstruction : ArmInstruction
    {
        // These methods are only used when peeking the instruction, not when exectuing as then the data needs to be fetched 
        public UInt32 PC { get; set; }

        // NB: I'm not setting the handler as this is purely for debugging!
        public static StoredInstruction DeepCopy(ArmInstruction instruction)
        {
            if (instruction == null) return new StoredInstruction("UNKNOWN INSTRUCTION", InstructionCatagory.Unkonwn, null);
            return new StoredInstruction(instruction.Mnemonic, instruction.Catagory, null)
            {
                Operand = instruction.Operand,
            };       
        }

        public static StoredInstruction DeepCopy(StoredInstruction instruction)
        {
            return new StoredInstruction(instruction.Mnemonic, instruction.Catagory, null)
            {
                Operand = instruction.Operand,
                PC = instruction.PC,
            };
        }

        public StoredInstruction(string mnemonic, InstructionCatagory catagory, Action<UInt32> handler) : base(mnemonic, catagory, null)
        {
         
        }

        public override String ToString()
        {
           /*
            if (HasOperand)
            {
                string instructionWithOperand = String.Format(Name, Operand);
                return String.Format("({0:X2})  ->  {1}", PC, instructionWithOperand);

            }
            else*/
            {
                return String.Format("({0:X2})  ->  {1}", PC, Mnemonic);
            }
        }
    }
}
