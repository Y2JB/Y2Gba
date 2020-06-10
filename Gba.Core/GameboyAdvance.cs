using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;

namespace Gba.Core
{
    public class GameboyAdvance
    {
        public Bios Bios { get; private set; }
        public Rom Rom { get; private set; }
        public Cpu Cpu { get; private set; }
        public Interrupts Interrupts { get; private set; }
        public LcdController LcdController { get; private set; }
        public Memory Memory { get; private set; }
        public Joypad Joypad { get; private set; }
        
        //long oneSecondTimer;
        public Stopwatch EmulatorTimer { get; private set; }

        // Renderer hooks
        public DirectBitmap FrameBuffer { get { return LcdController.FrameBuffer; } }
        public Action OnFrame { get; set; }

        // TODO: TTY output than can be passed on to the Emu container
        //public List<string> Tty { get; private set; }

        public bool PoweredOn { get; private set; }


        public GameboyAdvance()
        {
            EmulatorTimer = new Stopwatch();
            PoweredOn = false;
        }


        public void PowerOn()
        {
            PoweredOn = true;

            this.Bios = new Bios(this, "../../../../GBA.BIOS");
            //this.Rom = new Rom("../../../../roms/armwrestler.gba");
            //this.Rom = new Rom("../../../../roms/suite.gba");
            //this.Rom = new Rom("../../../../roms/arm.gba");
            //this.Rom = new Rom("../../../../roms/hello.gba");
            this.Rom = new Rom("../../../../roms/irq_demo.gba");
            //this.Rom = new Rom("../../../../roms/NCE-heart.gba");
            //this.Rom = new Rom("../../../../roms/brin_demo.gba");
            //this.Rom = new Rom("../../../../roms/Super Dodgeball Advance.gba");
            //this.Rom = new Rom("../../../../roms/Kirby.gba");

            this.Memory = new Memory(this);
            this.Cpu = new Cpu(this);
            this.Interrupts = new Interrupts(this);
            this.LcdController = new LcdController(this);
            this.Joypad = new Joypad(this);
            
            
            EmulatorTimer.Reset();
            EmulatorTimer.Start();

            Cpu.Reset();
            LcdController.Reset();
            Joypad.Reset();
        }


        public void Step()
        {
            // We lockstep everythng off of the CPU so we only update the cpu at this level
            Cpu.Step();

            // Expensive!
            //if (EmulatorTimer.ElapsedMilliseconds - oneSecondTimer >= 1000)
            //{
            //    oneSecondTimer = EmulatorTimer.ElapsedMilliseconds;
            //}
        }
    }
}
