using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    public class Window
    {
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
        public byte WinIn { get; set; }

        GameboyAdvance gba;

        public Window(GameboyAdvance gba)
        {
            this.gba = gba;
        }
    }
}
