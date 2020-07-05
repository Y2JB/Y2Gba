using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Gba.Core
{
    public class OamAffineMatrix
    {
        // These are 8.8 fixed point values. They must be sigend to work correctly.
        // |Pa Pb|
        // |Pc Pd|
        public short Pa { get { return (short) ((oamRam[oamRamOffset + 1] << 8) | oamRam[oamRamOffset]); } }
        public short Pb { get { return (short) ((oamRam[oamRamOffset + 9] << 8) | oamRam[oamRamOffset + 8]); } }
        public short Pc { get { return (short) ((oamRam[oamRamOffset + 17] << 8) | oamRam[oamRamOffset + 16]); } }
        public short Pd { get { return (short) ((oamRam[oamRamOffset + 25] << 8) | oamRam[oamRamOffset + 24]); } }

        byte[] oamRam;
        UInt32 oamRamOffset;


        public OamAffineMatrix(byte[] oamRam, UInt32 oamRamOffset)
        {
            this.oamRam = oamRam;
            this.oamRamOffset = oamRamOffset;
        }


        // The game will set these matices up to be the inverse texture mapping matrix so that they map from screen space to texture space.
        // This allows you to easily map (via this multiply) to do scale / rot / sheer
        public void Multiply(int xIn, int yIn, out int xOut, out int yOut)
        {
            // Fixed point arithmetic works with ints, you just shift away the fraction part at the end
            xOut = (((xIn * Pa) + (yIn * Pb)) >> 8);
            yOut = (((xIn * Pc) + (yIn * Pd)) >> 8);
        }
     
    }
}
