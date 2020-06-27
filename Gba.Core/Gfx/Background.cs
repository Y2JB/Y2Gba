﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
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

        // Cached data for rendering
        int bgWidthInPixel;
        int bgHeightInPixel;
        bool eightBitColour;

        public int BgNumber { get; private set; }

        GameboyAdvance gba;


        public Background(GameboyAdvance gba, int bgNumber)
        {
            this.gba = gba;
            this.BgNumber = bgNumber;
            CntRegister = gba.LcdController.BgControlRegisters[bgNumber];

            TileMap = new TileMap(gba.Memory.VRam, gba.LcdController.BgControlRegisters[bgNumber], bgNumber);
        }

        public void CacheRenderData()
        {          
            // TODO: Just make this a LUT
            // 0-3, in units of 16 KBytes
            tileDataVramOffset = (CntRegister.TileBlockBaseAddress * 16384);

            bgWidthInPixel = WidthInPixels();
            bgHeightInPixel = HeightInPixels();

            eightBitColour = CntRegister.PaletteMode == BgPaletteMode.PaletteMode256x1;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int RenderPixel(int screenX, int screenY)
        {
            int paletteOffset = 0;

            int scrollX = ScrollX;
            if (scrollX >= bgWidthInPixel) scrollX -= bgWidthInPixel;

            int scrollY = ScrollY;
            if (scrollY >= bgHeightInPixel) scrollY -= bgHeightInPixel;

            int wrappedBgY = scrollY + screenY;
            if (wrappedBgY >= bgHeightInPixel) wrappedBgY -= bgHeightInPixel;

            // Which line within the current tile are we rendering?
            int tileRow = wrappedBgY % 8;

            // If we reach the edge of the Bg, wrap around
            int wrappedBgX = scrollX + screenX;
            if (wrappedBgX >= bgWidthInPixel) wrappedBgX -= bgWidthInPixel;

            // Which column within the current tile are we rendering?
            int tileColumn = wrappedBgX % 8;

            var tileMetaData = TileMap.TileMapItemFromBgXY(wrappedBgX, wrappedBgY);

            // If we are in 4 bpp mode the tilemap contains which 16 colour palette to use. 16 entries per palette
            if (eightBitColour == false)
            {
                paletteOffset = tileMetaData.Palette * 16;
            }

            int tileVramOffset = (int)(tileDataVramOffset + ((tileMetaData.TileNumber) * tileSize4bit));

            int paletteIndex = TileHelpers.GetTilePixel(tileColumn, tileRow, eightBitColour, gba.Memory.VRam, tileVramOffset, tileMetaData.FlipHorizontal, tileMetaData.FlipVertical);

            if (paletteIndex == 0) return 0;

            return paletteOffset + paletteIndex;          
        }

        public void RenderMode0Scanline(int scanline, int scanlineWidth, DirectBitmap drawBuffer)
        {
            Color[] palette = gba.LcdController.Palettes.Palette0;
            int paletteOffset = 0;

            bool eightBitColour = CntRegister.PaletteMode == BgPaletteMode.PaletteMode256x1;

            int scrollX = ScrollX;
            if (scrollX >= bgWidthInPixel) scrollX -= bgWidthInPixel;

            int scrollY = ScrollY;
            if (scrollY >= bgHeightInPixel) scrollY -= bgHeightInPixel;

            int wrappedBgY = scrollY + scanline;
            if (wrappedBgY >= bgHeightInPixel) wrappedBgY -= bgHeightInPixel;

            // Which line within the current tile are we rendering?
            int tileRow = wrappedBgY % 8;

            for (int x = 0; x < scanlineWidth; x ++)
            {
                // If we reach the edge of the Bg, wrap around
                int wrappedBgX = scrollX + x;
                if (wrappedBgX >= bgWidthInPixel) wrappedBgX -= bgWidthInPixel;

                // Which column within the current tile are we rendering?
                int tileColumn = wrappedBgX % 8;

                var tileMetaData = TileMap.TileMapItemFromBgXY(wrappedBgX, wrappedBgY);

                // If we are in 4 bpp mode the tilemap contains which 16 colour palette to use. 16 entries per palette
                if (eightBitColour == false)
                {
                    paletteOffset = tileMetaData.Palette * 16;
                }

                // 4 bytes represent one row of pixel data for a single tile
                int tileVramOffset = (int)(tileDataVramOffset + ((tileMetaData.TileNumber) * tileSize4bit));

                int paletteIndex = TileHelpers.GetTilePixel(tileColumn, tileRow, eightBitColour, gba.Memory.VRam, tileVramOffset, tileMetaData.FlipHorizontal, tileMetaData.FlipVertical);
                
                // Pal 0 == Transparent 
                if (paletteIndex == 0)
                {
                    continue;
                }

                drawBuffer.SetPixel(x, scanline, palette[paletteOffset + paletteIndex]);
            }
        }


        // Used for dumping BG's or drawing to debug windows. The game does not render this way
        public void RenderFull(DirectBitmap drawBuffer)
        {
            for(int y = 0; y < HeightInPixels(); y++)
            {
                RenderMode0Scanline(y, WidthInPixels(), drawBuffer);
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


        public void Dump(bool renderViewPortBox)
        {
            var image = new DirectBitmap(WidthInPixels(), HeightInPixels());
            RenderFull(image);
            if (renderViewPortBox)
            {
                GfxHelpers.DrawViewportBox(image.Bitmap, ScrollX, ScrollY, WidthInPixels(), HeightInPixels());
            }
            image.Bitmap.Save(string.Format("../../../../dump/Bg{0}.png", BgNumber));
        }


    }
}
