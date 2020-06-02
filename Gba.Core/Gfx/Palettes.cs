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
            Palette0 = new Color[0xFF];
            Palette1 = new Color[0xFF];
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

            // Align to 2 byte boundary
            UInt32 tileIndex = (index / 2);

            ushort colour = (ushort)(PaletteRam[tileIndex] | (PaletteRam[tileIndex + 1] << 8));

            if (index < 512)
            {           
                Palette0[tileIndex] = Colour555To888(colour);
            }
            else
            {
                Palette1[tileIndex - 512] = Colour555To888(colour);
            }
        }


        Color Colour555To888(ushort colour)
        {
            // High bit is unused, make sure it's zero
            colour &= 0x7FFF;
            byte r8 = (byte) (((((colour >> 10) & 0x10) * 527) + 23) >> 6);
            byte g8 = (byte) (((((colour >> 5) & 0x10) * 527) + 23) >> 6);
            byte b8 = (byte) ((((colour & 0x10) * 527) + 23) >> 6);

            return Color.FromArgb(255, r8, g8, b8);
        }



        public void DumpPaletteToPng(int paletteNumber)
        {
            const int Rect_Size = 32;
            var image = new Bitmap(256, 256);

            Color[] palette = paletteNumber == 0 ? Palette0 : Palette1;

            using (var graphics = Graphics.FromImage(image))
            {
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        var brush = new SolidBrush(palette[(y * 8) + x]);
                        var rect = new Rectangle(x * Rect_Size, y * Rect_Size, Rect_Size, Rect_Size);
                        graphics.FillRectangle(brush, rect);
                    }
                }

                bool drawGrid = true;
                if (drawGrid)
                {
                    Pen pen = new Pen(Color.DarkRed, 0.5f);

                    for (int x = 0; x < 8; x++)
                    {
                        graphics.DrawLine(pen, x * Rect_Size, 0, x * Rect_Size, 256);
                    }

                    for (int y = 0; y < 8; y++)
                    {
                        graphics.DrawLine(pen, 0, y * Rect_Size, 256, y * Rect_Size);
                    }
                }
            }
            image.Save(string.Format("../../../../dump/palette{0}.png", paletteNumber));
        }
    }
}
