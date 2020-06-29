using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    public class Window
    {
        public enum WindowName
        { 
            Window0,
            Window1,
            WindowOut,
            WindowObj
        }

        // Right and bottom are + 1
        public byte Left { get; set; }
        public byte Right { get; set; }
        public byte Top { get; set; }
        public byte Bottom { get; set; }

        // Control of Inside of Window(s) (R/W)
        // Bit   Expl.
        // 0-3   BG0-BG3 Enable Bits     (0=No Display, 1=Display)
        // 4     OBJ Enable Bit          (0=No Display, 1=Display)
        // 5     Color Special Effect    (0=Disable, 1=Enable)
        // 6-7   Not used
        public byte Register { get; set; }

        GameboyAdvance gba;

        public Window(GameboyAdvance gba)
        {
            this.gba = gba;
        }

        public int DisplayBg0 { get { return (Register & 0x01); } }
        public int DisplayBg1 { get { return (Register & 0x02); } }
        public int DisplayBg2 { get { return (Register & 0x04); } }
        public int DisplayBg3 { get { return (Register & 0x08); } }

        public bool DisplayObjs { get { return ((Register & 0x10) != 0); } }

        public bool BlendingEnabled { get { return ((Register & 0x20) != 0); } }

        public bool DisplayBgInWindow(int i)
        {
            return ((Register & (1 << i)) != 0);
        }

        public int RightAdjusted()
        {
            // Garbage values of X2>240 or X1>X2 are interpreted as X2=240
            if (Right > 240) return 240;
            if (Left > Right) return 240;
            return Right;
        }

        public int BottomAdjusted()
        {
            // Garbage values of Y2>160 or Y1>Y2 are interpreted as Y2=160.
            if (Bottom > 160) return 160;
            if (Top > Bottom) return 160;
            return Bottom;
        }
    }
}
