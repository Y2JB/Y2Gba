using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Gba.Core
{
    public class Timers
    {
        GameboyAdvance gba;

        // Gba supports 4 timers which elapse depending on the frequency which can be set to one of the following 4 values
        // By setting up timers with various frequencies and also by having timers cascade, you can create any timer frequency
        public readonly int[] TimerPeriods = { 1, 64, 256, 1024 };

        public GbaTimer[] Timer { get; private set; }
        public UInt32 NextUpdateCycle { get; set; }


        public Timers(GameboyAdvance gba)
        {
            this.gba = gba;

            this.Timer = new GbaTimer[4];
            for (int i = 0; i < 4; i++)
            {
                Timer[i] = new GbaTimer(this, gba, i);
            }

            NextUpdateCycle = 0xFFFFFFFF;
        }


        public void CalculateWhenToNextUpdate()
        {
            UInt32 nextUpdate = 0xFFFFFFFF;
            for (int i = 0; i < 4; i++)
            {
                if (Timer[i].Enabled &&
                    Timer[i].CascadeMode == false && 
                    Timer[i].FiresOnCycle < nextUpdate)
                {
                    nextUpdate = Timer[i].FiresOnCycle;
                }
            }
            NextUpdateCycle = nextUpdate;
        }


        public void Update()
        {
            for (int i = 0; i < 4; i++)
            {
                if (Timer[i].Enabled && Timer[i].CascadeMode == false)
                {
                    Timer[i].AddCycles();                        
                }
            }         
        }
    }


    // This register doesn't actually hold any data, it just updates the timers and returns the real timer data 
    public class TimerValueRegister : MemoryRegister8
    {
        GameboyAdvance gba;
        GbaTimer timer;
        UInt32 address;

        public TimerValueRegister(GameboyAdvance gba, GbaTimer timer, Memory memory, UInt32 address) :
        base(memory, address, true, false)
        {
            this.gba = gba;
            this.timer = timer;
            this.address = address;
        }

        public override byte Value
        {
            get
            {
                gba.Timers.Update();                
                if(((address & 1) == 0)) return (byte) (timer.TimerValue & 0x00FF);
                return (byte)((timer.TimerValue & 0xFF00) >> 8);
            }

            set
            {
            }
        }
    }


    public class GbaTimer
    {
        Timers timers;
        GameboyAdvance gba;
        int timerNumber;
        //UInt32 elapsedCycles;
        UInt32 startCycle;

        public UInt32 FiresOnCycle { get; private set; }

        public GbaTimer(Timers timers, GameboyAdvance gba, int timerNumber)
        {
            this.timers = timers;
            this.gba = gba;
            this.timerNumber = timerNumber;            

            FiresOnCycle = 0xFFFFFFFF;

            UInt32 baseAddr = (UInt32)(0x4000100 + (timerNumber * 4));

            TimerValueRegister r0 = new TimerValueRegister(gba, this, gba.Memory, baseAddr);
            TimerValueRegister r1 = new TimerValueRegister(gba, this, gba.Memory, baseAddr + 1);
            TimerValueRegister = new MemoryRegister16(gba.Memory, baseAddr, true, false, r0, r1);

            ReloadValue = new MemoryRegister16(gba.Memory, baseAddr, false, true);

            baseAddr = (UInt32)(0x4000102 + (timerNumber * 4));
            MemoryRegister8WithSetHook reg = new MemoryRegister8WithSetHook(gba.Memory, baseAddr, true, true);
            MemoryRegister8 unused = new MemoryRegister8(gba.Memory, baseAddr + 1, true, true);
            TimerControlRegister = new MemoryRegister16(gba.Memory, baseAddr, true, true, reg, unused);
            reg.OnSet = (oldValue, newValue) =>
            {
                CalculateWhichCycleTheTimerWillFire();
                timers.CalculateWhenToNextUpdate();

                // When a timer is enabled, it reloads it's starting value. When it is disabled it just maintains its current values
                bool wasEnabled = ((oldValue & 0x80) != 0);
                bool enabled = ((newValue & 0x80) != 0);
                if (wasEnabled == false && enabled)
                {
                    startCycle = gba.Cpu.Cycles;

                    timers.Update();
                    TimerValue = ReloadValue.Value;                
                }
            };
        }


        // REG_TMxCNT
        // Bit Expl.
        // 0-1   Prescaler Selection (0=F/1, 1=F/64, 2=F/256, 3=F/1024)
        // 2     Count-up Timing(0=Normal, 1=Cascade)
        // 3-5   Not used
        // 6     Timer IRQ Enable(0=Disable, 1=IRQ on Timer overflow)
        // 7     Timer Start/Stop(0=Stop, 1=Operate)
        // 8-15  Not used
        MemoryRegister16 TimerControlRegister;

        public int Freq { get { return TimerControlRegister.Value & 0x03; } }
        public bool CascadeMode { get { return ((TimerControlRegister.Value & 0x04) != 0); } }
        public bool IrqEnable { get { return ((TimerControlRegister.Value & 0x40) != 0); } }
        public bool Enabled { get { return ((TimerControlRegister.Value & 0x80) != 0); } }


        // REG_TMxD - Read 
        MemoryRegister16 TimerValueRegister { get; set; }
        public ushort TimerValue { get; set; }
        MemoryRegister16 ReloadValue { get; set; }

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
                    gba.Interrupts.RequestInterrupt(interrupt);
                }

                TimerValue = ReloadValue.Value;

                // Deal with cascading timers (recursive) 
                if (timerNumber != 3 &&
                    timers.Timer[timerNumber + 1].CascadeMode &&
                    timers.Timer[timerNumber + 1].Enabled)
                {
                    timers.Timer[timerNumber + 1].Increment();
                }
            }
        }

        public void AddCycles()
        {
            UInt32 elapsedCycles = gba.Cpu.Cycles - startCycle; 
            if(elapsedCycles == 0)
            {
                return;
            }

            if (gba.Cpu.Cycles >= FiresOnCycle)
            {
                if ((gba.Cpu.Cycles - FiresOnCycle) >= 25)
                {
                    throw new ArgumentException("Late Timer Fire!");
                }

                if (IrqEnable)
                {
                    
                    Interrupts.InterruptType interrupt = (Interrupts.InterruptType)((int)(Interrupts.InterruptType.Timer0Overflow) + timerNumber);
                    gba.Interrupts.RequestInterrupt(interrupt);
                }


                TimerValue = ReloadValue.Value;
                CalculateWhichCycleTheTimerWillFire();

                // Deal with cascading timers (recursive) 
                if (timerNumber != 3 &&
                    timers.Timer[timerNumber + 1].CascadeMode &&
                    timers.Timer[timerNumber + 1].Enabled)
                {
                    timers.Timer[timerNumber + 1].Increment();
                }
            }
            else
            {
                TimerValue += (ushort)(elapsedCycles / timers.TimerPeriods[Freq]);
            }          
        }
        

        void CalculateWhichCycleTheTimerWillFire()
        {
            if(CascadeMode || !Enabled)
            {
                FiresOnCycle = 0xFFFFFFFF;
                return;
            }

            UInt32 cycle = (UInt32)(gba.Cpu.Cycles + ((0xFFFF - TimerValue) * timers.TimerPeriods[Freq]));
            FiresOnCycle = cycle;

            // Keep track on when we next need to update timers
            timers.CalculateWhenToNextUpdate();
        }

    }
}
