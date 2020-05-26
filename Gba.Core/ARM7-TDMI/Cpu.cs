using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    public partial class Cpu
    {
        public enum CpuState
        {
            Arm,
            Thumb
        }

        public CpuState State { get; private set; }

        GameboyAdvance gba;


        public Cpu(GameboyAdvance gba)
        {
            this.gba = gba;

            RegisterInstructions();            
        }


        public void Reset()
        {
            State = CpuState.Arm;
            PC = 0x08000000;

            ArmInstruction firstInstruction = DecodeInstruction(gba.Rom.EntryPoint);
            ExecuteArmInstruction(firstInstruction);
        }


        public void Step()
        {
            // TODO: This is newing wayyyyyyy too much
            ArmInstruction instruction = DecodeInstruction(gba.Memory.ReadWord(PC));
            ExecuteArmInstruction(instruction);
        }


        void ExecuteArmInstruction(ArmInstruction instruction)
        {
            // Conditional
            if(instruction.Conditional != ConditionalExecution.AL)
            {
                if(CondtionalHandlers[(int) instruction.Conditional].Invoke() == false)
                {
                    // Conditional return false, we do not execture the instruction
                    return;
                }
            }

            instruction.Handler(instruction.Operand);
        }



        public override String ToString()
        {
            return String.Format("PC - 0x{0:X8}{1}",
                PC, Environment.NewLine);
        }
    }
}
