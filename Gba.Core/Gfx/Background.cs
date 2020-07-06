using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Gba.Core
{
    public class Background
    {
        public BgControlRegister CntRegister { get; }

        public TileMap TileMap { get; private set; }
        public BgSize Size { get { return CntRegister.Size; } }

        public int ScrollX { get; set; }
        public int ScrollY { get; set; }

        public bool AffineMode { get; set; }

        // 32 but fixed point numbers. Will only ever be set for BG's 2 & 3. Used instead of the scroll registers above
        public byte affineX0 { get; set; }
        public byte affineX1 { get; set; }
        public byte affineX2 { get; set; }
        public byte affineX3 { get; set; }
        public UInt32 AffineScrollX
        {
            get { return (UInt32)((affineX3 << 24) | (affineX2 << 16) | (affineX1 << 8) | affineX0); }
            set { affineX0 = (byte)(value & 0xFF); affineX1 = (byte)((value & 0xFF00) >> 8); affineX2 = (byte)((value & 0xFF0000) >> 16); affineX3 = (byte)((value & 0xFF000000) >> 24); }
        }

        public byte affineY0 { get; set; }
        public byte affineY1 { get; set; }
        public byte affineY2 { get; set; }
        public byte affineY3 { get; set; }
        public UInt32 AffineScrollY
        {
            get { return (UInt32)((affineY3 << 24) | (affineY2 << 16) | (affineY1 << 8) | affineY0); }
            set { affineY0 = (byte)(value & 0xFF); affineY1 = (byte)((value & 0xFF00) >> 8); affineY2 = (byte)((value & 0xFF0000) >> 16); affineY3 = (byte)((value & 0xFF000000) >> 24); }
        }

        public BgAffineMatrix AffineMatrix { get; private set; }

        UInt32 tileDataVramOffset;

        // Cached data for rendering
        int bgWidthInPixel;
        int bgHeightInPixel;
        int bgWidthInTiles;
        int bgHeightInTiles;
        bool eightBitColour;
        int tileSize;

        public int BgNumber { get; private set; }

        GameboyAdvance gba;


        public Background(GameboyAdvance gba, int bgNumber, BgControlRegister cntRegister)
        {
            this.gba = gba;
            this.BgNumber = bgNumber;
            CntRegister = cntRegister;
            AffineMode = false;
            TileMap = new TileMap(gba.Memory.VRam, cntRegister, bgNumber);

            AffineMatrix = new BgAffineMatrix();
        }


        public void CacheRenderData()
        {
            AffineMode = false;
            if (gba.LcdController.DisplayControlRegister.BgMode == 1 && BgNumber == 2) AffineMode = true;
            else if (gba.LcdController.DisplayControlRegister.BgMode == 2 && (BgNumber == 2 || BgNumber == 3)) AffineMode = true;

            // TODO: Just make this a LUT
            // 0-3, in units of 16 KBytes
            tileDataVramOffset = (CntRegister.TileBlockBaseAddress * 16384);

            bgWidthInPixel = WidthInPixels();
            bgHeightInPixel = HeightInPixels();
            bgWidthInTiles = bgWidthInPixel / 8;
            bgHeightInTiles = bgHeightInPixel / 8;

            eightBitColour = (CntRegister.PaletteMode == BgPaletteMode.PaletteMode256x1 || AffineMode);
            tileSize = (eightBitColour ? LcdController.Tile_Size_8bit : LcdController.Tile_Size_4bit);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int PixelValue(int screenX, int screenY)
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

            int tileVramOffset = (int)(tileDataVramOffset + ((tileMetaData.TileNumber) * tileSize));

            int paletteIndex = TileHelpers.GetTilePixel(tileColumn, tileRow, eightBitColour, gba.Memory.VRam, tileVramOffset, tileMetaData.FlipHorizontal, tileMetaData.FlipVertical);

            if (paletteIndex == 0) return 0;

            return paletteOffset + paletteIndex;          
        }


        public int PixelValueAffine(int screenX, int screenY)
        {
            int paletteOffset = 0;

            // Scrolling values set the origin so that BG 0,0 == Screen 0,0
            int scrollX = 0;
            int scrollY = 0;

            // Coordinates within the current BG
            int wrappedBgX = scrollX + screenX;
            int wrappedBgY = scrollY + screenY;
            //if (wrappedBgX >= bgWidthInPixel) wrappedBgX -= bgWidthInPixel;
            //if (wrappedBgY >= bgHeightInPixel) wrappedBgY -= bgHeightInPixel;

            int bgRow = wrappedBgX / 8;
            int bgCol = wrappedBgY / 8;

            // Which row / column within the current tile are we rendering?
            int tileRow = wrappedBgY % 8;                
            int tileColumn = wrappedBgX % 8;

            // Affine BG's have one byte screen data (the tile index). Also all tiles are 8bpp
            int tileInfoOffset = (bgRow * bgWidthInTiles) + bgCol;
                
            int tileNumber = gba.Memory.VRam[(CntRegister.ScreenBlockBaseAddress * 2048) + tileInfoOffset];

            int tileVramOffset = (int)(tileDataVramOffset + (tileNumber * tileSize));

            int paletteIndex = TileHelpers.GetTilePixel(tileColumn, tileRow, eightBitColour, gba.Memory.VRam, tileVramOffset, false, false);

            if (paletteIndex == 0) return 0;

            return paletteOffset + paletteIndex;
        }


        // Used for debug rendering BG's. Renders the source BG, does not scroll etc 
        public void RenderMode0Scanline(int scanline, int scanlineWidth, DirectBitmap drawBuffer)
        {
            Color[] palette = gba.LcdController.Palettes.Palette0;
            int paletteOffset = 0;

            bool eightBitColour = CntRegister.PaletteMode == BgPaletteMode.PaletteMode256x1;

            // Which line within the current tile are we rendering?
            int tileRow = scanline % 8;

            for (int x = 0; x < scanlineWidth; x ++)
            {
                // Which column within the current tile are we rendering?
                int tileColumn = x % 8;

                var tileMetaData = TileMap.TileMapItemFromBgXY(x, scanline);

                // If we are in 4 bpp mode the tilemap contains which 16 colour palette to use. 16 entries per palette
                if (eightBitColour == false)
                {
                    paletteOffset = tileMetaData.Palette * 16;
                }

                int tileSize = (eightBitColour ? LcdController.Tile_Size_8bit : LcdController.Tile_Size_4bit);

                // 4 bytes represent one row of pixel data for a single tile
                int tileVramOffset = (int)(tileDataVramOffset + ((tileMetaData.TileNumber) * tileSize));

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
