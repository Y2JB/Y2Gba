using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    // GBA Cpu is ArmV4
    public partial class Cpu
    {
        public enum CpuState
        {
            Arm,
            Thumb
        }
        public CpuState State { get; private set; }

        public enum CpuMode
        {
            User,
            System,
            FIQ,            // Fast Interrupt Request (Not used on GBA)
            Supervisor,
            Abort,
            IRQ,
            Undefined
        }   
        public CpuMode Mode { get; private set; }

        public const int Pipeline_Size = 3;
        public UInt32[] InstructionPipeline { get; private set; }
        public int NextPipelineInsturction { get; private set; }

        bool requestFlushPipeline;

        // 16Mhz Cpu
        public const UInt32 Cycles_Per_Second = 16777216;
        public UInt32 Cycles { get; private set; }

        public Memory Memory { get; private set; }
        GameboyAdvance Gba { get; set; }


        public Cpu(GameboyAdvance gba)
        {
            this.Gba = gba;
            Memory = gba.Memory;
            InstructionPipeline = new UInt32[Pipeline_Size];            
            RegisterConditionalHandlers();
        }


        public void Reset()
        {
            State = CpuState.Arm;
            Mode = CpuMode.System;

            PC = 0x08000000;
            SP = 0x03007F00;
            //R13 = reg.r13_fiq = reg.r13_abt = reg.r13_und = 0x03007F00;
            //reg.r13_svc = 0x03007FE0;
            //reg.r13_irq = 0x03007FA0;
            //reg.r15 = 0x8000000;
            CPSR = 0x5F;

            Cycles = 0;

            NextPipelineInsturction = 0;
            RefillPipeline();
        }


        // Throw away what's in the pipeline and refill before executing another instruction
        public void FlushPipeline()
        {            
            InstructionPipeline[0] = 0;
            InstructionPipeline[1] = 0;
            InstructionPipeline[2] = 0;
        }


        // Arm7 works with a fetch, decode, execute pipeline so the PC is always 2 instructions ahead of the executing instruction (8 bytes)
        // If a branch or some other op has invalidated the pipeline, refill it fro scratch before we execute anything else
        public void RefillPipeline()
        {
            NextPipelineInsturction = 0;

            if (State == CpuState.Thumb)
            {
                InstructionPipeline[0] = Gba.Memory.ReadHalfWord(PC);
                PC += 2;
                InstructionPipeline[1] = Gba.Memory.ReadHalfWord(PC);
                PC += 2;
                InstructionPipeline[2] = Gba.Memory.ReadHalfWord(PC);
            }
            else
            {
                InstructionPipeline[0] = Gba.Memory.ReadWord(PC);
                PC += 4;
                InstructionPipeline[1] = Gba.Memory.ReadWord(PC);
                PC += 4;
                InstructionPipeline[2] = Gba.Memory.ReadWord(PC);
            }
        }


        // After an instruction executes we move the pieline forward one instruction
        public void PipelineAdvance()
        {
            // nextPipelineInsturction becomes the back of the queue, then we adjust it
            if (State == CpuState.Thumb)
            {
                PC += 2U;
                InstructionPipeline[NextPipelineInsturction] = Gba.Memory.ReadHalfWord(PC);
            }
            else
            {
                PC += 4U;
                InstructionPipeline[NextPipelineInsturction] = Gba.Memory.ReadWord(PC);
            }

            NextPipelineInsturction++;
            if (NextPipelineInsturction >= Pipeline_Size) NextPipelineInsturction = 0;
        }


        public void Cycle(uint cycles)
        {
            Cycles += cycles;

            while (cycles > 0)
            {
                Gba.LcdController.Step();
                Gba.Joypad.Step();
                //dmg.timer.Step();

                cycles--;
            }
        }


        public void Step()
        {
            if (State == CpuState.Thumb)
            {
                DecodeAndExecuteThumbInstruction((ushort)InstructionPipeline[NextPipelineInsturction]);
            }
            else
            {
                DecodeAndExecuteArmInstruction(InstructionPipeline[NextPipelineInsturction]);
            }


            if (requestFlushPipeline == false)
            {
                PipelineAdvance();
            }
            else
            {
                FlushPipeline();
                requestFlushPipeline = false;
                RefillPipeline();
            }

            // Even if conditional prevented execution? 
            Cycle(1);
        }


   
        public override String ToString()
        {
            return String.Format("PC - 0x{0:X8}{1}R0  - {2:X8}{3}R1  - {4:X8}{5}R2  - {6:X8}{7}R3  - {8:X8}{9}R4  - {10:X8}{11}R5  - {12:X8}{13}R6  - {14:X8}{15}R7  - {16:X8}{17}R8  - {18:X8}{19}R9  - {20:X8}{21}R10 - {22:X8}{23}R11 - {24:X8}{25}R12 - {26:X8}{27}SP - {28:X8}{29}LR - {30:X8}{31}CPSR - {32:X8}{33}PC(Adjusted) - {34:X8}{35}",
                PC, Environment.NewLine,
                R0, Environment.NewLine, 
                R1, Environment.NewLine, 
                R2, Environment.NewLine, 
                R3, Environment.NewLine, 
                R4, Environment.NewLine, 
                R5, Environment.NewLine, 
                R6, Environment.NewLine, 
                R7, Environment.NewLine, 
                R8, Environment.NewLine, 
                R9, Environment.NewLine, 
                R10, Environment.NewLine,
                R11, Environment.NewLine, 
                R12, Environment.NewLine,                 
                SP, Environment.NewLine,
                LR, Environment.NewLine,
                CPSR, Environment.NewLine,
                PC_Adjusted, Environment.NewLine);
        }
    }
}
