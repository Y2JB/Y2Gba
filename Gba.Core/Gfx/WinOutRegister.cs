using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    public class WinOutRegister
    {
        // Control of Outside of Windows & Inside of OBJ Window(R/W)
        // Bit Expl.
        // 0-3   Outside BG0-BG3 Enable Bits      (0=No Display, 1=Display)
        // 4     Outside OBJ Enable Bit(0=No Display, 1=Display)
        // 5     Outside Color Special Effect(0=Disable, 1=Enable)
        // 6-7   Not used
        // 8-11  OBJ Window BG0-BG3 Enable Bits(0=No Display, 1=Display)
        // 12    OBJ Window OBJ Enable Bit(0=No Display, 1=Display)
        // 13    OBJ Window Color Special Effect(0=Disable, 1=Enable)
        // 14-15 Not used
        public byte WinOutL { get; set; }
        public byte WinOutH { get; set; }
    }
}
