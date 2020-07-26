using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{

    // Bit   Expl.
    // 0-1   BG Priority (0-3, 0=Highest)
    // 2-3   Character Base Block (0-3, in units of 16 KBytes) (=BG Tile Data)
    // 4-5   Not used(must be zero) 
    // 6     Mosaic(0=Disable, 1=Enable)
    // 7     Colors/Palettes(0=16/16, 1=256/1)
    // 8-12  Screen Base Block(0-31, in units of 2 KBytes) (=BG Map Data)
    // 13    BG0/BG1: Not used(except in NDS mode: Ext Palette Slot for BG0/BG1)
    // 13    BG2/BG3: Display Area Overflow(0=Transparent, 1=Wraparound)
    // 14-15 Screen Size(0-3)

    public class BgControlRegister
    {
        // 4000008h - BG0CNT - BG0 Control(R/W) (BG Modes 0,1 only)
        // 400000Ah - BG1CNT - BG1 Control(R/W) (BG Modes 0,1 only)
        // 400000Ch - BG2CNT - BG2 Control(R/W) (BG Modes 0,1,2 only)
        // 400000Eh - BG3CNT - BG3 Control(R/W) (BG Modes 0,2 only)

        MemoryRegister16 register;

        /*
        byte reg0;
        public byte Register0
        {
            get
            { 
                return reg0;
            }
            set
            {
                reg0 = value;
                lcd.Bg[bgNumber].CacheRenderData();
            }
        }



        byte reg1;
        public byte Register1
        {
            get
            {
                return reg1;
            }
            set
            {
                UInt32 oldScreenBaseBlock = ScreenBlockBaseAddress;
                
                reg1 = value;

                if(ScreenBlockBaseAddress != oldScreenBaseBlock)
                {
                    lcd.Bg[bgNumber].TileMap.Reset();
                }
                lcd.Bg[bgNumber].CacheRenderData();
            }
        }
        */
      

        LcdController lcd;
        int bgNumber;

        public BgControlRegister(GameboyAdvance gba, LcdController lcd, int bgNumber, UInt32 address)
        {
            this.lcd = lcd;
            this.bgNumber = bgNumber;

            MemoryRegister8WithSetHook r0 = new MemoryRegister8WithSetHook(gba.Memory, address, true, true);
            MemoryRegister8WithSetHook r1 = new MemoryRegister8WithSetHook(gba.Memory, address + 1, true, true);
            register = new MemoryRegister16(gba.Memory, address, true, true, r0, r1);

            r0.OnSet = (oldValue, newValue) =>
            {                 
                lcd.Bg[bgNumber].CacheRenderData();
            };

            r1.OnSet = (oldValue, newValue) =>
            {
                // This *should* be 0x1F but it breaks bg on mario kart track 1. Hopefully we can just get rid of tilemap reset and be done with this
                //if ((oldValue & 0x1F) != (newValue & 0x1F))
                {
                    // THIS HAS GOT TO GO! Mario Kart canes it!
                    lcd.Bg[bgNumber].TileMap.Reset();
                }

                lcd.Bg[bgNumber].CacheRenderData();
            };
        }

        public int Priority { get { return register.LowByte.Value & 0x03; } }

        public UInt32 TileBlockBaseAddress { get { return (UInt32)((register.LowByte.Value & 0x0C) >> 2); } }
        public UInt32 ScreenBlockBaseAddress { get { return (UInt32)(register.HighByte.Value & 0x1F); } }

        public BgPaletteMode PaletteMode { get { return (BgPaletteMode)((register.LowByte.Value & 0x80) >> 7); } }

        // Does affine BG wrap?
        public bool DisplayAreaOverflow { get { return (register.HighByte.Value & 0x20) != 0;  } }

        public BgSize Size 
        { 
            get 
            {
                int regValue = (register.HighByte.Value & 0xC0) >> 6;
                if (lcd.Bg[bgNumber].AffineMode)
                {
                    regValue += 4;
                }
                return (BgSize)(regValue); 
            } 
        }
        
    }


    public enum BgSize
    {
        Bg256x256 = 0,
        Bg512x256,
        Bg256x512,
        Bg512x512,

        AffineBg128x128,
        AffineBg256x256,
        AffineBg512x512,
        AffineBg1024x1024,
    }



    public enum BgPaletteMode
    {
        PaletteMode16x16 = 0,
        PaletteMode256x1
    }

}
