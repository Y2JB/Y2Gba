using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    // GBA Cpu Arm7TDMI (ArmV4)
    public partial class Cpu
    {
        public enum CpuState
        {
            Arm,
            Thumb
        }
        CpuState state;
        public CpuState State { get { return state; }  set { state = value; if (state == CpuState.Thumb) SetFlag(StatusFlag.ThumbExecution); else ClearFlag(StatusFlag.ThumbExecution); } }

        CpuMode mode;
        public CpuMode Mode { get { return mode; } set { mode = value; UpdateCsprModeBits(value); } }

        public const int Pipeline_Size = 3;
        public UInt32[] InstructionPipeline { get; private set; }
        public int NextPipelineInsturction { get; private set; }

        public bool requestFlushPipeline { get; set;  }

        // 16Mhz Cpu
        public const UInt32 Cycles_Per_Second = 16777216;
        public UInt32 Cycles { get; private set; }

        UInt32 nonSequentialAccessTime = 4;
        UInt32 sequentialAccessTime = 2;
        MemoryRegister16 waitStateControlRegister;

        public Memory Memory { get; private set; }
        GameboyAdvance Gba { get; set; }

        Action RefillPipeline;
        Action PipelineAdvance;

        public Cpu(GameboyAdvance gba)
        {
            this.Gba = gba;
            Memory = gba.Memory;
            InstructionPipeline = new UInt32[Pipeline_Size];
            CalculateArmDecodeLookUpTable();
            CalculateThumbDecodeLookUpTable();
            RegisterConditionalHandlers();

            MemoryRegister8WithSetHook r0 = new MemoryRegister8WithSetHook(gba.Memory, 0x4000204, true, true);
            MemoryRegister8WithSetHook r1 = new MemoryRegister8WithSetHook(gba.Memory, 0x4000205, true, true);            
            waitStateControlRegister = new MemoryRegister16(gba.Memory, 0x4000204, true, true, r0, r1);

            Action<byte, byte> waitStateSet = (oldValue, newValue) =>
            {
                //Always make sure Bit 15 is 0 and Read-Only in GBA mode
                //memory_map[WAITCNT + 1] &= ~0x80;

                ushort waitControl = (ushort) (waitStateControlRegister.Value & ~0x80);

                //Determine first access cycles (Non-Sequential)
                switch ((waitControl >> 2) & 0x3)
                {
                    case 0x0: nonSequentialAccessTime = 4; break;
                    case 0x1: nonSequentialAccessTime = 3; break;
                    case 0x2: nonSequentialAccessTime = 2; break;
                    case 0x3: nonSequentialAccessTime = 8; break;
                }

                //Determine second access cycles (Sequential)
                switch ((waitControl >> 4) & 0x1)
                {
                    case 0x0: sequentialAccessTime = 2; break;
                    case 0x1: sequentialAccessTime = 1; break;
                }
            };
            r0.OnSet = waitStateSet;
            r1.OnSet = waitStateSet;
        }


        public void Reset()
        {
            State = CpuState.Arm;
            Mode = CpuMode.System;

            // Boot from BIOS
            //PC = 0x00000000;


            // Jump straight to the game rom
            PC = 0x08000000;
            SP = 0x03007F00;
            R13 = SP_Fiq = SP_Abt = SP_Und = 0x03007F00;
            SP_Svc = 0x03007FE0;
            SP_Irq = 0x03007FA0;
            //reg.r15 = 0x8000000;
            //CPSR = 0x5F;
            

            Cycles = 0;

            RefillPipeline = RefillPipelineSlow;
            PipelineAdvance = PipelineAdvanceSlow;

            NextPipelineInsturction = 0;
            RefillPipeline();
        }


        // Throw away what's in the pipeline and refill before executing another instruction
        public void FlushPipeline()
        {            
            InstructionPipeline[0] = 0;
            InstructionPipeline[1] = 0;
            InstructionPipeline[2] = 0;


            // We're executing from ROM, switch to the fast read
            if (PC >= 0x08000000 && PC <= 0x09FFFFFF)
            {
                RefillPipeline = RefillPipelineFastForRom;
                PipelineAdvance = PipelineAdvanceFastForRom;
            }
            else
            {
                // If we find ourselves executing from ROM or BIOS then we'll switch to faster versions otherwise we need to do the slow version
                RefillPipeline = RefillPipelineSlow;
                PipelineAdvance = PipelineAdvanceSlow;
            }
        }


        // Arm7 works with a fetch, decode, execute pipeline so the PC is always 2 instructions ahead of the executing instruction (8 bytes)
        // If a branch or some other op has invalidated the pipeline, refill it fro scratch before we execute anything else
        public void RefillPipelineSlow()
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
        public void PipelineAdvanceSlow()
        {           
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

            // nextPipelineInsturction becomes the back of the queue, then we adjust it
            NextPipelineInsturction++;
            if (NextPipelineInsturction >= Pipeline_Size) NextPipelineInsturction = 0;
        }


        
        public void RefillPipelineFastForRom()
        {
            NextPipelineInsturction = 0;
            
            UInt32 pc = PC - 0x08000000;

            if (State == CpuState.Thumb)
            {
                InstructionPipeline[0] = Gba.Rom.ReadHalfWordFast(pc);
                InstructionPipeline[1] = Gba.Rom.ReadHalfWordFast(pc + 2);
                InstructionPipeline[2] = Gba.Rom.ReadHalfWordFast(pc + 4);

                PC += 4;
            }
            else
            {
                InstructionPipeline[0] = Gba.Rom.ReadWordFast(pc);
                InstructionPipeline[1] = Gba.Rom.ReadWordFast(pc + 4);
                InstructionPipeline[2] = Gba.Rom.ReadWordFast(pc + 8);

                PC += 8;
            }
        }


        // We have cached all the ROM data at both 16 & 32 bit boundaries. This means that when we read the ROM we can just grab 16 or 32bit values in one shot rather than 4*ReadByte()
        public void PipelineAdvanceFastForRom()
        {
            if (State == CpuState.Thumb)
            {
                PC += 2U;
                InstructionPipeline[NextPipelineInsturction] = Gba.Rom.ReadHalfWordFast(PC - 0x08000000);
            }
            else
            {
                PC += 4U;
                InstructionPipeline[NextPipelineInsturction] = Gba.Rom.ReadWordFast(PC - 0x08000000);
            }

            NextPipelineInsturction++;
            if (NextPipelineInsturction >= Pipeline_Size) NextPipelineInsturction = 0;
        }


        // Determines how many cycles should elapse when emulating Wait States and access timing
        void CycleWithAccessTiming(UInt32 accessAddr, bool firstAccess)
        {
            /*
            // TODO: keep this commented out unless testing it
            if(isPeeking)
            {
                throw new ArgumentException("Bad Peek");
            }
            */

            UInt32 cycleCount = 1;

            // vram 
            // TODO: Need to consider mirroring?
            //if ((accessAddr >= 0x5000000) && (accessAddr <= 0x70003FF))
            if ((accessAddr >= 0x5000000) && (accessAddr <= 0x7FFFFFF))
            {
                // Access time is +1 if outside of H-Blank & V-Blank. Otherwise each 16-bit access is 1 cycle
                if (Gba.LcdController.Mode == LcdController.LcdMode.ScanlineRendering) 
                { 
                    cycleCount++; 
                }
            }

            // Wait State 0
            else if ((accessAddr >= 0x8000000) && (accessAddr <= 0x9FFFFFF))
            {
                if (firstAccess) 
                { 
                    cycleCount += nonSequentialAccessTime; 
                }
                else 
                { 
                    cycleCount += sequentialAccessTime; 
                }
            }

            Cycles += cycleCount;

            Gba.Scheduler.Check();
        }


        // Cycles a single instrcution / cycle 
        public void Cycle()
        {
            Cycles++;
            Gba.Scheduler.Check();
        }


        //Queue<UInt32> executionHistory = new Queue<uint>();
        public void Step()
        {
            //executionHistory.Enqueue(PC_Adjusted);
            //if (executionHistory.Count > 128) executionHistory.Dequeue();

            if (State == CpuState.Thumb)
            {
                DecodeAndExecuteThumbInstruction((ushort)InstructionPipeline[NextPipelineInsturction]);
            }
            else
            {
                DecodeAndExecuteArmInstruction(InstructionPipeline[NextPipelineInsturction]);
            } 


            // Handle interrupts
            Gba.Interrupts.ProcessInterrupts();

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
        }


   
        public override String ToString()
        {
            string flags = String.Format("[{0}{1}{2}{3}]", NegativeFlag ? "N" : "-", ZeroFlag ? "Z" : "-", CarryFlag ? "C" : "-", OverflowFlag ? "V" : "-");

            return String.Format("Mode - {0}{1}R0  - {2:X8}{3}R1  - {4:X8}{5}R2  - {6:X8}{7}R3  - {8:X8}{9}R4  - {10:X8}{11}R5  - {12:X8}{13}R6  - {14:X8}{15}R7  - {16:X8}{17}R8  - {18:X8}{19}R9  - {20:X8}{21}R10 - {22:X8}{23}R11 - {24:X8}{25}R12 - {26:X8}{27}SP  - {28:X8}{29}LR  - {30:X8}{31}PC  - {32:X8}{33}PCX - {34:X8}{35}CPSR - {36:X8}{37}{38}{39}SPSR - {40:X8}{41}",
                
                Mode, Environment.NewLine,
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
                PC, Environment.NewLine,
                PC_Adjusted, Environment.NewLine,
                CPSR, Environment.NewLine,
                flags, Environment.NewLine,
                SPSR, Environment.NewLine);
        }
    }
}
