using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    public partial class Cpu
    {        
        enum StatusFlag : UInt32
        {
            Negative    = 1U << 31,
            Zero        = 1U << 30,
            Carry       = 1U << 29,
            Overflow    = 1U << 28,
            CumulativeSaturation = 1U << 27,
            Jazelle     = 1U << 24,
            Endianness  = 1U << 9,
            AsyncAbortMask = 1U << 8,
            IrqMask     = 1U << 7,
            FirqMask    = 1U << 6,
            ThumbExecution = 1U << 5
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


        bool CarryFlag
        {
            get
            {
                return (CPSR & (UInt32) (StatusFlag.Carry)) != 0;
            }
        }


        void SetFlag(StatusFlag flag)
        {
            CPSR |= (UInt32)flag;
        }


        void ClearFlag(StatusFlag flag)
        {
            CPSR &= (UInt32)~((UInt32)flag);
        }


        void ClearAllFlags()
        {
            CPSR = 0;
        }
    }
}
