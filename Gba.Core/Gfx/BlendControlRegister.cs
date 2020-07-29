using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    // Bit Expl.
    // 0     BG0 1st Target Pixel (Background 0)
    // 1     BG1 1st Target Pixel(Background 1)
    // 2     BG2 1st Target Pixel(Background 2)
    // 3     BG3 1st Target Pixel(Background 3)
    // 4     OBJ 1st Target Pixel(Top-most OBJ pixel)
    // 5     BD  1st Target Pixel(Backdrop)
    // 6-7   Color Special Effect(0-3, see below)
    //        0 = None(Special effects disabled)
    //        1 = Alpha Blending(1st+2nd Target mixed)
    //        2 = Brightness Increase(1st Target becomes whiter)
    //        3 = Brightness Decrease(1st Target becomes blacker)
    // 8     BG0 2nd Target Pixel(Background 0)
    // 9     BG1 2nd Target Pixel(Background 1)
    // 10    BG2 2nd Target Pixel(Background 2)
    // 11    BG3 2nd Target Pixel(Background 3)
    // 12    OBJ 2nd Target Pixel(Top-most OBJ pixel)
    // 13    BD  2nd Target Pixel(Backdrop)
    // 14-15 Not used
    public class BlendControlRegister
    {
        // 0x4000050 & 0x4000051 
        MemoryRegister16 register;
        
        LcdController lcd;
     
        public BlendControlRegister(LcdController lcd, GameboyAdvance gba)
        {
            this.lcd = lcd;
            register = new MemoryRegister16(gba.Memory, 0x4000050, true, true);
        }

        public enum SepcialEffect
        {
            None = 0,
            AlphaBlending,
            BrignhtessIncrease,
            BrightnessDecrease
        }

        public bool Bg01stTargetPixel { get { return ((register.LowByte.Value & 0x01) != 0); } }
        public bool Bg11stTargetPixel { get { return ((register.LowByte.Value & 0x02) != 0); } }
        public bool Bg21stTargetPixel { get { return ((register.LowByte.Value & 0x04) != 0); } }
        public bool Bg31stTargetPixel { get { return ((register.LowByte.Value & 0x08) != 0); } }
        public bool Obj1stTargetPixel { get { return ((register.LowByte.Value & 0x10) != 0); } }
        public bool Backdrop1stTargetPixel { get { return ((register.LowByte.Value & 0x20) != 0); } }

        public SepcialEffect Effect { get { return (SepcialEffect)((register.LowByte.Value & 0xC0) >> 6); } }

        public bool Bg02ndTargetPixel { get { return ((register.HighByte.Value & 0x01) != 0); } }
        public bool Bg12ndTargetPixel { get { return ((register.HighByte.Value & 0x02) != 0); } }
        public bool Bg22ndTargetPixel { get { return ((register.HighByte.Value & 0x04) != 0); } }
        public bool Bg32ndTargetPixel { get { return ((register.HighByte.Value & 0x08) != 0); } }
        public bool Obj2ndTargetPixel { get { return ((register.HighByte.Value & 0x10) != 0); } }
        public bool Backdrop2ndTargetPixel { get { return ((register.HighByte.Value & 0x20) != 0); } }

    }


}
