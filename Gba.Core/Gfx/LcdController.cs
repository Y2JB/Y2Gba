using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace Gba.Core
{
    public class LcdController
    {
        public const byte Screen_X_Resolution = 240;
        public const byte Screen_Y_Resolution = 160;

        // How many clock cycles for the various LCD states...
        public const UInt32 Pixel_Length = 4;               // Render one pixel
        public const UInt32 HDraw_Length = 940;             // 240 * 4
        public const UInt32 HBlank_Length = 272;            // 68 pixels * 4
        public const UInt32 ScanLine_Length = 1232;         // HDraw + HBlank
        public const UInt32 VDraw_Length = 197120;          // ScanLine_Length * 160
        public const UInt32 VBlank_Length = 83776;          // ScanLine_Length * 68
        public const UInt32 ScreenRefresh_Length = 280896;  // VDraw_Length + VBlank_Length


        // IO Registers (driven by the memory controller))
        public DisplayControlRegister DisplayControlRegister { get; private set; }
        public DisplayStatusRegister DispStatRegister { get; private set; }
        public BgControlRegister[] BgControlRegisters { get; private set; }
        

        public byte CurrentScanline { get; private set; }

        public enum LcdMode
        {
            ScanlineRendering = 0,
            HBlank,
            VBlank
        }
        public LcdMode Mode { get; private set; }

        public UInt32 LcdCycles { get; private set; }

        public Palettes Palettes { get; private set; }

        public Bitmap FrameBuffer { get; private set; }
        Bitmap drawBuffer;
        Bitmap frameBuffer0;
        Bitmap frameBuffer1;
        double lastFrameTime;

        GameboyAdvance Gba { get; set; }

        public LcdController(GameboyAdvance gba)
        {
            Gba = gba;
            
        }


        public void Reset()
        {
            frameBuffer0 = new Bitmap(Screen_X_Resolution, Screen_Y_Resolution);
            frameBuffer1 = new Bitmap(Screen_X_Resolution, Screen_Y_Resolution);
            FrameBuffer = frameBuffer0;
            drawBuffer = frameBuffer1;

            this.Palettes = new Palettes();

            DisplayControlRegister = new DisplayControlRegister(this);
            DispStatRegister = new DisplayStatusRegister(this);
            BgControlRegisters = new BgControlRegister[4];

            Mode = LcdMode.ScanlineRendering;
            LcdCycles = 0;
            CurrentScanline = 0;
        }


        // Step one cycle
        public void Step()
        {
            LcdCycles++;

            switch (Mode)
            {
                case LcdMode.ScanlineRendering:
                    if (LcdCycles >= HDraw_Length)
                    {                    
                        LcdCycles -= HDraw_Length;
                        Mode = LcdMode.HBlank;
                    }
                    break;


                case LcdMode.HBlank:
                    if(LcdCycles >= HBlank_Length)
                    {
                        LcdCycles -= HBlank_Length;

                        CurrentScanline++;
                        if (CurrentScanline == 160)
                        {
                            Mode = LcdMode.VBlank;

                            Render();

                            // We can set the renderer drawing the frame as soon as we enter vblank
                            lock (FrameBuffer)
                            {
                                // Flip frames 
                                if (FrameBuffer == frameBuffer0)
                                {
                                    FrameBuffer = frameBuffer1;
                                    drawBuffer = frameBuffer0;
                                }
                                else
                                {
                                    FrameBuffer = frameBuffer0;
                                    drawBuffer = frameBuffer1;
                                }
                            }

                            // lock to 60fps - 1000 / 60.0
                            double fps60 = 16.6666666;
                            //double frameTime = Gba.EmulatorTimer.Elapsed.TotalMilliseconds - lastFrameTime;
                            while (Gba.EmulatorTimer.Elapsed.TotalMilliseconds - lastFrameTime < fps60)
                            {
                            }

                            lastFrameTime = Gba.EmulatorTimer.Elapsed.TotalMilliseconds;

                            if (Gba.OnFrame != null)
                            {
                                Gba.OnFrame();
                            }
                        }
                        else
                        {
                            Mode = LcdMode.ScanlineRendering;
                        }
                    }
                    break;


                case LcdMode.VBlank:

                    if(LcdCycles >= VBlank_Length)
                    {
                        // 160 + 68 lines per screen
                        if(CurrentScanline != 227)
                        {
                            throw new InvalidOperationException("LCD: Scanlines / cycles mismatch"); 
                        }

                        LcdCycles -= VBlank_Length;

                        CurrentScanline = 0;
                        Mode = LcdMode.ScanlineRendering;

                        //Palettes.DumpPaletteToPng(0);
                        //Palettes.DumpPaletteToPng(1);
                    }
                    else
                    {
                        // We are within vblank
                        if(LcdCycles % ScanLine_Length == 0)
                        {
                            CurrentScanline++;
                        }
                    }
                    break;
          
            }
        }

        public UInt32 TotalTicksForState()
        {
            switch (Mode)
            {
                case LcdMode.HBlank:
                    return HBlank_Length;
                case LcdMode.VBlank:
                    return VBlank_Length;
                case LcdMode.ScanlineRendering:
                    return ScanLine_Length;
            }

            throw new ArgumentException("bad mode");
        }


        private void Render()
        {
            switch(DisplayControlRegister.BgMode)
            {
                case 0x0:
                    break;

                case 0x4:
                    RenderMode4();
                    break;

                default:
                    throw new NotImplementedException("Unknown or unimplemented video mode");
            }
        }

        // Mode 4 is another bitmap mode. It also has a 240×160 frame-buffer, but instead of 16bpp pixels it uses 8bpp pixels. 
        // These 8 bits are a palette index to the background palette located at 0500:0000 (our palette 0)
        private void RenderMode4()
        {
            byte[] vram = Gba.Memory.VRam;
            Color[] palette = Palettes.Palette0;

            for (int y = 0; y < Screen_Y_Resolution; y++)
            {
                for (int x = 0; x < Screen_X_Resolution; x++)
                {
                    int index = vram[(y * Screen_X_Resolution) + x];
                    drawBuffer.SetPixel(x, y, palette[index]);
                }
            }
        }
    }
}
