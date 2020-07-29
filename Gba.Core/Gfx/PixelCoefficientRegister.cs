using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{

    public class PixelCoefficientRegister
    {
        // 0x4000050 & 0x4000051 

        MemoryRegister16 register;
        
        LcdController lcd;
     
        public PixelCoefficientRegister(LcdController lcd, GameboyAdvance gba, UInt32 address)
        {
            this.lcd = lcd;
            register = new MemoryRegister16(gba.Memory, address, true, true);
        }

    }


}
