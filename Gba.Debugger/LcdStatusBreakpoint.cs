using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Gba.Core;

namespace GbaDebugger
{
    public class LcdStatusBreakpoint : IBreakpoint
    {
        //UInt32 previousPc;
        UInt32 cyclesOnFirstCheck;
    
        GameboyAdvance gba;

        public enum BreakOn
        {
            HBlank,
            VBlank,
            Frame
        }

        BreakOn breakOn;

        public LcdStatusBreakpoint(GameboyAdvance gba, BreakOn breakOn)
        {
            this.gba = gba;
            this.breakOn = breakOn;
            cyclesOnFirstCheck = gba.Cpu.Cycles;
        }
     
        public bool ShouldBreak(UInt32 pc)
        {
            UInt32 cyclesSinceFirstCheck = gba.Cpu.Cycles - cyclesOnFirstCheck;

            switch (breakOn)
            {
                case BreakOn.HBlank:
                    // The cycle check ensures we don't break multiple times during one period
                    if(gba.LcdController.Mode == LcdController.LcdMode.HBlank && cyclesSinceFirstCheck > LcdController.HBlank_Length)
                    {
                        return true;
                    }
                    break;

                case BreakOn.VBlank:
                    if (gba.LcdController.Mode == LcdController.LcdMode.VBlank && cyclesSinceFirstCheck > LcdController.VBlank_Length)
                    {
                        return true;
                    }
                    break;

                case BreakOn.Frame:
                    if (gba.LcdController.Mode == LcdController.LcdMode.ScanlineRendering && gba.LcdController.CurrentScanline == 0 && cyclesSinceFirstCheck > LcdController.HDraw_Length)
                    {
                        return true;
                    }
                    break;
            }
            return false;
        }



        public override string ToString()
        {
            return String.Format("Breakpoint {0}", breakOn.ToString());
        }
    }
}
