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

        public UInt32 CurrentScanline { get; private set; }

        public enum LcdMode
        {
            ScanlineRendering = 0,
            HBlank,
            VBlank
        }
        public LcdMode Mode { get; private set; }

        UInt32 lcdCycles;

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

            Mode = LcdMode.ScanlineRendering;
            lcdCycles = 0;
            CurrentScanline = 0;
        }


        // Step one cycle
        public void Step()
        {
            lcdCycles++;

            switch (Mode)
            {
                case LcdMode.ScanlineRendering:
                    if (lcdCycles >= HDraw_Length)
                    {
                        lcdCycles -= HDraw_Length;
                        Mode = LcdMode.HBlank;
                    }
                    break;


                case LcdMode.HBlank:
                    if(lcdCycles >= HBlank_Length)
                    {
                        lcdCycles -= HBlank_Length;

                        CurrentScanline++;
                        if (CurrentScanline == 160)
                        {
                            Mode = LcdMode.VBlank;

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
                            while (Gba.EmulatorTimer.Elapsed.TotalMilliseconds - lastFrameTime < fps60)
                            { }

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

                    if(lcdCycles >= VBlank_Length)
                    {
                        // 160 + 68 lines per screen
                        if(CurrentScanline != 227)
                        {
                            throw new InvalidOperationException("LCD: Scanlines / cycles mismatch"); 
                        }

                        lcdCycles -= VBlank_Length;

                        CurrentScanline = 0;
                        Mode = LcdMode.ScanlineRendering;
                    }
                    else
                    {
                        // We are within vblank
                        if(lcdCycles % ScanLine_Length == 0)
                        {
                            CurrentScanline++;
                        }
                    }
                    break;
          
            }
        }

    }
}
