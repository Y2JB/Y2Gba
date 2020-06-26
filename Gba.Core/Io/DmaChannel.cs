using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    public class DmaChannel
    {
        public DmaControlRegister DmaCnt { get; set; }

        // The most significant address bits are ignored, only the least significant 27 or 28 bits are used(max 07FFFFFFh internal memory, or max 0FFFFFFFh any memory
        public byte sAddr0 { get; set; }
        public byte sAddr1 { get; set; }
        public byte sAddr2 { get; set; }
        public byte sAddr3 { get; set; }
        public UInt32 SourceAddress
        {
            get { return (UInt32)((sAddr3 << 24) | (sAddr2 << 16) |(sAddr1 << 8) | sAddr0); }
            set { sAddr0 = (byte)(value & 0xFF); sAddr1 = (byte)((value & 0xFF00) >> 8); sAddr2 = (byte)((value & 0xFF0000) >> 16); sAddr3 = (byte)((value & 0xFF000000) >> 24);  }
        }

        // The most significant address bits are ignored, only the least significant 27 or 28 bits are used (max 07FFFFFFh internal memory, or max 0FFFFFFFh any memory
        public byte dAddr0 { get; set; }
        public byte dAddr1 { get; set; }
        public byte dAddr2 { get; set; }
        public byte dAddr3 { get; set; }
        public UInt32 DestAddress
        {
            get { return (UInt32)((dAddr3 << 24) | (dAddr2 << 16) | (dAddr1 << 8) | dAddr0); }
            set { dAddr0 = (byte)(value & 0xFF); dAddr1 = (byte)((value & 0xFF00) >> 8); dAddr2 = (byte)((value & 0xFF0000) >> 16); dAddr3 = (byte)((value & 0xFF000000) >> 24); }
        }


        // Specifies the number of data units to be transferred, each unit is 16bit or 32bit depending on the transfer type, a value of zero is treated 
        // as max length (ie. 4000h, or 10000h for DMA3).
        public byte wordCount0 { get; set; }
        public byte wordCount1 { get; set; }
        public ushort WordCount
        {
            get { return (ushort)((wordCount1 << 8) | wordCount0); }
            set { wordCount0 = (byte)(value & 0xFF); wordCount1 = (byte)((value & 0xFF00) >> 8); }
        }

        GameboyAdvance gba;

        public DmaChannel(GameboyAdvance gba)
        {
            this.gba = gba;

            DmaCnt = new DmaControlRegister();
        }

    }
}
