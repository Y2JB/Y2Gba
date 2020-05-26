using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    public enum ConditionalExecution
    {
        EQ = 0x0,               // Equal                    Z==1
        NE,                     // Not Equal                Z==0
        CS,                     // Carry Set                C==1
        CC,                     // Carry Clear              C==0
        MI,                     // Negative                 N==1
        PL,                     // Possitive                N==0
        VS,                     // Overflow                 V==1
        VC,                     // No Overflow              V==0
        HI,                     // Unsigned Higer           C==1 && Z==0
        LS,                     // Unsigned Lower or EQ     C==0 || Z==1
        GE,                     // Signed >=                N==V
        LT,                     // Signed <                 N!=V
        GT,                     // Signed >                 Z==0 && N==V
        LE,                     // Signed <=                Z==1 || N!=V
        AL,                     // Always (unconditional)   -
        NV                      // NEVER                    Do not use this
    }

    public partial class Cpu
    {
        // One for each entry in the ConditionalExecution enum 
        Func<bool>[] CondtionalHandlers = new Func<bool>[16]
        {
            () => true,
            () => true,
            () => true,
            () => true,
            () => true,
            () => true,
            () => true,
            () => true,
            () => true,
            () => true,
            () => true,
            () => true,
            () => true,
            () => true,
            () => throw new ArgumentException("No need to ever execute this conditional, AL means there is no conditional"),
            () => throw new ArgumentException("Cannot use this conditional")
        };

    }
}
