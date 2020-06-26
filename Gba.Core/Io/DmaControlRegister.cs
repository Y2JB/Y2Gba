using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    public class DmaControlRegister
    {
        public byte dmaCntRegister0 { get; set; }
        public byte dmaCntRegister1 { get; set; }
        public ushort DmaCntRegister
        {
            get { return (ushort)((dmaCntRegister1 << 8) | dmaCntRegister0); }
            set { dmaCntRegister0 = (byte)(value & 0x00FF); dmaCntRegister1 = (byte)((value & 0xFF00) >> 8); }
        }

        public bool Repeat { get { return ((dmaCntRegister1 & 0x20) != 0); } }
        public bool IrqEnable { get { return ((dmaCntRegister1 & 0x40) != 0); } }
        public bool ChannelEnabled { get { return (dmaCntRegister1 & 0x80) != 0;  } }
    }


}
