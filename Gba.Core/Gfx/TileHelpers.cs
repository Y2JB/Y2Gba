using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Gba.Core
{
    public static class TileHelpers
    {
        public static int GetTilePixel(int tileX, int tileY, bool eightBitColour, byte[] vram, int offsetToTile, bool flipHorizontally, bool flipVertically)
        {
            if (flipHorizontally) tileX = 7 - tileX;
            if (flipVertically) tileY = 7 - tileY;

            if (eightBitColour)
            {
                return vram[offsetToTile + (tileY * 8) + tileX];
            }
            else
            {
                // 2 pixels per byte, 4 bytes per tile row
                byte pixelByte = vram[offsetToTile + (tileY * 4) + (tileX / 2)];

                // Select the right nibble for the pixel, odd/even numbered columns have a different nibble
                if ((tileX & 0x1) == 0)
                {
                    return (pixelByte & 0x0F);
                }
                else
                {
                    return ((pixelByte & 0xF0) >> 4);
                }
            }
        }


        public static Obj FindFirstSpriteThatUsesTile(int tileNumber, Obj[] objs)
        {
            foreach (var obj in objs)
            {
                if (tileNumber >= obj.Attributes.TileNumber && tileNumber <= (obj.Attributes.TileNumber + obj.Attributes.TileCount()))
                {
                    return obj;
                }
            }

            return null;
        }


        // Used for dumping tiles to guess the right palette. Go through all the bg's and find a usage of the tile and get it's palette
        public static int FindBgPaletteForTile(int tileNumber, TileMap tileMap)
        {
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    var tileInfo = tileMap.TileMapItemFromBgXY(x * 8, y * 8);
                    if (tileInfo.TileNumber == tileNumber)
                    {
                        return tileInfo.Palette;
                    }
                }
            }
            return 0;
        }

    }
}
