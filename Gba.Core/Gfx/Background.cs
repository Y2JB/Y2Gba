//#define THREADED_SCANLINE

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gba.Core
{
    public class Background : IDisposable
    {
        public BgControlRegister CntRegister { get; }

        public TileMap TileMap { get; private set; }
        public BgSize Size { get { return CntRegister.Size; } }

        // Bg Horizontal / Vertical Offset
        MemoryRegister16 BGXHOFS;
        MemoryRegister16 BGXVOFS;

        public int ScrollX { get { return BGXHOFS.Value; } }
        public int ScrollY { get { return BGXVOFS.Value; } }

        public bool AffineMode { get; set; }
       

        public AffineScrollRegister AffineScrollXReg { get; private set; }
        public AffineScrollRegister AffineScrollYReg { get; private set; }

        public int AffineScrollX { get { return (int)AffineScrollXReg.Value; } }
        public int AffineScrollY { get { return (int)AffineScrollYReg.Value; } }
        public int AffineScrollXCached { get { return AffineScrollXReg.CachedValue; } set { AffineScrollXReg.CachedValue = value; } }
        public int AffineScrollYCached { get { return AffineScrollYReg.CachedValue; } set { AffineScrollYReg.CachedValue = value; } }



        public BgAffineMatrix AffineMatrix { get; private set; }

        UInt32 tileDataVramOffset;

        // Cached data for rendering
        int bgWidthInPixel;
        int bgHeightInPixel;
        int bgWidthInTiles;
        int bgHeightInTiles;
        bool eightBitColour;
        int tileSize;

        public int[] ScanlineData { get; private set; }
#if THREADED_SCANLINE
        int cacheScanline;
        bool exitThread;
        System.Threading.Thread scanlineThread;
#endif

        public int BgNumber { get; private set; }

        GameboyAdvance gba;


        public Background(GameboyAdvance gba, int bgNumber, BgControlRegister cntRegister, UInt32 scrollXRegAddr, UInt32 scrollYRegAddr)
        {
            this.gba = gba;
            this.BgNumber = bgNumber;
            CntRegister = cntRegister;
            AffineMode = false;
            TileMap = new TileMap(gba.Memory.VRam, cntRegister, bgNumber);

            BGXHOFS = new MemoryRegister16(gba.Memory, scrollXRegAddr, false, true, 0x01);
            BGXVOFS = new MemoryRegister16(gba.Memory, scrollYRegAddr, false, true, 0x01);

            // Only bg 2 & 3 can rotate and scale 
            if (bgNumber == 2)
            {
                AffineMatrix = new BgAffineMatrix(gba, 0x4000020);
                AffineScrollXReg = new AffineScrollRegister(gba.Memory, 0x4000028, false, true);
                AffineScrollYReg = new AffineScrollRegister(gba.Memory, 0x400002C, false, true);
            }
            else if (bgNumber == 3)
            {
                AffineMatrix = new BgAffineMatrix(gba, 0x4000030);
                AffineScrollXReg = new AffineScrollRegister(gba.Memory, 0x4000038, false, true);
                AffineScrollYReg = new AffineScrollRegister(gba.Memory, 0x400003C, false, true);
            }

            ScanlineData = new int[LcdController.Screen_X_Resolution];

            

#if THREADED_SCANLINE
            Interlocked.Exchange(ref cacheScanline, 0);
            exitThread = false;
            scanlineThread = new Thread(new ThreadStart(ScanlineThread));
            scanlineThread.Start();
#endif
        }


        public void Dispose()
        {
#if THREADED_SCANLINE
            exitThread = true;
            Thread.Sleep(200);
#endif 
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

            // Sometimes Bg's can be set up with invalid data which won't be drawn
            if(tileVramOffset >= gba.Memory.VRam.Length)
            {
                return 0;
            }

            int paletteIndex = TileHelpers.GetTilePixel(tileColumn, tileRow, eightBitColour, gba.Memory.VRam, tileVramOffset, tileMetaData.FlipHorizontal, tileMetaData.FlipVertical);

            if (paletteIndex == 0) return 0;

            return paletteOffset + paletteIndex;          
        }


        public int PixelValueAffine(int screenX, int screenY)
        {
            // Scrolling values set the origin so that BG 0,0 == Screen 0,0
            // Affine scroll are 24.8 fixed point numbers but as long as you shift away the fraction part at the end, you can just do integer math on them and they work
            int scrollX = AffineScrollXCached;// >> 8;
            int scrollY = AffineScrollYCached;// >> 8;

            // The game will have set up the matrix to be the inverse texture mapping matrix. I.E it maps from screen space to texture space. Just what we need!                    
            int textureSpaceX, textureSpaceY;

            textureSpaceX = ((scrollX + (AffineMatrix.Pa * screenX)) >> 8);  
            textureSpaceY = ((scrollY + (AffineMatrix.Pc * screenX)) >> 8);

            //AffineMatrix.Multiply(screenX, screenY, out textureSpaceX, out textureSpaceY);

            // Apply displacement vector (affine scroll) 
            // textureSpaceX += scrollX;
            // textureSpaceY += scrollY;

            // BG Wrap?
            if (CntRegister.DisplayAreaOverflow)                
            {
               /* 
                while (textureSpaceX >= bgWidthInPixel) textureSpaceX -= bgWidthInPixel;
                while (textureSpaceY >= bgHeightInPixel) textureSpaceY -= bgHeightInPixel;
                while (textureSpaceX < 0) textureSpaceX += bgWidthInPixel;
                while (textureSpaceY < 0) textureSpaceY += bgHeightInPixel;
                */
                textureSpaceX &= (bgWidthInPixel - 1);
                textureSpaceY &= (bgHeightInPixel - 1);
            }
            else
            {
                if (textureSpaceX < 0 || textureSpaceX >= bgWidthInPixel) return 0;
                if (textureSpaceY < 0 || textureSpaceY >= bgHeightInPixel) return 0;
            }
   

            // Coords (measured in tiles) of the tile we want to render 
            //int bgRow = textureSpaceY / 8;
            //int bgColumn = textureSpaceX / 8;

            // Which row / column within the tile we are rendering?
            int tileRow = textureSpaceY % 8;                
            int tileColumn = textureSpaceX % 8;

            // Affine BG's have one byte screen data (the tile index). Also all tiles are 8bpp
            // Affine BG's are also all square (they have their own size table which is different to regular tiled bg's)
            //int tileInfoOffset = (bgRow * bgWidthInTiles) + bgColumn;
            uint tileInfoOffset = ((CntRegister.ScreenBlockBaseAddress * 2048u) | (uint)((textureSpaceY >> 3) * ((uint)bgWidthInPixel >> 3)) | (uint)(textureSpaceX >> 3));
            int tileNumber = gba.Memory.VRam[tileInfoOffset];

            int tileVramOffset = (int)(tileDataVramOffset + (tileNumber * tileSize));

            // Sometimes Bg's can be set up with invalid data which won't be drawn
            if (tileVramOffset >= gba.Memory.VRam.Length)
            {
                return 0;
            }

            int paletteIndex = TileHelpers.GetTilePixel(tileColumn, tileRow, true, gba.Memory.VRam, tileVramOffset, false, false);
            return paletteIndex;
        }


        void CacheScanlineData()
        {
            int scanline = gba.LcdController.CurrentScanline;

            for (int x = 0; x < LcdController.Screen_X_Resolution; x++)
            {
                if (AffineMode)
                {
                    ScanlineData[x] = PixelValueAffine(x, scanline);
                }
                else
                {
                    ScanlineData[x] = PixelValue(x, scanline);
                }
            }
        }


#if THREADED_SCANLINE
        void ScanlineThread()
        {
            while (exitThread == false)
            {
                if (Thread.VolatileRead(ref cacheScanline) == 1)
                {
                    lock (ScanlineData)
                    {
                        CacheScanlineData();
                        Interlocked.Exchange(ref cacheScanline, 0);
                    }
                }
            }
        }


        public void WaitForScanline()
        {
            while (Thread.VolatileRead(ref cacheScanline) == 1)
            {
            }
        }


        public void CacheScanline()
        {            
            Interlocked.Exchange(ref cacheScanline, 1);
        }
#else
        public void CacheScanline()
        {            
            CacheScanlineData();
        }

        public void WaitForScanline()
        {
        }
#endif


            // Used for debug rendering BG's. Renders the source BG, does not scroll etc         
            public void DebugRenderScanlineAffine(int scanline, int scanlineWidth, DirectBitmap drawBuffer)
        {            
            Color[] palette = gba.LcdController.Palettes.Palette0;
            bool eightBitColour = CntRegister.PaletteMode == BgPaletteMode.PaletteMode256x1;

            // Which line within the current tile are we rendering?
            int tileRow = scanline % 8;

            for (int x = 0; x < scanlineWidth; x ++)
            {
                // Coords (measured in tiles) of the tile we want to render 
                int bgRow = scanline / 8;
                int bgColumn = x / 8;

                // Which row / column within the tile we are rendering?
                int tileColumn = x % 8;

                // Affine BG's have one byte screen data (the tile index). Also all tiles are 8bpp
                // Affine BG's are also all square (they have their own size table which is different to regular tiled bg's)
                int tileInfoOffset = (bgRow * bgWidthInTiles) + bgColumn;

                int tileNumber = gba.Memory.VRam[(CntRegister.ScreenBlockBaseAddress * 2048) + tileInfoOffset];

                int tileVramOffset = (int)(tileDataVramOffset + (tileNumber * tileSize));

                // Sometimes Bg's can be set up with invalid data which won't be drawn
                if (tileVramOffset >= gba.Memory.VRam.Length)
                {
                    continue;
                }

                int paletteIndex = TileHelpers.GetTilePixel(tileColumn, tileRow, true, gba.Memory.VRam, tileVramOffset, false, false);

                // Pal 0 == Transparent 
                if (paletteIndex == 0)
                {
                    continue;
                }

                drawBuffer.SetPixel(x, scanline, palette[paletteIndex]);
            }
        }



        // Used for debug rendering BG's. Renders the source BG, does not scroll etc 
        public void DebugRenderScanline(int scanline, int scanlineWidth, DirectBitmap drawBuffer)
        {
  
            Color[] palette = gba.LcdController.Palettes.Palette0;
            int paletteOffset = 0;

            bool eightBitColour = (CntRegister.PaletteMode == BgPaletteMode.PaletteMode256x1);

            // Which line within the current tile are we rendering?
            int tileRow = scanline % 8;

            for (int x = 0; x < scanlineWidth; x++)
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
               
                // Sometimes Bg's can be set up with invalid data which won't be drawn
                if (tileVramOffset >= gba.Memory.VRam.Length)
                {
                    continue;
                }

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
        public void DebugRenderFull(DirectBitmap drawBuffer)
        {
            for(int y = 0; y < HeightInPixels(); y++)
            {                
                if (AffineMode)
                {
                    DebugRenderScanlineAffine(y, WidthInPixels(), drawBuffer);
                }
                else
                {
                    DebugRenderScanline(y, WidthInPixels(), drawBuffer);
                }
            }
        }


        public int WidthInPixels()
        {
            if (AffineMode)
            {
                if (Size == BgSize.AffineBg128x128) return 128;
                else if (Size == BgSize.AffineBg256x256) return 256;
                else if (Size == BgSize.AffineBg512x512) return 512;
                else if (Size == BgSize.AffineBg1024x1024) return 1024;
                throw new ArgumentException("Bad bg size");
            }
            else
            {
                if (Size == BgSize.Bg256x256 || Size == BgSize.Bg256x512) return 256;
                return 512;
            }
        }


        public int HeightInPixels()
        {
            if (AffineMode)
            {
                if (Size == BgSize.AffineBg128x128) return 128;
                else if (Size == BgSize.AffineBg256x256) return 256;
                else if (Size == BgSize.AffineBg512x512) return 512;
                else if (Size == BgSize.AffineBg1024x1024) return 1024;
                throw new ArgumentException("Bad bg size");
            }
            else
            {
                if (Size == BgSize.Bg256x256 || Size == BgSize.Bg512x256) return 256;
                return 512;
            }
        }


        public void Dump(bool renderViewPortBox)
        {
            var image = new DirectBitmap(WidthInPixels(), HeightInPixels());
            DebugRenderFull(image);
            if (renderViewPortBox)
            {
                GfxHelpers.DrawViewportBox(image.Bitmap, ScrollX, ScrollY, WidthInPixels(), HeightInPixels());
            }
            image.Bitmap.Save(string.Format("../../../../dump/Bg{0}.png", BgNumber));
        }


    }
}
