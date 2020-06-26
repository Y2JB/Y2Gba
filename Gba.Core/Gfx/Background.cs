using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Gba.Core
{
    public class Background
    {
        public BgControlRegister CntRegister { get; }

        public TileMap TileMap { get; private set; }
        public BgSize Size { get { return CntRegister.Size; } }

        public int ScrollX { get; set; }
        public int ScrollY { get; set; }

        UInt32 tileDataVramOffset;

        const int tileSize4bit = 32;
        const int tileSize8bit = 64;

        int bgNumber;
        GameboyAdvance gba;


        public Background(GameboyAdvance gba, int bgNumber)
        {
            this.gba = gba;
            this.bgNumber = bgNumber;
            CntRegister = gba.LcdController.BgControlRegisters[bgNumber];

            TileMap = new TileMap(gba.Memory.VRam, gba.LcdController.BgControlRegisters[bgNumber], bgNumber);
        }

        public void Reset()
        {                       
            TileMap.Reset();

            // 0-3, in units of 16 KBytes
            tileDataVramOffset = (CntRegister.TileBlockBaseAddress * 16384);
        }

       
        public void RenderMode0Scanline4bpp(int scanline, DirectBitmap drawBuffer)
        {
            Color[] palette = gba.LcdController.Palettes.Palette0;
            int paletteOffset;

            int bgWidthInPixel = WidthInPixels();
            int bgHeightInPixel = HeightInPixels();

            int scrollX = ScrollX;
            if (scrollX >= bgWidthInPixel) scrollX -= bgWidthInPixel;

            int scrollY = ScrollY;
            if (scrollY >= bgHeightInPixel) scrollY -= bgHeightInPixel;

            for (int x = 0; x < LcdController.Screen_X_Resolution; x ++)
            {
                // If we reach the edge of the Bg, wrap around
                int wrappedBgX = scrollX + x;
                if (wrappedBgX >= bgWidthInPixel) wrappedBgX -= bgWidthInPixel;
                int wrappedBgY = scrollY + scanline;
                if (wrappedBgY >= bgHeightInPixel) wrappedBgY -= bgHeightInPixel;

                // Which line within the current tile are we rendering?
                int tileRow = wrappedBgY % 8;

                // Which column within the current tile are we rendering?
                int tileColumn = wrappedBgX % 8;

                var tileMetaData = TileMap.TileMapItemFromBgXY(wrappedBgX, wrappedBgY);

                // We are in 4 bpp mode so the tilemap contains which 16 colour palette to use. 16 entries per palette
                paletteOffset = tileMetaData.Palette * 16;

                // 4 bytes represent one row of pixel data for a single tile
                int tileVramOffset = (int)(tileDataVramOffset + ((tileMetaData.TileNumber) * tileSize4bit));

                int paletteIndex = TileHelpers.GetTilePixel(tileColumn, tileRow, false, gba.Memory.VRam, tileVramOffset, tileMetaData.FlipHorizontal, tileMetaData.FlipVertical);
                
                // Pal 0 == Transparent 
                if (paletteIndex == 0)
                {
                    continue;
                }

                drawBuffer.SetPixel(x, scanline, palette[paletteOffset + paletteIndex]);
            }
        }


        public int WidthInPixels()
        {
            if (Size == BgSize.Bg256x256 || Size == BgSize.Bg256x512) return 256;
            return 512;
        }


        public int HeightInPixels()
        {
            if (Size == BgSize.Bg256x256 || Size == BgSize.Bg512x256) return 256;
            return 512;
        }


        public void DumpBg()
        {        
        }


    }
}
