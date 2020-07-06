using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text;

namespace Gba.Core
{
    public class Obj
    {
        public ObjAttributes Attributes { get; private set; }

        int vramBaseOffset;



        // Cache data for rendering 
        bool TileMapping2D;       
        int spriteWidthInTiles;
        int spriteHeightInTiles;
        bool eightBitColour;
        int tileSize;
        int spriteRowSizeInBytes;
        int paletteOffset;
        bool hFlip;
        bool vFlip;

        GameboyAdvance gba;

        public BoundingBox BoundingBoxScreenSpace { get; private set; }

        public Obj(GameboyAdvance gba, ObjAttributes attributes)
        {
            this.gba = gba;
            Attributes = attributes;

            BoundingBoxScreenSpace = new BoundingBox();

            // OBJ Tiles are stored in a separate area in VRAM: 06010000-06017FFF (32 KBytes) in BG Mode 0-2, or 06014000-06017FFF (16 KBytes) in BG Mode 3-5.
            vramBaseOffset = 0x00010000;
        }


        public void CacheRenderData()
        {
            // This should be all be cached (per scanline?)
            TileMapping2D = (gba.LcdController.DisplayControlRegister.ObjectCharacterVramMapping == 0);
            Size spriteDimensions = Attributes.Dimensions;
            spriteWidthInTiles = spriteDimensions.Width / 8;
            spriteHeightInTiles = spriteDimensions.Height / 8;
            eightBitColour = Attributes.PaletteMode == ObjAttributes.PaletteDepth.Bpp8;
            tileSize = (eightBitColour ? LcdController.Tile_Size_8bit : LcdController.Tile_Size_4bit);
            spriteRowSizeInBytes = tileSize * spriteWidthInTiles;
            paletteOffset = 0;
            if (eightBitColour == false)
            {
                paletteOffset = Attributes.PaletteNumber * 16;
            }
            hFlip = false;
            vFlip = false;
            if (Attributes.RotationAndScaling == false)
            {
                hFlip = Attributes.HorizontalFlip;
                vFlip = Attributes.VerticalFlip;
            }

            CalcBoundingBox();
        }

        // What is the pixel value within the sprite. passing 0,0 returns the pixel value of the top left of this sprite  
        public int PixelValue(int spriteX, int spriteY)
        {
            Size spriteDimensions = Attributes.Dimensions;

            if (spriteX < 0 || spriteX >= spriteDimensions.Width ||
                spriteY < 0 || spriteY >= spriteDimensions.Height)
            {
                return 0;
            }          

            int currentSpriteColumnInTiles = spriteX / 8;
            int currentSpriteRowInTiles    = spriteY / 8;

            int currentColumnWithinTile    = spriteX % 8;
            int currentRowWithinTile       = spriteY % 8;

            if (hFlip) currentSpriteColumnInTiles = (spriteWidthInTiles - 1) - currentSpriteColumnInTiles;
            if (vFlip) currentSpriteRowInTiles = (spriteHeightInTiles - 1) - currentSpriteRowInTiles;

            // This offset will be set to point to the start of the 8x8 that spriteX,spriteY is within
            int vramTileOffset;

            // Addressing mode (1d / 2d)
            if (TileMapping2D)
            {
                // 2D addressing, vram is thought of as a 32x32 matrix of tiles. A sprites tiles are arranged as you would view them on a screen
                int full32TileRowSizeInBytes = tileSize * 32;
                vramTileOffset = vramBaseOffset + (Attributes.TileNumber * tileSize) + (currentSpriteRowInTiles * full32TileRowSizeInBytes) + (currentSpriteColumnInTiles * tileSize);
            }
            else
            {
                // 1D addressing, all the sprites tiles are contiguous in vram
                vramTileOffset = vramBaseOffset + (Attributes.TileNumber * tileSize) + (currentSpriteRowInTiles * spriteRowSizeInBytes) + (currentSpriteColumnInTiles * tileSize);
            }

            // Lookup the actual pixel value (which is a palette index) in the tile data 
            int paletteIndex = TileHelpers.GetTilePixel(currentColumnWithinTile, currentRowWithinTile, eightBitColour, gba.Memory.VRam, vramTileOffset, hFlip, vFlip);

            // Pal 0 == Transparent 
            if (paletteIndex == 0)
            {
                return 0;
            }

            return paletteOffset + paletteIndex;
        }

        /*
                // What colour would this sprite like to set the passed screen coordinates?
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public int PixelScreenValue(int screenX, int screenY)
                {
                    Size spriteDimensions = Attributes.Dimensions;

                    // X value is 9 bit and Y is 8 bit! Clamp the values and wrap when they exceed them
                    int sprX = Attributes.XPosition;
                    if (sprX >= LcdController.Screen_X_Resolution) sprX -= 512;
                    int sprY = Attributes.YPosition;
                    if (sprY > LcdController.Screen_Y_Resolution) sprY -= 255;

                    if (screenX < sprX ||
                        screenX >= (sprX + spriteDimensions.Width))
                    {
                        return 0;
                    }

                    // This should be all be cached (per scanline?)
                    bool TileMapping2D = (gba.LcdController.DisplayControlRegister.ObjectCharacterVramMapping == 0);
                    byte[] vram = gba.Memory.VRam;
                    int spriteWidthInTiles = spriteDimensions.Width / 8;
                    int spriteHeightInTiles = spriteDimensions.Height / 8;
                    bool eightBitColour = Attributes.PaletteMode == ObjAttributes.PaletteDepth.Bpp8;
                    int tileSize = (eightBitColour ? LcdController.Tile_Size_8bit : LcdController.Tile_Size_4bit);
                    int spriteRowSizeInBytes = tileSize * spriteWidthInTiles;
                    int paletteOffset = 0;
                    if (eightBitColour == false)
                    {
                        paletteOffset = Attributes.PaletteNumber * 16;
                    }

                    // What x pixel within the sprite are we rendering??
                    int spriteX = screenX - sprX;       

                    // Which row of tiles are we rendering? EG: A 64x64 sprinte will have 8 rows of tiles 
                    int currentSpriteRowInTiles = (screenY - sprY) / 8;
                    if (Attributes.VerticalFlip) currentSpriteRowInTiles = (spriteHeightInTiles - 1) - currentSpriteRowInTiles;

                    int currentRowWithinTile = (screenY - sprY) % 8;        

                    int currentSpriteColumnInTiles = spriteX / 8;
                    if (Attributes.HorizontalFlip) currentSpriteColumnInTiles = (spriteWidthInTiles - 1) - currentSpriteColumnInTiles;

                    if(Attributes.Mode == ObjAttributes.ObjMode.SemiTransparent)
                    {
                        // Blending 
                        //throw new NotImplementedException();
                    }

                    int currentColumnWithinTile = spriteX % 8;

                    // This offset will be set to point to the start of the next 8x8 tile we will draw
                    int vramTileOffset;

                    // Addressing mode (1d / 2d)
                    if (TileMapping2D)
                    {
                        // 2D addressing, vram is thought of as a 32x32 matrix of tiles. A sprites tiles are arranged as you would view them on a screen

                        int full32TileRowSizeInBytes = tileSize * 32;

                        vramTileOffset = vramBaseOffset + (Attributes.TileNumber * tileSize) + (currentSpriteRowInTiles * full32TileRowSizeInBytes) + (currentSpriteColumnInTiles * tileSize);
                    }
                    else
                    {
                        // 1D addressing, all the sprites tiles are contiguous in vram
                        vramTileOffset = vramBaseOffset + (Attributes.TileNumber * tileSize) + (currentSpriteRowInTiles * spriteRowSizeInBytes) + (currentSpriteColumnInTiles * tileSize);
                    }

                    // Lookup the actual pixel value (which is a palette index) in the tile data 
                    int paletteIndex = TileHelpers.GetTilePixel(currentColumnWithinTile, currentRowWithinTile, eightBitColour, vram, vramTileOffset, Attributes.HorizontalFlip, Attributes.VerticalFlip);

                    // Pal 0 == Transparent 
                    if (paletteIndex == 0)
                    {
                        return 0;
                    }

                    return paletteOffset + paletteIndex;           
                }
        */

        /*
        public void RenderObjScanline(DirectBitmap drawBuffer, int scanline)
        {
            // OBJ Tiles are stored in a separate area in VRAM: 06010000-06017FFF (32 KBytes) in BG Mode 0-2, or 06014000-06017FFF (16 KBytes) in BG Mode 3-5.
            int vramBaseOffset = 0x00010000;

            bool TileMapping2D = (gba.LcdController.DisplayControlRegister.ObjectCharacterVramMapping == 0);

            byte[] vram = gba.Memory.VRam;

            Size spriteDimensions = Attributes.Dimensions;

            // X value is 9 bit and Y is 8 bit! Clamp the values and wrap when they exceed them
            int sprX = Attributes.XPosition;
            int sprY = Attributes.YPosition;
            if (sprY > LcdController.Screen_Y_Resolution) sprY -= 255;


            if (Attributes.Visible == false ||
                scanline < sprY ||
                scanline >= (sprY + spriteDimensions.Height))
            {
                return;
            }

            int spriteWidthInTiles = spriteDimensions.Width / 8;
            int spriteHeightInTiles = spriteDimensions.Height / 8;

            bool eightBitColour = Attributes.PaletteMode == ObjAttributes.PaletteDepth.Bpp8;
            int tileSize = (eightBitColour ? LcdController.Tile_Size_8bit : LcdController.Tile_Size_4bit);
            int spriteRowSizeInBytes = tileSize * spriteWidthInTiles;

            // Which row of tiles are we rendering? EG: A 64x64 sprinte will have 8 rows of tiles 
            int currentSpriteRowInTiles = (scanline - sprY) / 8;
            if (Attributes.VerticalFlip) currentSpriteRowInTiles = (spriteHeightInTiles - 1) - currentSpriteRowInTiles;

            int currentRowWithinTile = (scanline - sprY) % 8;

            int paletteOffset = 0;
            if (eightBitColour == false)
            {
                paletteOffset = Attributes.PaletteNumber * 16;
            }

            for (int spriteX = 0; spriteX < spriteDimensions.Width; spriteX++)
            {
                // Clamp to 9 bit range
                int screenX = Attributes.XPosition + spriteX;
                if (screenX >= 512) screenX -= 512;

                if (screenX < 0 || screenX >= LcdController.Screen_X_Resolution)
                {
                    continue;
                }

                int currentSpriteColumnInTiles = spriteX / 8;
                if (Attributes.HorizontalFlip) currentSpriteColumnInTiles = (spriteWidthInTiles - 1) - currentSpriteColumnInTiles;

                int currentColumnWithinTile = spriteX % 8;

                // This offset will be set to point to the start of the next 8x8 tile we will draw
                int vramTileOffset;

                // Addressing mode (1d / 2d)
                if (TileMapping2D)
                {
                    // 2D addressing, vram is thought of as a 32x32 matrix of tiles. A sprites tiles are arranged as you would view them on a screen

                    int full32TileRowSizeInBytes = tileSize * 32;

                    vramTileOffset = vramBaseOffset + (Attributes.TileNumber * tileSize) + (currentSpriteRowInTiles * full32TileRowSizeInBytes) + (currentSpriteColumnInTiles * tileSize);
                }
                else
                {
                    // 1D addressing, all the sprites tiles are contiguous in vram
                    vramTileOffset = vramBaseOffset + (Attributes.TileNumber * tileSize) + (currentSpriteRowInTiles * spriteRowSizeInBytes) + (currentSpriteColumnInTiles * tileSize);
                }

                // Lookup the actual pixel value (which is a palette index) in the tile data 
                int paletteIndex = TileHelpers.GetTilePixel(currentColumnWithinTile, currentRowWithinTile, eightBitColour, vram, vramTileOffset, Attributes.HorizontalFlip, Attributes.VerticalFlip);

                // Pal 0 == Transparent 
                if (paletteIndex == 0)
                {
                    continue;
                }

                drawBuffer.SetPixel(screenX, scanline, gba.LcdController.Palettes.Palette1[paletteOffset + paletteIndex]);
            }
        }
        */

        public void CalcBoundingBox()
        {
            BoundingBoxScreenSpace.X = Attributes.XPositionAdjusted();
            BoundingBoxScreenSpace.Y = Attributes.YPositionAdjusted();
            if(Attributes.RotationAndScaling && Attributes.DoubleSize)
            {
                BoundingBoxScreenSpace.Width = Attributes.Dimensions.Width * 2;
                BoundingBoxScreenSpace.Height = Attributes.Dimensions.Height * 2;
            }
            else
            {
                BoundingBoxScreenSpace.Width = Attributes.Dimensions.Width;
                BoundingBoxScreenSpace.Height = Attributes.Dimensions.Height;
            }
        }
    }
}
