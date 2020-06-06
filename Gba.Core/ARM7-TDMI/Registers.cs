using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{    
    public partial class Cpu
    {
        UInt32[] registers = new UInt32[16];

        UInt32 r8Fiq;
        UInt32 r9Fiq;
        UInt32 r10Fiq;
        UInt32 r11Fiq;
        UInt32 r12Fiq;

        // Flags - Current Program Status Register
        UInt32 CPSR;

        UInt32 SP_Fiq;
        UInt32 SP_Svc;
        UInt32 SP_Abt;
        UInt32 SP_Irq;
        UInt32 SP_Und;

        UInt32 LR_Fiq;
        UInt32 LR_Svc;
        UInt32 LR_Abt;
        UInt32 LR_Irq;
        UInt32 LR_Und;

        UInt32 SPSR_Fiq;
        UInt32 SPSR_Svc;
        UInt32 SPSR_Abt;
        UInt32 SPSR_Irq;
        UInt32 SPSR_Und;

        enum RegisterName
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


        public UInt32 GetRegisterValue(UInt32 index)
        {
            if(index <= 7) return registers[index];

            switch (index)
            {

                case 8:
                    switch (Mode)
                    {
                        case CpuMode.FIQ: return r8Fiq;
                        default: return registers[index]; 
                    }

                case 9:
                    switch (Mode)
                    {
                        case CpuMode.FIQ: return r9Fiq;
                        default: return registers[index]; 
                    }

                case 10:
                    switch (Mode)
                    {
                        case CpuMode.FIQ: return r10Fiq;
                        default: return registers[index]; 
                    }

                case 11:
                    switch (Mode)
                    {
                        case CpuMode.FIQ: return r11Fiq;
                        default: return registers[index]; 
                    }

                case 12:
                    switch (Mode)
                    {
                        case CpuMode.FIQ: return r12Fiq;
                        default: return registers[index]; 
                    }

                case 13:
                    switch (Mode)
                    {
                        case CpuMode.User:
                        case CpuMode.System:
                            return registers[13];
                        case CpuMode.FIQ:
                            return SP_Fiq;
                        case CpuMode.Supervisor:
                            return SP_Svc;
                        case CpuMode.Abort:
                            return SP_Abt;
                        case CpuMode.IRQ:
                            return SP_Irq;
                        case CpuMode.Undefined:
                            return SP_Und;
                        default: throw new ArgumentException("Invalid register get");
                    }

                case 14:
                    switch (Mode)
                    {
                        case CpuMode.User:
                        case CpuMode.System:
                            return registers[14];
                        case CpuMode.FIQ:
                            return LR_Fiq;
                        case CpuMode.Supervisor:
                            return LR_Svc;
                        case CpuMode.Abort:
                            return LR_Abt;
                        case CpuMode.IRQ:
                            return LR_Irq;
                        case CpuMode.Undefined:
                            return LR_Und;
                        default:
                            throw new ArgumentException("Invalid register get");
                    }

                case 15:
                    return PC;
            }

            throw new ArgumentException("Invalid register get");
        }


        public void SetRegisterValue(UInt32 index, UInt32 value)
        {
            if (index <= 7)
            {
                registers[index] = value;
                return;
            }

            switch (index)
            {

                case 8:
                    switch (Mode)
                    {
                        case CpuMode.FIQ: r8Fiq = value; break;
                        default: registers[index] = value; break;
                    }
                    break;

                case 9:
                    switch (Mode)
                    {
                        case CpuMode.FIQ: r9Fiq = value; break;
                        default: registers[index] = value; break;
                    }
                    break;

                case 10:
                    switch (Mode)
                    {
                        case CpuMode.FIQ: r10Fiq = value; break;
                        default: registers[index] = value; break;
                    }
                    break;

                case 11:
                    switch (Mode)
                    {
                        case CpuMode.FIQ: r11Fiq = value; break;
                        default: registers[index] = value; break;
                    }
                    break;

                case 12:
                    switch (Mode)
                    {
                        case CpuMode.FIQ: r12Fiq = value; break;
                        default: registers[index] = value; break;
                    }
                    break;

                case 13:
                    switch (Mode)
                    {
                        case CpuMode.User:
                        case CpuMode.System:
                            registers[13] = value; break;
                        case CpuMode.FIQ:
                            SP_Fiq = value; break;
                        case CpuMode.Supervisor:
                            SP_Svc = value; break;
                        case CpuMode.Abort:
                            SP_Abt = value; break;
                        case CpuMode.IRQ:
                            SP_Irq = value; break;
                        case CpuMode.Undefined:
                            SP_Und = value; break;
                        default: throw new ArgumentException("Invalid register set");
                    }
                    break;

                case 14:
                    switch (Mode)
                    {
                        case CpuMode.User:
                        case CpuMode.System:
                            registers[14] = value; break;
                        case CpuMode.FIQ:
                            LR_Fiq = value; break;
                        case CpuMode.Supervisor:
                            LR_Svc = value; break;
                        case CpuMode.Abort:
                            LR_Abt = value; break;
                        case CpuMode.IRQ:
                            LR_Irq = value; break;
                        case CpuMode.Undefined:
                            LR_Und = value; break;
                        default:
                            throw new ArgumentException("Invalid register set");
                    }
                    break;

                case 15:
                    PC = value;
                    break;

                default:
                    throw new ArgumentException("Invalid register set");
            }
        }


        public string GetRegisterName(UInt32 index)
        {
            if (index == 15) return "PC";
            if (index == 14) return "LR";
            if (index == 13) return "SP";
            return ((RegisterName)(index)).ToString();
        }


        public UInt32 PC { get { return registers[15]; } set { registers[15] = value; } }
        
        // Link register, used to store return address when making a function call. Program must manage it when doing nested calls.
        public UInt32 LR { get { return R14; } set { R14 = value; } }

        // Used as Stack Pointer (SP) in THUMB state. While in ARM state the user may decided to use R13 and/or other register(s) as stack pointer(s), or as general purpose register.
        public UInt32 SP { get { return R13; } set { R13 = value; } }

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
        public UInt32 R15 { get { return registers[15]; } set { registers[15] = value; } }


        public UInt32 R13 
        { 
            get 
            {                
                switch (Mode)
                {
                    case CpuMode.User:
                    case CpuMode.System:
                        return registers[13];
                    case CpuMode.FIQ:
                        return SP_Fiq;
                    case CpuMode.Supervisor:
                        return SP_Svc;
                    case CpuMode.Abort:
                        return SP_Abt;
                    case CpuMode.IRQ:
                        return SP_Irq;
                    case CpuMode.Undefined:
                        return SP_Und;
                }
                throw new ArgumentException("Invalid SP register request");
            } 
            set 
            {
                switch (Mode)
                {
                    case CpuMode.User:
                    case CpuMode.System:
                        registers[13] = value;
                        break;
                    case CpuMode.FIQ:
                        SP_Fiq = value;
                        break;
                    case CpuMode.Supervisor:
                        SP_Svc = value;
                        break;
                    case CpuMode.Abort:
                        SP_Abt = value;
                        break;
                    case CpuMode.IRQ:
                        SP_Irq = value;
                        break;
                    case CpuMode.Undefined:
                        SP_Und = value;
                        break;

                    default:
                        throw new ArgumentException("Invalid SP register set request");
                }              
            } 
        }

        public UInt32 R14
        {
            get
            {
                switch (Mode)
                {
                    case CpuMode.User:
                    case CpuMode.System:
                        return registers[14];
                    case CpuMode.FIQ:
                        return LR_Fiq;
                    case CpuMode.Supervisor:
                        return LR_Svc;
                    case CpuMode.Abort:
                        return LR_Abt;
                    case CpuMode.IRQ:
                        return LR_Irq;
                    case CpuMode.Undefined:
                        return LR_Und;
                }
                throw new ArgumentException("Invalid LR register request");
            }
            set
            {
                switch (Mode)
                {
                    case CpuMode.User:
                    case CpuMode.System:
                        registers[14] = value;
                        break;
                    case CpuMode.FIQ:
                        LR_Fiq = value;
                        break;
                    case CpuMode.Supervisor:
                        LR_Svc = value;
                        break;
                    case CpuMode.Abort:
                        LR_Abt = value;
                        break;
                    case CpuMode.IRQ:
                        LR_Irq = value;
                        break;
                    case CpuMode.Undefined:
                        LR_Und = value;
                        break;

                    default:
                        throw new ArgumentException("Invalid LR register set request");
                }
            }
        }

        public UInt32 PC_Adjusted { get { if (State == CpuState.Arm) return (PC - 8); else return (PC - 4); } }

        public UInt32 SPSR 
        { 
            get 
            {
                switch (Mode)
                {
                    case CpuMode.User:
                    case CpuMode.System: 
                        return CPSR;
                    case CpuMode.FIQ: 
                        return SPSR_Fiq; 
                    case CpuMode.Supervisor: 
                        return SPSR_Svc; 
                    case CpuMode.Abort: 
                        return SPSR_Abt; 
                    case CpuMode.IRQ: 
                        return SPSR_Irq; 
                    case CpuMode.Undefined: 
                        return SPSR_Und; 
                }
                throw new ArgumentException("Invalid SPSR register request");
            } 
            
            set 
            {
                switch (Mode)
                {
                    case CpuMode.User:
                    case CpuMode.System:
                        break;
                    case CpuMode.FIQ:
                        SPSR_Fiq = value;
                        return;
                    case CpuMode.Supervisor:
                        SPSR_Svc = value;
                        return;
                    case CpuMode.Abort:
                        SPSR_Abt = value;
                        return;
                    case CpuMode.IRQ:
                        SPSR_Irq = value;
                        return;
                    case CpuMode.Undefined:
                        SPSR_Und = value;
                        return;
                }
                throw new ArgumentException("Invalid SPSR register request");
            } 
        }
    }
}
