using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Gba.Core;

namespace GbaDebugger
{
    public class IrqBreakpoint : IBreakpoint
    {
        //UInt32 previousPc;
        Interrupts.InterruptType interrupt;
        bool breakOnIrq;
        bool breakOnIrqReturn;
        UInt32 returnAddress;
        
        int scanline;

        GameboyAdvance gba;

        public IrqBreakpoint(GameboyAdvance gba, Interrupts.InterruptType interrupt, bool breakOnIrq, bool breakOnIrqReturn, int scanline = -1)
        {
            this.gba = gba;
            this.interrupt = interrupt;
            //previousPc = 0;
            this.breakOnIrq = breakOnIrq;
            this.breakOnIrqReturn = breakOnIrqReturn;
            returnAddress = 0;
            this.scanline = scanline;
        }



        // Condition == ROM - prvious != 0x8XXXX current == 0x08xxx
        //Condition == hblanIRQ
        // print irq flags when break
     
        public bool ShouldBreak(UInt32 pc)
        {
            if(scanline != -1 && gba.LcdController.CurrentScanline != scanline)
            {
                return false;
            }

            if(pc == Interrupts.Interrupt_Vector &&
               gba.Interrupts.InterruptPending(interrupt))
            {
                // As we enter the IRQ, grab the return address
                returnAddress = gba.Cpu.LR;

                if (breakOnIrq)
                {
                    return true;
                }
            }


            if(breakOnIrqReturn &&
                returnAddress != 0 &&
                pc == returnAddress)
            {
                returnAddress = 0;
                return true;
            }
                
            // Store the pc here and you know it's the last instruction then you can check against the next one to see if we went BIOS -> ROM or ROM -> BIOS ....            
            //previousPc = pc;

            return false;
        }



        public override string ToString()
        {
            return String.Format("Breakpoint {0}", interrupt.ToString());
        }
    }
}
