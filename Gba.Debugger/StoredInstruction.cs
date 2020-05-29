using System;
using Gba.Core;

namespace GbaDebugger
{
    // We bolt onto the instruction the state it had when it executed - pc, operand etc
    // We can do more expensive processing in here as we only create these when a progam is being debugged
    public class StoredInstruction
    {
        public string friendlyInstruction { get; set; }

        // These methods are only used when peeking the instruction, not when exectuing as then the data needs to be fetched 
        public UInt32 PC { get; set; }

        //GameboyAdvance gba;

        public StoredInstruction(string friendlyInstruction, UInt32 pc) 
        {
            this.friendlyInstruction = friendlyInstruction;
            this.PC = pc;
        }


        public override String ToString()
        {
            //string instructionWithOperand = "UNFORMATED";

            return String.Format("({0:X8})  ->  {1}", PC, friendlyInstruction);
        }
    }
}
