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

        Bitmap palette0Bmp;
        Bitmap palette1Bmp;


        GameboyAdvance gba;

        public GfxInspector(GameboyAdvance gba)
        {
            InitializeComponent();

            this.gba = gba;

            palette0Bmp = new Bitmap(512, 512);
            palette1Bmp = new Bitmap(512, 512);
        }


        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);


            // Gets a reference to the current BufferedGraphicsContext
            gfxBufferedContext = BufferedGraphicsManager.Current;

            // Creates a BufferedGraphics instance associated with this form, and with dimensions the same size as the drawing surface of Form1.
            gfxBuffer = gfxBufferedContext.Allocate(this.CreateGraphics(), this.DisplayRectangle);
        }


        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            if (gfxBufferedContext != null)
            {
                gfxBuffer = gfxBufferedContext.Allocate(this.CreateGraphics(), this.DisplayRectangle);
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


        public void RenderPalettes()
        {
            gba.LcdController.Palettes.DrawPalette(0, palette0Bmp);
            gba.LcdController.Palettes.DrawPalette(1, palette1Bmp);

            gfxBuffer.Graphics.DrawImage(palette0Bmp, 0, 0, palette0Bmp.Width, palette0Bmp.Height);
            gfxBuffer.Graphics.DrawImage(palette1Bmp, 0, palette0Bmp.Height + 40, palette1Bmp.Width, palette1Bmp.Height);

            gfxBuffer.Render();
        }
    }
}
