using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;

namespace Gba.Core
{
    public class GameboyAdvance
    {
        public Rom Rom { get; private set; }
        public Cpu Cpu { get; private set; }
        public LcdController LcdController { get; private set; }
        public Memory Memory { get; private set; }

        long oneSecondTimer;
        public Stopwatch EmulatorTimer { get; private set; }


        // Renderer hooks
        public Bitmap FrameBuffer { get { return LcdController.FrameBuffer; } }
        public Action OnFrame { get; set; }

        public bool PoweredOn { get; private set; }


        public GameboyAdvance()
        {
            EmulatorTimer = new Stopwatch();
            PoweredOn = false;
        }

        public void PowerOn()
        {
            PoweredOn = true;

            this.Rom = new Rom("../../../../roms/armwrestler.gba");
            this.Memory = new Memory(this);
            this.Cpu = new Cpu(this);
            this.LcdController = new LcdController(this);

            EmulatorTimer.Reset();
            EmulatorTimer.Start();

            Cpu.Reset();
            LcdController.Reset();
        }


        public void Step()
        {
            Cpu.Step();

            // Expensive!
            //if (EmulatorTimer.ElapsedMilliseconds - oneSecondTimer >= 1000)
            //{
            //    oneSecondTimer = EmulatorTimer.ElapsedMilliseconds;
            //}
        }
    }
}
