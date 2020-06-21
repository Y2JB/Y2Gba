using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    // 0     V-Blank flag(Read only) (1=VBlank) (set in line 160..226; not 227)
    // 1     H-Blank flag(Read only) (1=HBlank) (toggled in all lines, 0..227)
    // 2     V-Counter flag(Read only) (1=Match)  (set in selected line)    (R)
    // 3     V-Blank IRQ Enable(1=Enable)                                   (R/W)
    // 4     H-Blank IRQ Enable(1=Enable)                                   (R/W)
    // 5     V-Counter IRQ Enable(1=Enable)                                 (R/W)
    // 6     Not used(0) / DSi: LCD Initialization Ready(0=Busy, 1=Ready)   (R)
    // 7     Not used(0) / NDS: MSB of V-Vcount Setting(LYC.Bit8) (0..262)  (R/W)
    // 8-15  V-Count Setting(LYC)      (0..227)                             (R/W)
    public class DisplayStatusRegister
    {
        // 0x4000005
        public byte VCountSetting { get; set; }


        // 0x4000004
        byte reg0;
        public byte Register0
        {
            get
            {
                // Dynamically set the first 3 flag bits 
                if (lcd.Mode == LcdController.LcdMode.VBlank) reg0 |= 0x01;
                else reg0 &= 0xFE;

                if (lcd.Mode == LcdController.LcdMode.HBlank || lcd.HblankInVblank)
                {
                    reg0 |= 0x02;
                }
                else reg0 &= 0xFD;

                if (VCountSetting == lcd.CurrentScanline) reg0 |= 0x04;
                else reg0 &= 0xFB;

                return reg0;
            }
            set
            {
                reg0 = value;
            }
        }
        
        LcdController lcd;
        
        
        public DisplayStatusRegister(LcdController lcd)
        {
            this.lcd = lcd;
        }

        public bool VBlankIrqEnabled { get { return (Register0 & 0x08) != 0; } }
        public bool HBlankIrqEnabled { get { return (Register0 & 0x10) != 0; } }
        public bool VCounterIrqEnabled { get { return (Register0 & 0x20) != 0; } }
    }

}
