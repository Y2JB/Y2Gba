using System;
using System.IO;

namespace Gba.Core
{
    public class TileMap
    {
        //IPpu ppu;
        IMemoryReader memory;
        readonly UInt32 vramOffset;

         public TileMap(IMemoryReader memory, UInt32 vramOffset)
        {
            if(vramOffset != 0x9800 && vramOffset != 0x9C00)
            {
                throw new ArgumentException("Background map must be located at cofrrect address");
            }
            this.vramOffset = vramOffset;

            this.memory = memory;
        }


        byte GetTileIndex(byte index)
        {
            return memory.ReadByte((ushort)(vramOffset + index));
        }

/*
        // Which tile occupies the x,y in the 256x256 screen space?
        byte TileIndexFromXY(byte x, byte y)
        {
            byte tileX = (byte) (x / 8);
            byte tileY = (byte) (y / 8);
            return memory.ReadByte((ushort)(vramOffset + (tileY * 32) + tileX));
        }


        public Tile TileFromXY(byte x, byte y)
        {
            byte tileIndex = TileIndexFromXY(x, y);
          
            ushort vramPointer;
            if (ppu.MemoryRegisters.LCDC.BgAndWindowTileAddressingMode == 0)
            {
                sbyte signedTileIndex = (sbyte)tileIndex;
                // The "8800 method" uses $9000 as its base pointer and uses a signed addressing
                vramPointer = (ushort)(0x9000 + (short)(signedTileIndex * 16));
            }
            else
            {
                // The "8000 method" uses $8000 as its base pointer and uses an unsigned addressing,
                vramPointer = (ushort)(0x8000 + (ushort)(tileIndex * 16));
            }

            return ppu.GetTileByVRamAdrressFast(vramPointer);
        }
*/

        public void DumpTileMap()
        {
            string fn = String.Format("tilemap{0:x}.txt", vramOffset);
            using (FileStream fs = File.Open("../../../../dump/" + fn, FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    for (int y = 0; y < 32; y++)
                    {
                        for (int x = 0; x < 32; x++)
                        {
                            string tileIndex = string.Format("{0:X2} ", memory.ReadByte((ushort)(vramOffset + (y * 32) + x)));
                            sw.Write(tileIndex);
                        }
                        sw.Write(Environment.NewLine);
                    }
                }
            }
        }


    }
}
