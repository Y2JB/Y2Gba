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
        public byte InterruptEnableRegister0 { get; set; }
        public byte InterruptEnableRegister1 { get; set; }
        public ushort InterruptEnableRegister
        {
            get { return (ushort)((InterruptEnableRegister1 << 8) | InterruptEnableRegister0); }
            set { InterruptEnableRegister0 = (byte)(value & 0x00FF); InterruptEnableRegister1 = (byte)((value & 0xFF00) >> 8); }
        }

        // 0x4000202 / 3
        public byte InterruptRequestFlags0 { get; set; }
        public byte InterruptRequestFlags1 { get; set; }
        public ushort InterruptRequestFlags 
        { 
            get { return (ushort)((InterruptRequestFlags1 << 8) | InterruptRequestFlags0); } 
            set { InterruptRequestFlags0 = (byte) (value & 0x00FF); InterruptRequestFlags1 = (byte) ((value & 0xFF00)>>8); }
        }


        public const UInt32 Interrupt_Vector = 0x18;

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


        bool InterruptEnabled(InterruptType interrupt)
        {
            return (InterruptEnableRegister & (UInt32) interrupt) != 0;
        }


        public bool InterruptPending(InterruptType interrupt)
        {
            return ( InterruptEnabled(interrupt) && ((InterruptRequestFlags & (UInt32) interrupt) != 0));
        }

         
        bool AnyInterruptPending()
        {
            return (InterruptEnableRegister & InterruptRequestFlags) != 0;
        }


        // Jumps to or exits an IRQ / hardware interrupt 
        public void ProcessInterrupts()
        {
            if ((InterruptMasterEnable!=0) && gba.Cpu.IrqDisableFlag == false && AnyInterruptPending())
            {
                //gba.LogMessage(String.Format("Firing Interrupts IE {0:X} IF {1:X}", InterruptEnableRegister, InterruptRequestFlags));

                gba.Cpu.Mode = Cpu.CpuMode.IRQ;

                // SUBS pc, lr, #imm subtracts a value from the link register and loads the PC with the result, then copies the SPSR to the CPSR.
                // When returning from an interrupt, the GBA calls SUBS R15, R14, 0x4 to return where it left off

                // If a Branch instruction has just executed, the PC is changed to point at the next instructin we want to execute. This is done before jumping into the interrupt
                // By adding 4, we negate the 4 passed to subs and therefore point at the 'next' instruction
                if (gba.Cpu.requestFlushPipeline)
                {
                    gba.Cpu.LR = gba.Cpu.PC + 4;
                }
                else
                {
                    // SUBS will use -4 (one instruction) and we just executed the instruction at pc - 8. PC-4 will point us at the 'next' instruction
                    if (gba.Cpu.State == Cpu.CpuState.Arm)
                    {
                        gba.Cpu.LR = gba.Cpu.PC;
                    }
                    else
                    {
                        // SUBS -4 would point us at the instruction we just executed so fudge it to point at the 'next' instruction
                        gba.Cpu.LR = gba.Cpu.PC + 2;
                    }
                }


                // Save the flags before we do anything. The interrupt handler will restore them when it is done
                gba.Cpu.SPSR_Irq = gba.Cpu.CPSR;

                gba.Cpu.SetFlag(Cpu.StatusFlag.IrqDisable);

                gba.Cpu.PC = Interrupt_Vector;
                gba.Cpu.requestFlushPipeline = true;

                gba.Cpu.State = Cpu.CpuState.Arm;

                // It is the games job to clear interurpt flags etc

                // Return is handled by the subs instruction, any data processing instruction with the S flag set and r15 as its destination restores the CPSR
            }
        }


       

    }
}
