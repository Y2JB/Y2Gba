using Gba.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace GbaDebugger
{
    public class Breakpoint : IBreakpoint
    {
        public UInt32 Address { get; set; }
        

        public Breakpoint(UInt32 address)
        {
            Address = address;
        }


        public bool ShouldBreak(UInt32 pc)
        {
            if(pc == Address)
            {
                return true;
            }
            return false;
        }


        public override string ToString()
        {
            return String.Format("Breakpoint {0:X4}", Address);
        }
    }
}
