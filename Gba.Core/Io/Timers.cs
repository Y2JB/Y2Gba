using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    public class Timers
    {
        GameboyAdvance gba;

        // Gba supports 4 timers which elapse depending on the frequency which can be set to one of the following 4 values
        // By setting up timers with various frequencies and also by having timers cascade, you can create any timer frequency
        readonly int[] TimerPeriods = { 1, 64, 256, 1024 };

        public GbaTimer[] Timer { get; private set; }
        UInt32 lastUpdatedOnCycle;
        UInt32 elapsedCycles;


        public Timers(GameboyAdvance gba)
        {
            this.gba = gba;

            this.Timer = new GbaTimer[4];
            for (int i = 0; i < 4; i++)
            {
                Timer[i] = new GbaTimer(this, gba.Interrupts, i);
            }
        }


        public void Update()
        {
            for(int cycles = 0; cycles < (gba.Cpu.Cycles - lastUpdatedOnCycle); cycles++)
            {
                elapsedCycles++;

                for (int i = 0; i < 4; i++)
                {
                    if (Timer[i].Enabled)
                    {
                        if (Timer[i].CascadeMode == false &&
                            (elapsedCycles % TimerPeriods[Timer[i].Freq]) == 0)
                        {
                            Timer[i].Increment();
                        }
                    }
                }
            }
            lastUpdatedOnCycle = gba.Cpu.Cycles;
        }
    }


    public class GbaTimer
    {
        Timers timers;
        Interrupts interrupts;
        int timerNumber;

        public GbaTimer(Timers timers, Interrupts interrupts, int timerNumber)
        {
            this.timers = timers;
            this.interrupts = interrupts;
            this.timerNumber = timerNumber;
        }


        // REG_TMxCNT
        // Bit Expl.
        // 0-1   Prescaler Selection (0=F/1, 1=F/64, 2=F/256, 3=F/1024)
        // 2     Count-up Timing(0=Normal, 1=Cascade)
        // 3-5   Not used
        // 6     Timer IRQ Enable(0=Disable, 1=IRQ on Timer overflow)
        // 7     Timer Start/Stop(0=Stop, 1=Operate)
        // 8-15  Not used
        byte timerCnt;
        public byte TimerControlRegister 
        { 
            get { return timerCnt; } 
            set
            {
                // When a timer is enabled, it reloads it's starting value. When it is disabled it just maintains its current values
                bool wasEnabled = Enabled;
                timerCnt = value;
                if(wasEnabled == false && Enabled)
                {
                    TimerValue = ReloadValue;
                }

                if(IrqEnable)
                {
                    // TODO: We need to schedule when this will happen
                    throw new NotImplementedException();
                }
            }
        }

        public int Freq { get { return TimerControlRegister & 0x03; } }
        public bool CascadeMode { get { return ((TimerControlRegister & 0x04) != 0); } }
        public bool IrqEnable { get { return ((TimerControlRegister & 0x40) != 0); } }
        public bool Enabled { get { return ((TimerControlRegister & 0x80) != 0); } }


        // REG_TMxD - Read        
        byte timerValue0, timerValue1;
        public byte TimerValue0 { get { return timerValue0; } set { timerValue0 = value; } }
        public byte TimerValue1 { get { return timerValue1; } set { timerValue1 = value; } }
        public ushort TimerValue
        {            
            get { return (ushort)((TimerValue1 << 8) | TimerValue0); }
            set { TimerValue0 = (byte)(value & 0x00FF); TimerValue1 = (byte)((value & 0xFF00) >> 8); }
        }


        // REG_TMxD - Write
        public byte ReloadValue0 { get; set; }
        public byte ReloadValue1 { get; set; }
        public ushort ReloadValue
        {
            get { return (ushort)((ReloadValue1 << 8) | ReloadValue0); }
            set { ReloadValue0 = (byte)(value & 0x00FF); ReloadValue1 = (byte)((value & 0xFF00) >> 8); }
        }

        public void Increment()
        {
            if (TimerValue < 0xFFFF)
            {
                TimerValue++;
            }
            else
            {
                if (IrqEnable)
                {
                    Interrupts.InterruptType interrupt = (Interrupts.InterruptType) ((int)(Interrupts.InterruptType.Timer0Overflow) + timerNumber);
                    interrupts.RequestInterrupt(interrupt);
                }

                TimerValue = ReloadValue;

                // Deal with cascading timers (recursive) 
                if (timerNumber != 3 &&
                    timers.Timer[timerNumber + 1].CascadeMode &&
                    timers.Timer[timerNumber + 1].Enabled)
                {
                    timers.Timer[timerNumber + 1].Increment();
                }
            }
        }

    }
}
