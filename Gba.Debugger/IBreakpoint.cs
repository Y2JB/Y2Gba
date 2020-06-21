using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Gba.Core;

namespace GbaDebugger
{
    public interface IBreakpoint
    {
        public bool ShouldBreak(UInt32 pc);
    }
}
