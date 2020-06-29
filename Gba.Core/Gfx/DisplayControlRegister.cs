using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{

    // Bit   Expl.
    // 0-2   BG Mode                (0-5 =Video Mode, 6-7=Prohibited)
    // 3     Reserved / CGB Mode    (0=GBA, 1=CGB; can be set only by BIOS opcodes)
    // 4     Display Frame Select   (0-1=Frame 0-1) (for BG Modes 4,5 only)
    // 5     H-Blank Interval Free  (1=Allow access to OAM during H-Blank)
    // 6     OBJ Character VRAM Mapping (0=Two dimensional, 1=One dimensional)
    // 7     Forced Blank           (1=Allow FAST access to VRAM,Palette,OAM)
    // 8     Screen Display BG0  (0=Off, 1=On)
    // 9     Screen Display BG1  (0=Off, 1=On)
    // 10    Screen Display BG2  (0=Off, 1=On)
    // 11    Screen Display BG3  (0=Off, 1=On)
    // 12    Screen Display OBJ  (0=Off, 1=On)
    // 13    Window 0 Display Flag   (0=Off, 1=On)
    // 14    Window 1 Display Flag   (0=Off, 1=On)
    // 15    OBJ Window Display Flag (0=Off, 1=On)

    public class DisplayControlRegister
    {
        // 0x4000000
        // 0x4000001 

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
     
        public DisplayControlRegister(LcdController lcd)
        {
            this.lcd = lcd;
        }

        public UInt32 BgMode { get { return (UInt32)(Register0 & 0x07); } }
        public UInt32 DisplayFrameSelect { get { return (UInt32)((Register0 & 0x10) >> 4); } }
        public bool HBlankIntervalFree { get { return ((Register0 & 0x20)!= 0); } }
        public UInt32 ObjectCharacterVramMapping { get { return (UInt32)((Register0 & 0x40) >> 6); } }
        public bool ForcedBlank { get { return ((Register0 & 0x80) != 0); } }

        public bool DisplayBg0 { get { return ((Register1 & 0x01) != 0); } }
        public bool DisplayBg1 { get { return ((Register1 & 0x02) != 0); } }
        public bool DisplayBg2 { get { return ((Register1 & 0x04) != 0); } }
        public bool DisplayBg3 { get { return ((Register1 & 0x08) != 0); } }
        public bool DisplayObj { get { return ((Register1 & 0x10) != 0); } }
        public bool DisplayWin0 { get { return ((Register1 & 0x20) != 0); } }
        public bool DisplayWin1 { get { return ((Register1 & 0x40) != 0); } }
        public bool DisplayObjWin { get { return ((Register1 & 0x80) != 0); } }

        public bool BgVisible(int i)
        {
            return ((Register1 & (1 << i)) != 0);
        }
    }


}
