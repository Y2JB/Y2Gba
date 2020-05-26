using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    public partial class Cpu
    {      
     
        void Branch(UInt32 nn)
        {
            PC = PC + 8 + nn * 4;
        }


        void BranchAndLink(UInt32 nn)
        {
            PC = PC + 8 + nn * 4;
            LR = PC + 4;
        }





    }
}
