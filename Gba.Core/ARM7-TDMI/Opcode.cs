using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    // I'm listing these in binary as it makes referning the docs a bit easier
    public enum InstructionCatagory : UInt32
    {
        Unkonwn = 0,

        Branch              = 0b10100000,
        Branch_Exchange     = 0b00010010,
        DataProcessing      = 0b00100000,
        SingleDataTransfer  = 0b01000000,
    }

   


}
