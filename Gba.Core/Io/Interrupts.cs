using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics.X86;
using System.Text;

namespace Gba.Core
{
    public class Interrupts
    {
        // Note that there is another 'master enable flag' directly in the CPUs Status Register(CPSR) accessible in privileged modes, see CPU reference for details.

        // 0=Disable All, 1=See IE register
        // 0x4000208 
        public byte InterruptMasterEnable { get; set; }

        // 0x4000200
        public ushort InterruptEnableRegister { get; set; }

        // 0x4000202 / 3
        public byte InterruptRequestFlags0 { get; set; }
        public byte InterruptRequestFlags1 { get; set; }
        public ushort InterruptRequestFlags 
        { 
            get { return (ushort)((InterruptRequestFlags1 << 8) | InterruptRequestFlags0); } 
            set { InterruptRequestFlags0 = (byte) (value & 0x00FF); InterruptRequestFlags1 = (byte) ((value & 0xFF00)>>8); }
        }



        // 0 = Disable
        public enum InterruptType : UInt16
        {
            VBlank          = 1 << 0,
            HBlank          = 1 << 1,
            VCounterMatch   = 1 << 2,
            Timer0Overflow  = 1 << 3,
            Timer1Overflow  = 1 << 4,
            Timer2Overflow  = 1 << 5,
            Timer3Overflow  = 1 << 6,
            SerialComms     = 1 << 7,
            Dma0            = 1 << 8,
            Dma1            = 1 << 9,
            Dma2            = 1 << 10,
            Dma3            = 1 << 11,
            Keypad          = 1 << 12,
            GamePak         = 1 << 13
        }


        GameboyAdvance gba;

        public Interrupts(GameboyAdvance gba)
        {
            this.gba = gba;
        }


        public void RequestInterrupt(InterruptType interrupt)
        {
            InterruptRequestFlags |= (ushort)interrupt;
        }

        bool InterruptPending()
        {
            return InterruptRequestFlags != 0;
        }

        // Jumps to or exits an IRQ / hardware interrupt 
        public void ProcessInterrupts()
        {
            if((InterruptMasterEnable!=0) && gba.Cpu.IrqDisableFlag == false && InterruptPending())
            {
                // Save the flags before we do anything. The interrupt handler will restore them when it is done
                gba.Cpu.SPSR_Irq = gba.Cpu.CPSR;

                gba.Cpu.SetFlag(Cpu.StatusFlag.IrqDisable);
                gba.Cpu.Mode = Cpu.CpuMode.IRQ;
                UInt32 nextInstruction = (gba.Cpu.State == Cpu.CpuState.Arm ? 4u : 2u);
                gba.Cpu.LR = gba.Cpu.PC_Adjusted + nextInstruction;
                gba.Cpu.PC = 0x18;
                gba.Cpu.requestFlushPipeline = true;

                gba.Cpu.State = Cpu.CpuState.Arm;

                // It is the games job to clear interurpt flags etc

                // Return is handled by the subs instruction, any data processing instruction with the S flag set and r15 as its destination restores the CPSR
            }
        }


       

    }
}
