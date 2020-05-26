using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    public partial class Cpu
    {
        UInt32[] registers = new UInt32[16];

        // Flags - Current Program Status Register
        UInt32 CPSR;

        enum RegisterNames
        {
            R0 = 0,
            R1,
            R2,
            R3,
            R4,
            R5,
            R6,
            R7,
            R8,
            R9,
            R10,
            R11,
            R12,
            R13,
            R14,
            R15,

            PC = R15
        }

        UInt32 GetRegisterValue(int index)
        {
            return registers[index];
        }

        public UInt32 PC { get { return registers[15]; } set { registers[15] = value; } }
        
        // Link register, used to store return address when making a function call. Program must manage it when doing nested calls.
        public UInt32 LR { get { return registers[14]; } set { registers[14] = value; } }

        // Used as Stack Pointer (SP) in THUMB state. While in ARM state the user may decided to use R13 and/or other register(s) as stack pointer(s), or as general purpose register.
        public UInt32 SP { get { return registers[13]; } set { registers[13] = value; } }

        public UInt32 R0 { get { return registers[0]; } set { registers[0] = value; } }
        public UInt32 R1 { get { return registers[1]; } set { registers[1] = value; } }
        public UInt32 R2 { get { return registers[2]; } set { registers[2] = value; } }
        public UInt32 R3 { get { return registers[3]; } set { registers[3] = value; } }
        public UInt32 R4 { get { return registers[4]; } set { registers[4] = value; } }
        public UInt32 R5 { get { return registers[5]; } set { registers[5] = value; } }
        public UInt32 R6 { get { return registers[6]; } set { registers[6] = value; } }
        public UInt32 R7 { get { return registers[7]; } set { registers[7] = value; } }
        public UInt32 R8 { get { return registers[8]; } set { registers[8] = value; } }
        public UInt32 R9 { get { return registers[9]; } set { registers[9] = value; } }
        public UInt32 R10 { get { return registers[10]; } set { registers[10] = value; } }
        public UInt32 R11 { get { return registers[11]; } set { registers[11] = value; } }
        public UInt32 R12 { get { return registers[12]; } set { registers[12] = value; } }
        public UInt32 R13 { get { return registers[13]; } set { registers[13] = value; } }
        public UInt32 R14 { get { return registers[14]; } set { registers[14] = value; } }
        public UInt32 R15 { get { return registers[15]; } set { registers[15] = value; } }
    }
}
