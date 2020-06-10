using System;
using System.IO;

namespace Gba.Core
{
    public class TileMap
    {
        byte[] vram;
        readonly UInt32 vramOffset;
        BgSize bgSize;       
        TileMapEntry[] tileMapData;

        public int TileCount { get; private set; }


        public TileMap(byte[] vram, UInt32 vramOffset, BgSize bgSize)
        {
            if (vramOffset % 2048 != 0 ||
                vramOffset > 0xF800)
            {
                throw new ArgumentException("BG map must be located at a valid offset");
            }

            this.vramOffset = vramOffset;
            this.vram = vram;
            this.bgSize = bgSize;

            switch (bgSize)
            {
                case BgSize.Bg256x256:
                    TileCount = 1024;
                    break;


                default:
                    throw new NotImplementedException();
            }

            tileMapData = new TileMapEntry[1024];
            for(int i = 0; i < tileMapData.Length; i++)
            {
                tileMapData[i] = new TileMapEntry(vram, (UInt32) (vramOffset + (i * 2)));
            }
        }


        public TileMapEntry TileMapItemFromIndex(int index)
        {
            return tileMapData[index];
        }


        // Which tile occupies the x,y in the 256x256 screen space?
        public TileMapEntry TileMapItemFromXY(int x, int y)
        {
            int tileX = (x / 8);
            int tileY = (y / 8);
            return TileMapItemFromIndex((tileY * 32) + tileX);
        }

        /*
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

        */
    }


    // 0-9 (T) = The tile number 
    // A   (H) = If this bit is set, the tile is flipped horizontally left to right. 
    // B   (V) = If this bit is set, the tile is flipped vertically upside down. 
    // C-F (L) = Palette number 
    public class TileMapEntry
    {
        byte[] vram;
        UInt32 vramOffset;

        public TileMapEntry(byte[] vram, UInt32 vramOffset)
        {
            this.vramOffset = vramOffset;
            this.vram = vram;
        }

        public int TileNumber { get { return (vram[vramOffset] | (vram[vramOffset + 1] << 8)) & 0x3FF; } }
        public bool FlipHorizontal { get { return (vram[vramOffset + 1] & 0x400) != 0; } }
        public bool FlipVertical { get { return (vram[vramOffset + 1] & 0x800) != 0; } }
        public int Palette { get { return ((vram[vramOffset + 1] & 0xF000) >> 12); } }
    }
}
