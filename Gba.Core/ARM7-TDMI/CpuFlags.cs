using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    public partial class Cpu
    {        
        public enum StatusFlag : UInt32
        {
            Negative            = 1U << 31,
            Zero                = 1U << 30,
            Carry               = 1U << 29,
            Overflow            = 1U << 28,
            IrqDisable          = 1U << 7,
            FirqDisable         = 1U << 6,
            ThumbExecution      = 1U << 5

            // Bottom 5 bits contain Cpu mode
        }

        public enum CpuMode
        {
            User         = 0x10,
            System       = 0x1F,
            FIQ          = 0x11,   // Fast Interrupt Request (Not used on GBA)
            Supervisor   = 0x13,   // SVC
            Abort        = 0x17,
            IRQ          = 0x12,
            Undefined    = 0x1B
        }

        bool ZeroFlag
        {
            get
            {
                return (CPSR & (UInt32) (StatusFlag.Zero)) != 0;
            }
        }


        bool NegativeFlag
        {
            get
            {
                return (CPSR & (UInt32) (StatusFlag.Negative)) != 0;
            }
        }


        bool OverflowFlag
        {
            get
            {
                return (CPSR & (UInt32)(StatusFlag.Overflow)) != 0;
            }
        }

        bool CarryFlag
        {
            get
            {
                return (CPSR & (UInt32) (StatusFlag.Carry)) != 0;
            }
        }

        bool ThumbFlag
        {
            get
            {
                return (CPSR & (UInt32)(StatusFlag.ThumbExecution)) != 0;
            }
        }


        public bool IrqDisableFlag
        {
            get
            {
                return (CPSR & (UInt32)(StatusFlag.IrqDisable)) != 0;
            }
        }


        public void SetFlag(StatusFlag flag)
        {
            CPSR |= (UInt32)flag;
        }


        public void ClearFlag(StatusFlag flag)
        {
            CPSR &= (UInt32)~((UInt32)flag);
        }


        void UpdateCsprModeBits(CpuMode mode)
        {
            CPSR |= (UInt32) mode;
        }
    }
}
