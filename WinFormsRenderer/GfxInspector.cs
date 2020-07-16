using Gba.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WinFormsRenderer
{
    public partial class GfxInspector : Form
    {
        BufferedGraphicsContext gfxBufferedContext;
        BufferedGraphics gfxBuffer;

        DirectBitmap palette0Bmp;
        DirectBitmap palette1Bmp;

        DirectBitmap tiles0Bmp;
        DirectBitmap tiles1Bmp;

        DirectBitmap bgBmp;
        Rectangle bgRenderRect = new Rectangle(0, 0, 512, 512);

        GameboyAdvance gba;

        public GfxInspector(GameboyAdvance gba)
        {
            InitializeComponent();

            this.gba = gba;

            palette0Bmp = new DirectBitmap(512, 512);
            palette1Bmp = new DirectBitmap(512, 512);

            tiles0Bmp = new DirectBitmap(256, 256);
            tiles1Bmp = new DirectBitmap(256, 256);

            bgBmp = new DirectBitmap(512, 512);
        }


        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);


            // Gets a reference to the current BufferedGraphicsContext
            gfxBufferedContext = BufferedGraphicsManager.Current;

            // Creates a BufferedGraphics instance associated with this form, and with dimensions the same size as the drawing surface of Form1.
            gfxBuffer = gfxBufferedContext.Allocate(this.tabControl.SelectedTab.CreateGraphics(), tabControl.SelectedTab.DisplayRectangle);

            gfxBuffer.Graphics.Clear(Color.White);
        }


        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            if (gfxBufferedContext != null)
            {
                gfxBuffer = gfxBufferedContext.Allocate(this.tabControl.SelectedTab.CreateGraphics(), tabControl.DisplayRectangle);
            }
        }


        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
        }


        public void RenderTab()
        {
            if(gfxBuffer == null)
            {
                return;
            }

            gfxBuffer.Graphics.Clear(Color.White);

            if(tabControl.SelectedIndex >= 2 && tabControl.SelectedIndex <=5)
            {
                using (var graphics = Graphics.FromImage(bgBmp.Bitmap))
                {
                    graphics.Clear(Color.Transparent);
                }
            }

            switch (tabControl.SelectedIndex)
            {
                case 0:
                    int vramBaseOffset = 0x00010000;
                    Color[] palette = gba.LcdController.Palettes.Palette1;

                    // You have to supply the code to get the tiles palette
                    Func<int, int> get4BitPaletteNumber = (int tileNumber) => {
                        Obj obj = TileHelpers.FindFirstSpriteThatUsesTile(tileNumber, gba.LcdController.Obj);
                        return (obj == null ? 0 : obj.Attributes.PaletteNumber * 16);
                    };
                    
                    gfxBuffer.Graphics.Clear(Color.Transparent);
                    using (var graphics = Graphics.FromImage(tiles0Bmp.Bitmap))
                    {
                        graphics.Clear(Color.Transparent);
                    }

                    gba.DrawTiles(tiles0Bmp, gba.Memory.VRam, vramBaseOffset, palette, false, get4BitPaletteNumber);

                    gfxBuffer.Graphics.DrawImage(tiles0Bmp.Bitmap, 0, 0, tiles0Bmp.Width * 2, tiles0Bmp.Height * 2);
                    break;

                case 1:
                    gba.LcdController.Palettes.DrawPalette(0, palette0Bmp.Bitmap);
                    gba.LcdController.Palettes.DrawPalette(1, palette1Bmp.Bitmap);
                    gfxBuffer.Graphics.DrawImage(palette0Bmp.Bitmap, 0, 0, palette0Bmp.Width, palette0Bmp.Height);
                    gfxBuffer.Graphics.DrawImage(palette1Bmp.Bitmap, 0, palette0Bmp.Height + 80, palette1Bmp.Width, palette1Bmp.Height);
                    break;

                case 2:                    
                    gba.LcdController.Bg[0].RenderFull(bgBmp);
                    GfxHelpers.DrawViewportBox(bgBmp.Bitmap, gba.LcdController.Bg[0].ScrollX, gba.LcdController.Bg[0].ScrollY, gba.LcdController.Bg[0].WidthInPixels(), gba.LcdController.Bg[0].HeightInPixels());
                    gfxBuffer.Graphics.DrawImage(bgBmp.Bitmap, bgRenderRect,  0, 0, gba.LcdController.Bg[0].WidthInPixels(), gba.LcdController.Bg[0].HeightInPixels(), GraphicsUnit.Pixel);
                    break;

                case 3:
                    gba.LcdController.Bg[1].RenderFull(bgBmp);
                    GfxHelpers.DrawViewportBox(bgBmp.Bitmap, gba.LcdController.Bg[1].ScrollX, gba.LcdController.Bg[1].ScrollY, gba.LcdController.Bg[1].WidthInPixels(), gba.LcdController.Bg[1].HeightInPixels());
                    gfxBuffer.Graphics.DrawImage(bgBmp.Bitmap, bgRenderRect, 0, 0, gba.LcdController.Bg[1].WidthInPixels(), gba.LcdController.Bg[1].HeightInPixels(), GraphicsUnit.Pixel);
                    break;

                case 4:
                    gba.LcdController.Bg[2].RenderFull(bgBmp);
                    GfxHelpers.DrawViewportBox(bgBmp.Bitmap, gba.LcdController.Bg[2].ScrollX, gba.LcdController.Bg[2].ScrollY, gba.LcdController.Bg[2].WidthInPixels(), gba.LcdController.Bg[2].HeightInPixels());
                    gfxBuffer.Graphics.DrawImage(bgBmp.Bitmap, bgRenderRect, 0, 0, gba.LcdController.Bg[2].WidthInPixels(), gba.LcdController.Bg[2].HeightInPixels(), GraphicsUnit.Pixel);
                    break;

                case 5:
                    gba.LcdController.Bg[3].RenderFull(bgBmp);
                    GfxHelpers.DrawViewportBox(bgBmp.Bitmap, gba.LcdController.Bg[3].ScrollX, gba.LcdController.Bg[3].ScrollY, gba.LcdController.Bg[3].WidthInPixels(), gba.LcdController.Bg[3].HeightInPixels());
                    gfxBuffer.Graphics.DrawImage(bgBmp.Bitmap, bgRenderRect, 0, 0, gba.LcdController.Bg[3].WidthInPixels(), gba.LcdController.Bg[3].HeightInPixels(), GraphicsUnit.Pixel);
                    break;
            }

            gfxBuffer.Render();
        }

        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (gfxBufferedContext != null)
            {
                gfxBuffer = gfxBufferedContext.Allocate(this.tabControl.SelectedTab.CreateGraphics(), tabControl.SelectedTab.DisplayRectangle);
            }
        }
    }
}
