using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    public class Bios
    {
        public enum Status
        {
            STARTUP,
            IRQ_EXECUTE,
            IRQ_FINISH,
            SWI_FINISH
        };

        public Status State { get; set; }

        GameboyAdvance gba;

        public Bios(GameboyAdvance gba)
        {
            this.gba = gba;
            State = Status.STARTUP;
        }
    }
}
