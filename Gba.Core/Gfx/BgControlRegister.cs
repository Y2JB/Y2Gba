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
                reg1 = value;
            }
        }
      

        LcdController lcd;
     
        public BgControlRegister(LcdController lcd)
        {
            this.lcd = lcd;
        }

        public int Priority { get { return Register0 & 0x03; } }

        public UInt32 TileBlockBaseAddress { get { return (UInt32)((Register0 & 0x0C) >> 2); } }
        public UInt32 ScreenBlockBaseAddress { get { return (UInt32)(Register1 & 0x1F); } }

        public BgPaletteMode PaletteMode { get { return (BgPaletteMode)((Register0 & 0x80) >> 7); } }

        public BgSize Size { get { return (BgSize)((Register1 & 0xC0) >> 6); } }
        
    }


    public enum BgSize
    {
        Bg256x256 = 0,
        Bg512x256,
        Bg256x512,
        Bg512x512,
    }

    public enum BgPaletteMode
    {
        PaletteMode16x16 = 0,
        PaletteMode256x1
    }

}
