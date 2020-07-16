using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Gba.Core
{
    public static class GfxHelpers
    {


        public static void DrawTileGrid(Bitmap image, int tileCountX, int tileCountY)
        {
            bool drawGrid = true;
            if (drawGrid)
            {
                Pen blackPen = new Pen(Color.FromArgb(64, 0, 0, 0), 0.1f);
                using (var graphics = Graphics.FromImage(image))
                {
                    for (int x = 0; x < tileCountX; x++)
                    {
                        graphics.DrawLine(blackPen, x * 8, 0, x * 8, tileCountY * 8);
                    }

                    for (int y = 0; y < tileCountY; y++)
                    {
                        graphics.DrawLine(blackPen, 0, y * 8, tileCountX * 8, y * 8);
                    }
                }
            }
        }


        public static void DrawViewportBox(Bitmap image, int viewPortX, int viewPortY, int bgWidthInPixels, int bgHeightInPixels)
        {
            if (viewPortX >= bgWidthInPixels) viewPortX -= bgWidthInPixels;
            if (viewPortY >= bgHeightInPixels) viewPortY -= bgHeightInPixels;

            Pen pen = new Pen(Color.RoyalBlue, 1.0f);
            using (var graphics = Graphics.FromImage(image))
            {
                int x1 = viewPortX;
                int x2 = viewPortX + LcdController.Screen_X_Resolution;

                int y1 = viewPortY;
                int y2 = viewPortY + LcdController.Screen_Y_Resolution;

                // Each side can take 2 lines to draw if it wraps

                int adjustX2 = x2;
                if (x2 >= bgWidthInPixels) adjustX2 = x2 - bgWidthInPixels;

                int adjustY2 = y2;
                if (y2 >= bgHeightInPixels) adjustY2 = y2 - bgHeightInPixels;

                // Top of rect (can go off end of image)
                graphics.DrawLine(pen, x1, y1, x2, y1);
                if (x2 != adjustX2) graphics.DrawLine(pen, 0, y1, adjustX2, y1);

                // Bottom of rect
                graphics.DrawLine(pen, x1, adjustY2, x2, adjustY2);
                if (x2 != adjustX2) graphics.DrawLine(pen, 0, adjustY2, adjustX2, adjustY2);

                // Left of rect (can go off end of image)
                graphics.DrawLine(pen, x1, y1, x1, y2);
                if (y2 != adjustY2) graphics.DrawLine(pen, x1, 0, x1, adjustY2);

                // Right
                graphics.DrawLine(pen, adjustX2, y1, adjustX2, y2);
                if (y2 != adjustY2) graphics.DrawLine(pen, adjustX2, 0, adjustX2, adjustY2);
            }
            
        }

    }
}
