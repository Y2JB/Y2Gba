using System;
using System.Drawing;
using System.IO;

namespace Gba.Core
{
    public class TileMap
    {
        byte[] vram;
        BgControlRegister cntReg;    
        


        // BG's are laid out in memory in 'screen blocks' like so:
        //   0011
        //   2233
        // Where 00, 11, 22, 33 are each a single screen block of 32x32 tiles (256x256 pixels). Therefore pixel (256, 0) is in tile screen block in 11
        // 00 uses BG screen base address (Bit 8-12 of BG#CNT), 11 uses same address +2K, 22 address +4K, 33 address +6K
        TileMapEntry[] screenBlock0;
        TileMapEntry[] screenBlock1;
        TileMapEntry[] screenBlock2;
        TileMapEntry[] screenBlock3;


        public TileMap(byte[] vram, BgControlRegister cntReg)
        {
            this.cntReg = cntReg;
            this.vram = vram;
            
           
            // Regardless of Bg size, we create enough tol hold the max size
            screenBlock0 = new TileMapEntry[1024];
            screenBlock1 = new TileMapEntry[1024];
            screenBlock2 = new TileMapEntry[1024];
            screenBlock3 = new TileMapEntry[1024];
            for (int i = 0; i < 1024; i++)
            {
                screenBlock0[i] = new TileMapEntry(vram);
                screenBlock1[i] = new TileMapEntry(vram);
                screenBlock2[i] = new TileMapEntry(vram);
                screenBlock3[i] = new TileMapEntry(vram);
            }
        }

        public int TileCount
        {
            get
            {
                switch (cntReg.Size)
                {
                    case BgSize.Bg256x256:
                        return 1024;

                    case BgSize.Bg512x256:
                    case BgSize.Bg256x512:
                        return 2048;

                    case BgSize.Bg512x512:
                        return 4096;

                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public void Reset()
        {

            /*
            if (vramMapDataOffset % 2048 != 0 ||
                vramMapDataOffset > 0xF800)
            {
                throw new ArgumentException("BG map must be located at a valid offset");
            }
            */

            UInt32 vramMapDataOffset = cntReg.ScreenBlockBaseAddress * 2048;

            for (int i = 0; i < 1024; i++)
            {
                screenBlock0[i].VramOffset = (UInt32)(vramMapDataOffset + (i * 2));
                screenBlock1[i].VramOffset = (UInt32)(vramMapDataOffset + 2048 + (i * 2));
                screenBlock2[i].VramOffset = (UInt32)(vramMapDataOffset + 4096 + (i * 2));
                screenBlock3[i].VramOffset = (UInt32)(vramMapDataOffset + 6144 + (i * 2));
            }
        }


        TileMapEntry TileMapItemFromScreenBlockIndex(TileMapEntry[] screenBlock, int index)
        {
            return screenBlock[index];
        }


        // Returns the item from within a screen block
        TileMapEntry TileMapItemFromScreenBlockXY(TileMapEntry[] screenBlock, int x, int y)
        {
            // X & Y can only be 0 - 256 but we can't add checks everytime or things will slow to a crawl, keep this commented out unless debugging 
            if (x >= 256 || y >= 256)
            {
                throw new ArgumentException("Bad screen block call");
            }

            int tileX = (x / 8);
            int tileY = (y / 8);
            return screenBlock[((tileY * 32) + tileX)];
        }



        // Which tile occupies the x,y in the bg map space?
        public TileMapEntry TileMapItemFromBgXY(int x, int y)
        {       
            // 0011
            // 2233
            if(x < 256)
            {
                // Must be 00 or 22
                if (y < 256)
                {
                    // 00
                    return TileMapItemFromScreenBlockXY(screenBlock0, x, y);
                }
                else
                {
                    // 22
                    return TileMapItemFromScreenBlockXY(screenBlock2, x, y - 256);
                }
            }
            else
            {
                // Must be 11 or 33
                if (y < 256)
                {
                    // 11
                    return TileMapItemFromScreenBlockXY(screenBlock1, x - 256, y);
                }
                else
                {
                    // 33
                    return TileMapItemFromScreenBlockXY(screenBlock3, x - 256, y - 256);
                }
            }

            throw new ArgumentException("Failed to lookup tile map data");
        }


        public void DumpTileMap()
        {
            string fn = String.Format("tilemapBg{0}.txt", 0);
            using (FileStream fs = File.Open("../../../../dump/" + fn, FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    for (int y = 0; y < 32; y++)
                    {
                        for (int x = 0; x < 64; x++)
                        {
                            var tileInfo = TileMapItemFromBgXY(x * 8, y * 8);
                            string tileIndex = string.Format("{0:X3}:{1:X}  ", tileInfo.TileNumber, tileInfo.Palette);
                            sw.Write(tileIndex);
                        }
                        sw.Write(Environment.NewLine);
                    }
                }
            }
        }

    }


    // 0-9 (T) = The tile number 
    // A   (H) = If this bit is set, the tile is flipped horizontally left to right. 
    // B   (V) = If this bit is set, the tile is flipped vertically upside down. 
    // C-F (L) = Palette number 
    public class TileMapEntry
    {
        byte[] vram;
        
        public UInt32 VramOffset { get; set; }

        public TileMapEntry(byte[] vram)
        {
            this.vram = vram;
        }

        public int TileNumber { get { return (vram[VramOffset] | (vram[VramOffset + 1] << 8)) & 0x3FF; } }
        public bool FlipHorizontal { get { return (vram[VramOffset + 1] & 0x400) != 0; } }
        public bool FlipVertical { get { return (vram[VramOffset + 1] & 0x800) != 0; } }
        public int Palette { get { return ((vram[VramOffset + 1] & 0xF0) >> 4); } }
    }
}
