//#define ParallelizeScanline
//#define THREADED_SCANLINE

using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using System.Runtime.CompilerServices;

namespace Gba.Core
{
    public class LcdController : IDisposable
    {
        public const byte Screen_X_Resolution = 240;
        public const byte Screen_Y_Resolution = 160;

        public const int Tile_Size_4bit = 32;
        public const int Tile_Size_8bit = 64;

        // How many clock cycles for the various LCD states...
        public const UInt32 Pixel_Length = 4;               // Render one pixel
        public const UInt32 HDraw_Length = 960;             // 240 * 4
        public const UInt32 HBlank_Length = 272;            // 68 pixels * 4
        public const UInt32 ScanLine_Length = 1232;         // HDraw + HBlank
        public const UInt32 VDraw_Length = 197120;          // ScanLine_Length * 160
        public const UInt32 VBlank_Length = 83776;          // ScanLine_Length * 68
        public const UInt32 ScreenRefresh_Length = 280896;  // VDraw_Length + VBlank_Length

        // When do we next need to be updated?
        public UInt32 NextUpdateCycle { get; private set; }
        UInt32 lastUpdatedCycle = 0;

        // IO Registers (driven by the memory controller))
        public DisplayControlRegister DisplayControlRegister { get; private set; }
        public DisplayStatusRegister DispStatRegister { get; private set; }
        public BgControlRegister[] BgControlRegisters { get; private set; }
        public BlendControlRegister BlendControlRegister { get; private set; }
        public PixelCoefficientRegister BlendingCoefficientRegister { get; private set; }
        public PixelCoefficientRegister BrightnessCoefficientRegister { get; private set; }


        public Background[] Bg { get; private set; }

        public ObjController ObjController { get; private set; }

        // Win0, Win1, WinOut, WinObj
        public Window[] Windows { get; private set; }

        MemoryRegister8 VCount { get; set; }
        public byte CurrentScanline { get { return VCount.Value; } private set { VCount.Value = value; } }

        public enum LcdMode
        {
            ScanlineRendering = 0,
            HBlank,
            VBlank
        }
        public LcdMode Mode { get; private set; }

        public UInt32 LcdCycles { get; private set; }
        public UInt32 VblankScanlineCycles { get; private set; }
        public UInt32 FrameCycles { get; private set; }
        public bool HblankInVblank { get { return VblankScanlineCycles >= HDraw_Length; } }

        public Palettes Palettes { get; private set; }

        public DirectBitmap FrameBuffer { get; private set; }
        DirectBitmap drawBuffer;
        DirectBitmap frameBuffer0;
        DirectBitmap frameBuffer1;
        double lastFrameTime;


#if THREADED_SCANLINE
        int drawScanline;
        bool exitThread;
        System.Threading.Thread scanlineThread;
#endif 


        GameboyAdvance gba { get; set; }

        public LcdController(GameboyAdvance gba)
        {
            this.gba = gba;


            frameBuffer0 = new DirectBitmap(Screen_X_Resolution, Screen_Y_Resolution);
            frameBuffer1 = new DirectBitmap(Screen_X_Resolution, Screen_Y_Resolution);
            FrameBuffer = frameBuffer0;
            drawBuffer = frameBuffer1;

            this.Palettes = new Palettes();

            DisplayControlRegister = new DisplayControlRegister(gba, this);
            DispStatRegister = new DisplayStatusRegister(gba, this);
            
            BgControlRegisters = new BgControlRegister[4];
            BgControlRegisters[0] = new BgControlRegister(gba, this, 0, 0x4000008);
            BgControlRegisters[1] = new BgControlRegister(gba, this, 1, 0x400000A);
            BgControlRegisters[2] = new BgControlRegister(gba, this, 2, 0x400000C);
            BgControlRegisters[3] = new BgControlRegister(gba, this, 3, 0x400000E);

            Bg = new Background[4];
            UInt32 scrollBaseAddress = 0x4000010;
            for (int i = 0; i < 4; i++)
            {                
                Bg[i] = new Background(gba, i, BgControlRegisters[i], scrollBaseAddress, scrollBaseAddress + 2);

                scrollBaseAddress += 4;
            }

            VCount = new MemoryRegister8(gba.Memory, 0x04000006, true, false);

            this.ObjController = new ObjController(gba);

            Windows = new Window[4];
            Windows[0] = new Window(gba, 0x4000040, 0x4000048);
            Windows[1] = new Window(gba, 0x4000042, 0x4000049);
            Windows[2] = new Window(gba, 0, 0x400004A);
            Windows[3] = new Window(gba, 0, 0x400004B);

            this.BlendControlRegister = new BlendControlRegister(this, gba);
            BlendingCoefficientRegister = new PixelCoefficientRegister(this, gba, 0x4000052);
            BrightnessCoefficientRegister = new PixelCoefficientRegister(this, gba, 0x4000054);

#if THREADED_SCANLINE
            Interlocked.Exchange(ref drawScanline, 0);
            exitThread = false;
            scanlineThread = new Thread(new ThreadStart(ScanlineThread));
            scanlineThread.Priority = ThreadPriority.Highest;
            scanlineThread.Start();
#endif
        }


        public void Dispose()
        {
#if THREADED_SCANLINE
            exitThread = true;
            Thread.Sleep(200);
#endif

            for (int i = 0; i < 4; i++)
            {                
                Bg[i].Dispose();
            }
        }


        public void Reset()
        {
            Mode = LcdMode.ScanlineRendering;
            NextUpdateCycle = gba.Cpu.Cycles + HDraw_Length;
            LcdCycles = 0;
            FrameCycles = 0;
            VblankScanlineCycles = 0;
            CurrentScanline = 0;
        }

        //Queue<double> avr = new Queue<double>();
        // Step one cycle
        public void Step()
        {
            UInt32 cycles = gba.Cpu.Cycles - lastUpdatedCycle;
            lastUpdatedCycle = gba.Cpu.Cycles;

            // If we change these, change vblank cycle count too
            LcdCycles += cycles;
            FrameCycles += cycles;

            switch (Mode)
            {
                case LcdMode.ScanlineRendering:
                    if (LcdCycles >= HDraw_Length)
                    {
#if THREADED_SCANLINE
                        if (scanlineThread.IsAlive == false)
                        {
                            throw new ArgumentException("Thread pop!");
                        }

                        // Wait for scanline rendering to finsih
                        while (Thread.VolatileRead(ref drawScanline) == 1) 
                        {
                        }
#endif
                        LcdCycles -= HDraw_Length;
                        Mode = LcdMode.HBlank;
                        NextUpdateCycle = gba.Cpu.Cycles + HBlank_Length;

                        if (DispStatRegister.HBlankIrqEnabled)
                        {
                            gba.Interrupts.RequestInterrupt(Interrupts.InterruptType.HBlank);
                        }

                        // Start hblank DMA's (HDMA)
                        for(int i=0; i < 4; i++)
                        {
                            if(gba.Dma[i].DmaCnt.StartTiming == DmaControlRegister.DmaStartTiming.HBlank &&
                               gba.Dma[i].DmaCnt.ChannelEnabled == true)
                            {
                                gba.Dma[i].Started = true;
                            }
                        }
                    }
                    break;


                case LcdMode.HBlank:
                    if(LcdCycles >= HBlank_Length)
                    {
                        LcdCycles -= HBlank_Length;

                        CurrentScanline++;

                        if (DispStatRegister.VCounterIrqEnabled &&
                            CurrentScanline == gba.LcdController.DispStatRegister.VCountSetting)
                        {
                            gba.Interrupts.RequestInterrupt(Interrupts.InterruptType.VCounterMatch);
                        }


                        if (Bg[2].AffineMode)
                        {
                            Bg[2].AffineScrollXCached += Bg[2].AffineMatrix.Pb;
                            Bg[2].AffineScrollYCached += Bg[2].AffineMatrix.Pd;
                        }

                        if (Bg[3].AffineMode)
                        {
                            Bg[3].AffineScrollXCached += Bg[3].AffineMatrix.Pb;
                            Bg[3].AffineScrollYCached += Bg[3].AffineMatrix.Pd;
                        }
                                          

                        if (CurrentScanline == 160)
                        {
                            Mode = LcdMode.VBlank;
                            NextUpdateCycle = gba.Cpu.Cycles + HDraw_Length;
                            VblankScanlineCycles = 0;

                            if (DispStatRegister.VBlankIrqEnabled)
                            {
                                gba.Interrupts.RequestInterrupt(Interrupts.InterruptType.VBlank);
                            }

                            if (Bg[2].AffineMode)
                            {
                                Bg[2].AffineScrollXCached = Bg[2].AffineScrollX;
                                Bg[2].AffineScrollYCached = Bg[2].AffineScrollY;
                            }

                            if (Bg[3].AffineMode)
                            {
                                Bg[3].AffineScrollXCached = Bg[3].AffineScrollX;
                                Bg[3].AffineScrollYCached = Bg[3].AffineScrollY;
                            }
                                                       

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

                                // Clear the draw buffer to the background color 
                                //using (var graphics = Graphics.FromImage(drawBuffer.Bitmap))
                                //{
                                //    graphics.Clear(Palettes.Palette0[0]);
                                //}
                            }

                            // lock to 60fps - 1000 / 60.0
                            double fps60 = 16.6666666;
                           /*
                            double frameTime = gba.EmulatorTimer.Elapsed.TotalMilliseconds - lastFrameTime;
                            avr.Enqueue(frameTime);
                            if (avr.Count == 11)
                            {
                                avr.Dequeue();
                                double frameAverage = avr.Average();
                                gba.LogMessage(String.Format("frame Ms {0:N2}", frameAverage));
                            }
                           */
                            while (gba.EmulatorTimer.Elapsed.TotalMilliseconds - lastFrameTime < fps60)
                            {
                            }

                            lastFrameTime = gba.EmulatorTimer.Elapsed.TotalMilliseconds;
                            

                            if (gba.OnFrame != null)
                            {
                                gba.OnFrame();
                            }
                        }
                        else
                        {
                            Mode = LcdMode.ScanlineRendering;
                            NextUpdateCycle = gba.Cpu.Cycles + HDraw_Length;
                            Render();
                        }
                    }
                    break;


                case LcdMode.VBlank:

                    VblankScanlineCycles += cycles;
                    if (LcdCycles >= VBlank_Length)
                    {/*
                        // 160 + 68 lines per screen
                        if(CurrentScanline != 227 ||
                            FrameCycles != ScreenRefresh_Length)
                        {
                            throw new InvalidOperationException("LCD: Scanlines / cycles mismatch"); 
                        }
*/
                        LcdCycles -= VBlank_Length;
                        FrameCycles = 0;

                        CurrentScanline = 0;
                        Mode = LcdMode.ScanlineRendering;
                        NextUpdateCycle = gba.Cpu.Cycles + HDraw_Length;
                        Render();

                        if (DispStatRegister.VCounterIrqEnabled &&
                            CurrentScanline == gba.LcdController.DispStatRegister.VCountSetting)
                        {
                           gba.Interrupts.RequestInterrupt(Interrupts.InterruptType.VCounterMatch);
                        }

                    }
                    else
                    {
                        // HBlanks IRQ's still fire durng vblank
                        if(VblankScanlineCycles >= HDraw_Length)
                        {
                            NextUpdateCycle = gba.Cpu.Cycles + HBlank_Length;

                            if (DispStatRegister.HBlankIrqEnabled)
                            {
                                gba.Interrupts.RequestInterrupt(Interrupts.InterruptType.HBlank);
                            }
                        }

                        // We are within vblank
                        if(VblankScanlineCycles >= ScanLine_Length)
                        {
                            NextUpdateCycle = gba.Cpu.Cycles + HDraw_Length;

                            VblankScanlineCycles = 0;
                            CurrentScanline++;

                            if (DispStatRegister.VCounterIrqEnabled &&
                            CurrentScanline == gba.LcdController.DispStatRegister.VCountSetting)
                            {
                                gba.Interrupts.RequestInterrupt(Interrupts.InterruptType.VCounterMatch);
                            }
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
                case 0x1:
                case 0x2:
#if THREADED_SCANLINE
                    if(scanlineThread.IsAlive == false)
                    {
                        throw new ArgumentException("Thread pop");
                    }
                    if(Thread.VolatileRead(ref drawScanline) == 1)
                    {
                        throw new ArgumentException("Scanline already true???");
                    }
                    Interlocked.Exchange(ref drawScanline, 1);
#else
                    RenderScanlineTextMode();
#endif
                    break;               

                case 0x4:
                    // TODO: What about sprites here?
                    RenderMode4Scanline();
                    break;

                default:
                    throw new NotImplementedException("Unknown or unimplemented video mode");
            }
        }

/*
        private void ObjPrioritySort()
        {
            for (int i = 0; i < 4; i++)
            {
                priorityObjList[i].Clear();
            }

            foreach(var obj in Obj)
            {
                int sprY = obj.Attributes.YPosition;
                if (sprY > LcdController.Screen_Y_Resolution) sprY -= 255;

                int height = obj.Attributes.Dimensions.Height;
                if (obj.Attributes.RotationAndScaling && obj.Attributes.DoubleSize)
                {
                    height *= 2;
                }

                // Visible is only valid if rotation and scaling is not enabled 
                if ((obj.Attributes.RotationAndScaling == false && obj.Attributes.Visible == false) ||
                    CurrentScanline < sprY ||
                    CurrentScanline >= (sprY + height))
                {
                    continue;
                }

                obj.CacheRenderData();                

                priorityObjList[obj.Attributes.Priority].Add(obj);
            }
        }
*/

#if THREADED_SCANLINE
        void ScanlineThread()
        {
            while (exitThread == false)
            {
                if (Thread.VolatileRead(ref drawScanline) == 1)
                {
                    //lock (drawBuffer)
                    {
                        RenderScanlineTextMode();
                        Interlocked.Exchange(ref drawScanline, 0);
                    }
                }
            }
        }
#endif


        // DENNIS: the way I did threaded rendering was by caching the LCD IO registers, and updating them for the PPU whenever it started rendering the next scanline

        private void RenderScanlineTextMode()
        {
            /*
            if (DisplayControlRegister.BgVisible(0)) Bg[0].CacheScanline();
            if (DisplayControlRegister.BgVisible(1)) Bg[1].CacheScanline();
            if (DisplayControlRegister.BgVisible(2)) Bg[2].CacheScanline();
            if (DisplayControlRegister.BgVisible(3)) Bg[3].CacheScanline();
            */

            ObjController.ObjPrioritySort();
            int scanline = CurrentScanline;
            bool windowing = (DisplayControlRegister.DisplayWin0 || DisplayControlRegister.DisplayWin1 || DisplayControlRegister.DisplayWin1 || DisplayControlRegister.DisplayObjWin);


            //Bg[0].WaitForScanline();
            //Bg[1].WaitForScanline();
            //Bg[2].WaitForScanline();
            //Bg[3].WaitForScanline();

            // We render front to back. Once a pixel is drawn we stop going through the layers.
            // TODO: In order to do blending we may need to go through all the layers for each pixel
#if ParallelizeScanline
            var paritioner = Partitioner.Create(0, LcdController.Screen_X_Resolution, 80);
            var result = Parallel.ForEach(paritioner, (range) =>
            {
            for (int x = range.Item1; x < range.Item2; x++)
#else
            for (int x = 0; x < Screen_X_Resolution; x++)
#endif
            {
                int paletteIndex;
                bool pixelDrawn = false;

                // Windowing can disable obj's and bg's
                bool objVisibleOverride = false;

                int windowRegion = 0;

                int bgVisibleOverride = 0; // bitmask for bg visible (from window)
                if (windowing)
                {
                    windowRegion = TileHelpers.PixelWindowRegion(x, CurrentScanline, gba);

                    // 0 is outside of all windows
                    if (windowRegion == 0)
                    {
                        objVisibleOverride = Windows[(int) Window.WindowName.WindowOut].DisplayObjs;

                        bgVisibleOverride = Windows[(int)Window.WindowName.WindowOut].DisplayBg0 |
                                            Windows[(int)Window.WindowName.WindowOut].DisplayBg1 |
                                            Windows[(int)Window.WindowName.WindowOut].DisplayBg2 |
                                            Windows[(int)Window.WindowName.WindowOut].DisplayBg3;
                    }
                    // Window 0 takes priority over window 1. We don't need to check if the point is within both areas as it is done in the function call above
                    else if((windowRegion & (int)TileHelpers.WindowRegion.Window0) != 0)
                    {
                        objVisibleOverride = Windows[(int)Window.WindowName.Window0].DisplayObjs;

                        bgVisibleOverride = Windows[(int)Window.WindowName.Window0].DisplayBg0 |
                                            Windows[(int)Window.WindowName.Window0].DisplayBg1 |
                                            Windows[(int)Window.WindowName.Window0].DisplayBg2 |
                                            Windows[(int)Window.WindowName.Window0].DisplayBg3;

                    }
                    else if ((windowRegion & (int)TileHelpers.WindowRegion.Window1) != 0)
                    {
                        objVisibleOverride = Windows[(int)Window.WindowName.Window1].DisplayObjs;

                        bgVisibleOverride = Windows[(int)Window.WindowName.Window1].DisplayBg0 |
                                            Windows[(int)Window.WindowName.Window1].DisplayBg1 |
                                            Windows[(int)Window.WindowName.Window1].DisplayBg2 |
                                            Windows[(int)Window.WindowName.Window1].DisplayBg3;
                    }


                }
                else
                {
                    objVisibleOverride = true;
                    bgVisibleOverride = 0;
                }

                // Start at the top priority, if something draws to the pixel, we can early out and stop processing this pixel 
                // Highest priority (0) is drawn at the front. Lower priorities can be obscured 
                for (int priority = 0; priority < 4; priority++)
                {
                    // Sprite rendering
                    // If a sprite has the same priority as a bg, the sprite is drawn on top, therefore we check sprites first 
                    if (DisplayControlRegister.DisplayObj &&
                        (!windowing || (windowing && objVisibleOverride)))
                    {
                        pixelDrawn = ObjController.RenderSpritePixel(drawBuffer, x, scanline, priority, windowing, windowRegion, ref bgVisibleOverride);
                        if (pixelDrawn)
                        {
                            break;
                        }                        
                    }

                    // No Sprite occupied this pixel, move on to backgrounds

                    // Bg Rendering
                    // Find the backgrounds with this priority                        
                    // In case of same priority, BG0 has highest (drawn at front)
                    for (int bgSelect = 0; bgSelect < 4; bgSelect++)
                    {                      
                        if (Bg[bgSelect].CntRegister.Priority != priority ||
                            DisplayControlRegister.BgVisible(Bg[bgSelect].BgNumber) == false ||
                            (windowing && ((bgVisibleOverride & (1 << bgSelect)) == 0)))
                        {
                            continue;
                        }

                        /*
                        if(Bg[2].AffineMode && bgSelect == 1)
                        {
                            continue;
                        }
                        */

                        
                        if (Bg[bgSelect].AffineMode)
                        {
                            paletteIndex = Bg[bgSelect].PixelValueAffine(x, scanline);
                        }
                        else
                        {
                            paletteIndex = Bg[bgSelect].PixelValue(x, scanline);
                        }
                        
                        // TODO: If we need to blend then get the pixels we need to blend here

                        /*
                        lock (Bg[bgSelect].ScanlineData)
                        {
                            paletteIndex = Bg[bgSelect].ScanlineData[x];
                        }
                        */

                        // Pal 0 == Transparent 
                        if (paletteIndex == 0)
                        {
                            continue;
                        }

                        drawBuffer.SetPixel(x, scanline, Palettes.Palette0[paletteIndex]);
                        pixelDrawn = true;

                        // Once a pixel has been drawn, no need to check other BG's
                        break;
                    }                                        
                    if(pixelDrawn)
                    {
                        break;
                    }
                }

                // If nothing is drawn then default to backdrop colour
                if (pixelDrawn == false)
                {
                    drawBuffer.SetPixel(x, scanline, Palettes.Palette0[0]);
                }
            }
#if ParallelizeScanline
            }); // Parallel.For
            //while (result.IsCompleted == false) ;
#endif
        }


        // Mode 4 is another bitmap mode. It also has a 240×160 frame-buffer, but instead of 16bpp pixels it uses 8bpp pixels. 
        // These 8 bits are a palette index to the background palette located at 0500:0000 (palette 0)
        private void RenderMode4Scanline()
        {
            byte[] vram = gba.Memory.VRam;
            Color[] palette = Palettes.Palette0;            

            int y = CurrentScanline;
            bool pixelDrawn;
            bool windowing = false;
            bool objVisibleOverride = true;
            int windowRegion = 0;
            int bgVisibleOverride = 0;

            ObjController.ObjPrioritySort();

            for (int x = 0; x < Screen_X_Resolution; x++)
            {
                pixelDrawn = false;

                for (int priority = 0; priority < 4; priority++)
                {
                    // Sprite rendering
                    if (DisplayControlRegister.DisplayObj &&
                        (!windowing || (windowing && objVisibleOverride)))
                    {
                        pixelDrawn = ObjController.RenderSpritePixel(drawBuffer, x, y, priority, windowing, windowRegion, ref bgVisibleOverride);
                        if (pixelDrawn)
                        {
                            break;
                        }
                    }
                }

                if (pixelDrawn == false)
                {
                    // 2nd framebuffer is at 0xA000
                    int index = vram[((y * Screen_X_Resolution) + x)];
                    drawBuffer.SetPixel(x, y, palette[index]);
                }
            }
        }



    }
}
