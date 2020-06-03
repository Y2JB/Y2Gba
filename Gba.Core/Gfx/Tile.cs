using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace Gba.Core
{
    public class Tile
    {       
        public byte[,] renderTile { get; private set; }

        public UInt32 VRamAddress { get; private set; }

        public Tile(UInt32 vramAddress)
        {
            renderTile  = new byte[8, 8];
            VRamAddress = vramAddress;
        }
/*
        public void Parse(byte[] vram)
        {
            Parse(vram, VRamAddress - 0x8000);
        }

        public void Parse(byte[] vramTile, int offset)
        {
            // Gameboy tiles are 8x8 pixels wide and 2 bits per pixel. This means 2 bytes per row
            // The first bit of the first pixel of each row is stored in the msb of the vram byte 1
            // The second bit of the first pixel of each row is stored in the msb of the vram byte 2
            // This function converts the format to something we can actually draw. The renderTile data becomes an 8x8 array of palette indices which can be 0-3 for the 4 gb colours            
            int y = 0;
            for (int i = 0; i < 16; i += 2)
            {
                for (int x = 0; x < 8; x++)
                {
                    int bitIndex = 1 << (7 - x);

                    byte pixelValue = 0;
                    if ((vramTile[offset + i] & bitIndex) != 0) pixelValue += 1;
                    if ((vramTile[offset + i + 1] & bitIndex) != 0) pixelValue += 2;

                    renderTile[x, y] = pixelValue;
                 }

                y++;
            }
        }
*/
        /*
        public void DumptToImageFile(string fn)
        {
            Color[] palette = new Color[4] { Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), Color.FromArgb(0xFF, 0xC0, 0xC0, 0xC0), Color.FromArgb(0xFF, 0x60, 0x60, 0x60), Color.FromArgb(0xFF, 0x00, 0x00, 0x00) };

            var image = new Bitmap(8, 8);
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    image.SetPixel(x, y, palette[renderTile[x, y]]);
                }
            }

            image.Save(fn);
        }
        */
    }
}
