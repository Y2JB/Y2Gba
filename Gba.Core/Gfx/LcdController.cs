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

        public DirectBitmap FrameBuffer { get; private set; }
        DirectBitmap drawBuffer;
        DirectBitmap frameBuffer0;
        DirectBitmap frameBuffer1;
        double lastFrameTime;

        GameboyAdvance Gba { get; set; }

        public LcdController(GameboyAdvance gba)
        {
            Gba = gba;
            
        }


        public void Reset()
        {
            frameBuffer0 = new DirectBitmap(Screen_X_Resolution, Screen_Y_Resolution);
            frameBuffer1 = new DirectBitmap(Screen_X_Resolution, Screen_Y_Resolution);
            FrameBuffer = frameBuffer0;
            drawBuffer = frameBuffer1;

            this.Palettes = new Palettes();

            DisplayControlRegister = new DisplayControlRegister(this);
            DispStatRegister = new DisplayStatusRegister(this);
            BgControlRegisters = new BgControlRegister[4];
            for(int i = 0; i < 4; i++)
            {
                BgControlRegisters[i] = new BgControlRegister(this);
            }

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
                        Render();

                        LcdCycles -= HDraw_Length;
                        Mode = LcdMode.HBlank;

                        if (DispStatRegister.HBlankIrqEnabled)
                        {
                            Gba.Interrupts.RequestInterrupt(Interrupts.InterruptType.HBlank);
                        }
                    }
                    break;


                case LcdMode.HBlank:
                    if(LcdCycles >= HBlank_Length)
                    {
                        LcdCycles -= HBlank_Length;

                        CurrentScanline++;

                        if (DispStatRegister.VCounterIrqEnabled && 
                            CurrentScanline == Gba.LcdController.DispStatRegister.VCountSetting)
                        {
                            Gba.Interrupts.RequestInterrupt(Interrupts.InterruptType.VCounterMatch);
                        }
                        

                        if (CurrentScanline == 160)
                        {
                            Mode = LcdMode.VBlank;


                            if (DispStatRegister.VBlankIrqEnabled)
                            {
                                Gba.Interrupts.RequestInterrupt(Interrupts.InterruptType.VBlank);
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
                    RenderMode0Scanline();
                    break;

                case 0x4:
                    RenderMode4Scanline();
                    break;

                default:
                    throw new NotImplementedException("Unknown or unimplemented video mode");
            }
        }

        private void RenderMode0Scanline()
        {
            
            if (DisplayControlRegister.DisplayBg0)
            {
                BgControlRegister bgReg = BgControlRegisters[0];
                // 0-31, in units of 2 KBytes
                UInt32 mapDataOffset = (bgReg.ScreenBaseBlock * 2048);
                // 0-3, in units of 16 KBytes
                UInt32 tileDataOffset = (bgReg.CharacterBaseBlock * 16384);


                BgSize bgSize = bgReg.Size;                
                switch(bgSize)                
                {
                    case BgSize.Bg256x256:
                        break;


                    default:
                        throw new NotImplementedException();
                }

                BgPaletteMode paletteMode =  bgReg.PaletteMode;

                TileMap tileMap = new TileMap(Gba.Memory.VRam, mapDataOffset, bgSize);

                for(int i = 0; i < tileMap.TileCount; i++)
                {
                    TileMapEntry item = tileMap.TileMapItemFromIndex(i);
                }


                Color[] palette = Palettes.Palette0;


                const int tileSize4bit = 32;
                
                // Which line within the current tile are we rendering?
                int tileY = CurrentScanline % 8;

                for (int x = 0; x < Screen_X_Resolution; x += 8)
                {
                    TileMapEntry tileMetaData = tileMap.TileMapItemFromXY(x, CurrentScanline);

                    UInt32 tileVramOffset = (UInt32) (tileDataOffset + (tileMetaData.TileNumber * tileSize4bit) + (tileY * 4));

                    // 4 bytes per tile row in 4 bpp mode 
                    byte b0 = Gba.Memory.VRam[tileVramOffset];
                    byte b1 = Gba.Memory.VRam[tileVramOffset + 1];
                    byte b2 = Gba.Memory.VRam[tileVramOffset + 2];
                    byte b3 = Gba.Memory.VRam[tileVramOffset + 3];

                    int xpixel0 = b0 & 0x0F;
                    int xpixel1 = ((b0 & 0xF0) >> 4);
                    int xpixel2 = b1 & 0x0F;
                    int xpixel3 = ((b1 & 0xF0) >> 4);
                    int xpixel4 = b2 & 0x0F;
                    int xpixel5 = ((b2 & 0xF0) >> 4);
                    int xpixel6 = b3 & 0x0F;
                    int xpixel7 = ((b3 & 0xF0) >> 4);

                    Color pixel0 = palette[b0 & 0x0F];
                    Color pixel1 = palette[((b0 & 0xF0) >> 4)];
                    Color pixel2 = palette[b1 & 0x0F];
                    Color pixel3 = palette[((b1 & 0xF0) >> 4)];
                    Color pixel4 = palette[b2 & 0x0F];
                    Color pixel5 = palette[((b2 & 0xF0) >> 4)];
                    Color pixel6 = palette[b3 & 0x0F];
                    Color pixel7 = palette[((b3 & 0xF0) >> 4)];

                    drawBuffer.SetPixel(x, CurrentScanline, pixel0);
                    drawBuffer.SetPixel(x+1, CurrentScanline, pixel1);
                    drawBuffer.SetPixel(x+2, CurrentScanline, pixel2);
                    drawBuffer.SetPixel(x+3, CurrentScanline, pixel3);
                    drawBuffer.SetPixel(x+4, CurrentScanline, pixel4);
                    drawBuffer.SetPixel(x+5, CurrentScanline, pixel5);
                    drawBuffer.SetPixel(x+6, CurrentScanline, pixel6);
                    drawBuffer.SetPixel(x+7, CurrentScanline, pixel7);
                }

                /*
                
                for (int tileNum = 0; tileNum < 10; tileNum++)
                {
                    byte[] tile = new byte[32];
                    for (int i = 0; i < 32; i++)
                    {
                        tile[i] = Gba.Memory.VRam[tileDataOffset + i + tileNum];
                    }


  
                    var image = new Bitmap(8, 8);
                    for (int y = 0; y < 8; y++)
                    {
                        int pixel0 = ((tile[(y * 4) + 0] & 0x0F));
                        int pixel1 = ((tile[(y * 4) + 0] & 0xF0) >> 4);
                        int pixel2 = ((tile[(y * 4) + 1] & 0x0F));
                        int pixel3 = ((tile[(y * 4) + 1] & 0xF0) >> 4);
                        int pixel4 = ((tile[(y * 4) + 2] & 0x0F));
                        int pixel5 = ((tile[(y * 4) + 2] & 0xF0) >> 4);
                        int pixel6 = ((tile[(y * 4) + 3] & 0x0F));
                        int pixel7 = ((tile[(y * 4) + 3] & 0xF0) >> 4);
                        image.SetPixel(0, y, palette[pixel0]);
                        image.SetPixel(1, y, palette[pixel1]);
                        image.SetPixel(2, y, palette[pixel2]);
                        image.SetPixel(3, y, palette[pixel3]);
                        image.SetPixel(4, y, palette[pixel4]);
                        image.SetPixel(5, y, palette[pixel5]);
                        image.SetPixel(6, y, palette[pixel6]);
                        image.SetPixel(7, y, palette[pixel7]);
                
                    }
                
                    image.Save("../../../../dump/tile" + tileNum.ToString() + ".png");
                }
                */
            }
        }

        // Mode 4 is another bitmap mode. It also has a 240×160 frame-buffer, but instead of 16bpp pixels it uses 8bpp pixels. 
        // These 8 bits are a palette index to the background palette located at 0500:0000 (our palette 0)
        private void RenderMode4Scanline()
        {
            byte[] vram = Gba.Memory.VRam;
            Color[] palette = Palettes.Palette0;

            // 2nd framebuffer is at 0xA000

            int y = CurrentScanline;

            //for (int y = 0; y < Screen_Y_Resolution; y++)
            {
                for (int x = 0; x < Screen_X_Resolution; x++)
                {
                    int index = vram[((y * Screen_X_Resolution) + x)];
                    drawBuffer.SetPixel(x, y, palette[index]);
                }
            }
        }
    }
}
