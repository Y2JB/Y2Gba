using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    public class Interrupts
    {
        // Note that there is another 'master enable flag' directly in the CPUs Status Register(CPSR) accessible in privileged modes, see CPU reference for details.

        // 0=Disable All, 1=See IE register
        // 0x4000208 
        public byte InterruptMasterEnable { get; set; }

        // 0x4000202
        public ushort InterruptRequestFlags { get; set; }

        // 0x4000200
        public ushort InterruptEnableRegister { get; set; }

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

        // Jumps to or exits an IRQ / hardware interrupt 
        public void ProcessInterrupts()
        {

        }


       

    }
}
