using System;


namespace Gba.Core
{
    public class Joypad
    {
        // When a key is pressed, its state on a GB is 0
        byte register0 = 0xFF;
        byte register1 = 0xFF;

        // Bits 5 & 6 select if we are reading buttons or the pad. The program sets 5 & 6 to tell us what it wants

        public byte Register0
        {
            get
            {
                UpdateRegister();
                return register0;
            }
            set
            {
                register0 = value;                
            }
        }

        public byte Register1
        {
            get
            {
                UpdateRegister();
                return register1;
            }
            set
            {
                register1 = value;
            }
        }

        public enum GbaKey
        {
            A = 0,
            B,
            Select,
            Start,            
            Right,
            Left,
            Up,
            Down,            
            R,          
            L                    
        }

        public enum GbaKeyBits
        {
            A_Bit       = 1 << 0,
            B_Bit       = 1 << 1,
            Select_Bit  = 1 << 2,
            Start_Bit   = 1 << 3,
            Right_Bit   = 1 << 4,            
            Left_Bit    = 1 << 5,            
            Up_Bit      = 1 << 6,            
            Down_Bit    = 1 << 7,
           
            R_Bit       = 1 << 0,
            L_Bit       = 1 << 1

        }


        // We capture key state the 'right' way, true == pressed
        bool[] keys = new bool[10];

        GameboyAdvance gba;
        

        UInt32 lastCpuTickCount;
        UInt32 elapsedTicks;


        public Joypad(GameboyAdvance gba)
        {
            this.gba = gba;
        }


        public void Reset()
        {
            register0 = 0xFF;
            register1 = 0x02;

            for (int i=0; i < 8; i++)
            {
                keys[i] = false;
            }
        }


        public void Step()
        {
            /*
            UInt32 cpuTickCount = dmg.cpu.Ticks;

            // Track how many cycles the CPU has done since we last changed states
            elapsedTicks += (cpuTickCount - lastCpuTickCount);
            lastCpuTickCount = cpuTickCount;

            // Joypad Poll Speed (64 Hz)
            //if (elapsedTicks >= 65536)        // tcycles
            if (elapsedTicks >= 16384)          // mcycles
            {
                elapsedTicks -= 16384;
                UpdateRegister();
            }
            */
        }


        void UpdateRegister()
        {
            // Default everything to pressed then turn off what isn't pressed
            register0 = register1 = 0;

            if (keys[(int)GbaKey.A] == false) register0 |= (byte)(GbaKeyBits.A_Bit);
            if (keys[(int)GbaKey.B] == false) register0 |= (byte)(GbaKeyBits.B_Bit);
            if (keys[(int)GbaKey.Start] == false) register0 |= (byte)(GbaKeyBits.Start_Bit);
            if (keys[(int)GbaKey.Select] == false) register0 |= (byte)(GbaKeyBits.Select_Bit);
            if (keys[(int)GbaKey.Up] == false) register0 |= (byte)(GbaKeyBits.Up_Bit);
            if (keys[(int)GbaKey.Down] == false) register0 |= (byte)(GbaKeyBits.Down_Bit);
            if (keys[(int)GbaKey.Left] == false) register0 |= (byte)(GbaKeyBits.Left_Bit);
            if (keys[(int)GbaKey.Right] == false) register0 |= (byte)(GbaKeyBits.Right_Bit);

            if (keys[(int)GbaKey.L] == false) register1 |= (byte)(GbaKeyBits.L_Bit);
            if (keys[(int)GbaKey.R] == false) register1 |= (byte)(GbaKeyBits.R_Bit);
        }


        public void UpdateKeyState(GbaKey key, bool state)
        {
            /*
            // Interrupt occurs when a button becomes pressed
            bool fireInterrupt = false;
            if(keys[(int)key] == false && state == true)
            {
                fireInterrupt = true;
            }
            */

            keys[(int)key] = state;

            /*
            if (fireInterrupt)
            {
                interrupts.RequestInterrupt(Interrupts.Interrupt.INTERRUPTS_JOYPAD);
            }
            */
        }
    }
}
