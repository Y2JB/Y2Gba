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
        MemoryRegister16 register;

        // 0x4000005
        public byte VCountSetting { get { return register.HighByte.Value; } }

        public byte Register { get { return register.LowByte.Value; } }


        LcdController lcd;


        class DispStatRegister : MemoryRegister8
        {
            LcdController lcd;
            DisplayStatusRegister dspStat;

            public DispStatRegister(LcdController lcd, DisplayStatusRegister dspStat, Memory memory, UInt32 address) :
                base(memory, address, true, true)
            {
                this.lcd = lcd;
                this.dspStat = dspStat;
            }
            public override byte Value
            {
                get
                {
                    // Dynamically set the first 3 flag bits 
                    if (lcd.Mode == LcdController.LcdMode.VBlank) reg |= 0x01;
                    else reg &= 0xFE;

                    if (lcd.Mode == LcdController.LcdMode.HBlank || lcd.HblankInVblank)
                    {
                        reg |= 0x02;
                    }
                    else reg &= 0xFD;

                    if (dspStat.VCountSetting == lcd.CurrentScanline) reg |= 0x04;
                    else reg &= 0xFB;

                    return reg;
                }

                set
                {
                    base.Value = value;
                }
            }
        }


        public DisplayStatusRegister(GameboyAdvance gba, LcdController lcd)
        {
            this.lcd = lcd;
            DispStatRegister r0 = new DispStatRegister(lcd, this, gba.Memory, 0x4000004);
            MemoryRegister8 r1 = new MemoryRegister8(gba.Memory, 0x4000005, true, true);
            register = new MemoryRegister16(gba.Memory, 0x4000004, true, true, r0, r1);
        }


        public bool VBlankIrqEnabled { get { return (Register & 0x08) != 0; } }
        public bool HBlankIrqEnabled { get { return (Register & 0x10) != 0; } }
        public bool VCounterIrqEnabled { get { return (Register & 0x20) != 0; } }
    }

}
