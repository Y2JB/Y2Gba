using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    public class GameboyAdvance
    {
        public Rom Rom { get; private set; }
        public Cpu Cpu { get; private set; }
        public Memory Memory { get; private set; }


        public void PowerOn()
        {
            this.Rom = new Rom("../../../../roms/armwrestler.gba");

            this.Cpu = new Cpu(this);
            this.Memory = new Memory(this);

            Cpu.Reset();
        }


        public void Step()
        {
            Cpu.Step();
        }
    }
}
