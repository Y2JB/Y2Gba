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
        public MemoryRegister8 Left { get; set; }
        public MemoryRegister8 Right { get; set; }
        public MemoryRegister8 Top { get; set; }
        public MemoryRegister8 Bottom { get; set; }

        // Control of Inside of Window(s) (R/W)
        // Bit   Expl.
        // 0-3   BG0-BG3 Enable Bits     (0=No Display, 1=Display)
        // 4     OBJ Enable Bit          (0=No Display, 1=Display)
        // 5     Color Special Effect    (0=Disable, 1=Enable)
        // 6-7   Not used
        public MemoryRegister8 Register { get; set; }

        GameboyAdvance gba;

        public Window(GameboyAdvance gba, UInt32 rightRegisterAddress, UInt32 registerAddress)
        {
            this.gba = gba;

            if (rightRegisterAddress != 0)
            {
                Right = new MemoryRegister8(gba.Memory, rightRegisterAddress, false, true);
                Left = new MemoryRegister8(gba.Memory, rightRegisterAddress + 1, false, true);
                Bottom = new MemoryRegister8(gba.Memory, rightRegisterAddress + 4, false, true);
                Top = new MemoryRegister8(gba.Memory, rightRegisterAddress + 5, false, true);
            }

            Register = new MemoryRegister8(gba.Memory, registerAddress, true, true);
        }

        public int DisplayBg0 { get { return (Register.Value & 0x01); } }
        public int DisplayBg1 { get { return (Register.Value & 0x02); } }
        public int DisplayBg2 { get { return (Register.Value & 0x04); } }
        public int DisplayBg3 { get { return (Register.Value & 0x08); } }

        public bool DisplayObjs { get { return ((Register.Value & 0x10) != 0); } }

        public bool BlendingEnabled { get { return ((Register.Value & 0x20) != 0); } }

        public bool DisplayBgInWindow(int i)
        {
            return ((Register.Value & (1 << i)) != 0);
        }

        public int RightAdjusted()
        {
            // Garbage values of X2>240 or X1>X2 are interpreted as X2=240
            if (Right.Value > 240) return 240;
            if (Left.Value > Right.Value) return 240;
            return Right.Value;
        }

        public int BottomAdjusted()
        {
            // Garbage values of Y2>160 or Y1>Y2 are interpreted as Y2=160.
            if (Bottom.Value > 160) return 160;
            if (Top.Value > Bottom.Value) return 160;
            return Bottom.Value;
        }
    }
}
