using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Gba.Core
{
    public class BgAffineMatrix
    {
        // These are 8.8 fixed point values. They must be sigend to work correctly.
        // |Pa Pb|
        // |Pc Pd|

        public byte PaL { get; set; }
        public byte PaH { get; set; }
        public byte PbL { get; set; }
        public byte PbH { get; set; }
        public byte PcL { get; set; }
        public byte PcH { get; set; }
        public byte PdL { get; set; }
        public byte PdH { get; set; }

        public short Pa { get { return (short)((PaH << 8) | PaL); } }
        public short Pb { get { return (short)((PbH << 8) | PbL); } }
        public short Pc { get { return (short)((PcH << 8) | PcL); } }
        public short Pd { get { return (short)((PdH << 8) | PdL); } }


        // The game will set these matices up to be the inverse texture mapping matrix so that they map from screen space to texture space.
        // This allows you to easily map (via this multiply) to do scale / rot / sheer
        public void Multiply(int xIn, int yIn, out int xOut, out int yOut)
        {
            // Fixed point arithmetic works with ints as everything just overflows nicely, you just have to shift away the fraction part at the end
            xOut = (((xIn * Pa) + (yIn * Pb)) >> 8);
            yOut = (((xIn * Pc) + (yIn * Pd)) >> 8);
        }
     
    }
}
