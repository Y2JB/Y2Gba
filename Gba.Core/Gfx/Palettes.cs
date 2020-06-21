using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Gba.Core
{
    public class Palettes
    {
        public byte[] PaletteRam { get; set; }
        public Color[] Palette0 { get; set; }
        public Color[] Palette1 { get; set; }

        public Palettes()
        {
            PaletteRam = new byte[1024];
            Palette0 = new Color[256];
            Palette1 = new Color[256];

            for(int i =0; i < 256; i++)
            {
                Palette0[i] = Color.FromArgb(255, 0, 0, 0);
                Palette1[i] = Color.FromArgb(255, 0, 0, 0);
            }
        }

        public void SetPaletteEntry(int palette, int index, ushort colour)
        {
            if(palette == 0)
            {
                Palette0[index] = Colour555To888(colour);
            }
            else
            {
                Palette1[index] = Colour555To888(colour);
            }
        }

        public void UpdatePaletteByte(UInt32 index, byte b)
        {
            PaletteRam[index] = b;

            if((index % 2) == 1)
            {
                index--;
            }

            ushort colour = (ushort)(PaletteRam[index] | (PaletteRam[index + 1] << 8));

            // Align to 2 byte boundary
            index /= 2;

            if (index < 256)
            {           
                Palette0[index] = Colour555To888(colour);
            }
            else
            {
                Palette1[index - 256] = Colour555To888(colour);
            }
        }


        Color Colour555To888(ushort colour)
        {
            // High bit is unused, make sure it's zero
            colour &= 0x7FFF;
            byte b8 = (byte) (255 / 31 * (colour >> 10));
            byte g8 = (byte)(255 / 31 * ((colour & 0x3E0) >> 5));
            byte r8 = (byte)(255 / 31 * (colour & 0x1F));
            return Color.FromArgb(255, r8, g8, b8);
        }



        public void DumpPaletteToPng(int paletteNumber)
        {
            const int Rect_Size = 32;
            var image = new Bitmap(512, 512);

            Color[] palette = paletteNumber == 0 ? Palette0 : Palette1;

            using (var graphics = Graphics.FromImage(image))
            {
                for (int y = 0; y < 16; y++)
                {
                    for (int x = 0; x < 16; x++)
                    {
                        var brush = new SolidBrush(palette[(y * 16) + x]);
                        var rect = new Rectangle(x * Rect_Size, y * Rect_Size, Rect_Size, Rect_Size);
                        graphics.FillRectangle(brush, rect);
                    }
                }

                bool drawGrid = true;
                if (drawGrid)
                {
                    Pen pen = new Pen(Color.DarkRed, 0.5f);

                    for (int x = 0; x <= 16; x++)
                    {
                        graphics.DrawLine(pen, x * Rect_Size, 0, x * Rect_Size, 512);
                    }

                    for (int y = 0; y <= 16; y++)
                    {
                        graphics.DrawLine(pen, 0, y * Rect_Size, 512, y * Rect_Size);
                    }
                }
            }
            image.Save(string.Format("../../../../dump/palette{0}.png", paletteNumber));
        }
    }
}
