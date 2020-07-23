using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{

    public class PixelCoefficientRegister
    {
        // 0x4000050 & 0x4000051 

        public byte Register0 { get; set; }
        public byte Register1 { get; set; }
        
        LcdController lcd;
     
        public PixelCoefficientRegister(LcdController lcd)
        {
            this.lcd = lcd;
        }

    }


}
