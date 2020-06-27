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
        Func<bool>[] CondtionalHandlers = new Func<bool>[16];
        
        void RegisterConditionalHandlers()
        {
            CondtionalHandlers[(int)ConditionalExecution.EQ] = () => ZeroFlag;
            CondtionalHandlers[(int)ConditionalExecution.NE] = () => ZeroFlag == false;
            CondtionalHandlers[(int)ConditionalExecution.CS] = () => CarryFlag;
            CondtionalHandlers[(int)ConditionalExecution.CC] = () => CarryFlag == false;
            CondtionalHandlers[(int)ConditionalExecution.MI] = () => NegativeFlag;
            CondtionalHandlers[(int)ConditionalExecution.PL] = () => NegativeFlag == false;
            CondtionalHandlers[(int)ConditionalExecution.VS] = () => OverflowFlag;
            CondtionalHandlers[(int)ConditionalExecution.VC] = () => OverflowFlag == false;
            CondtionalHandlers[(int)ConditionalExecution.HI] = () => (CarryFlag && ZeroFlag == false);
            CondtionalHandlers[(int)ConditionalExecution.LS] = () => (CarryFlag == false || ZeroFlag);
            CondtionalHandlers[(int)ConditionalExecution.GE] = () => ((NegativeFlag && OverflowFlag) || (!NegativeFlag && !OverflowFlag));
            CondtionalHandlers[(int)ConditionalExecution.LT] = () => ((NegativeFlag && !OverflowFlag) || (!NegativeFlag && OverflowFlag));
            CondtionalHandlers[(int)ConditionalExecution.GT] = () => (!ZeroFlag && ((NegativeFlag && OverflowFlag) || (!NegativeFlag && !OverflowFlag)));
            CondtionalHandlers[(int)ConditionalExecution.LE] = () => (ZeroFlag || ((NegativeFlag && !OverflowFlag) || (!NegativeFlag && OverflowFlag)));
            CondtionalHandlers[(int)ConditionalExecution.AL] = () => throw new ArgumentException("No need to ever execute this conditional, AL means there is no conditional");
            CondtionalHandlers[(int)ConditionalExecution.NV] = () => throw new ArgumentException("Cannot use this conditional");
        }


   
    }
}
