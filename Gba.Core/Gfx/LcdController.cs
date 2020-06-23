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

        public const byte Max_Sprites = 128;

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


        // IO Registers (driven by the memory controller))
        public DisplayControlRegister DisplayControlRegister { get; private set; }
        public DisplayStatusRegister DispStatRegister { get; private set; }
        public BgControlRegister[] BgControlRegisters { get; private set; }
        
        public Background[] Bg { get; private set; }
        
        public ObjAttributes[] Obj { get; private set; }
        

        public byte CurrentScanline { get; private set; }

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
        public bool HblankInVblank { get { return VblankScanlineCycles > HDraw_Length; } }

        public Palettes Palettes { get; private set; }

        public DirectBitmap FrameBuffer { get; private set; }
        DirectBitmap drawBuffer;
        DirectBitmap frameBuffer0;
        DirectBitmap frameBuffer1;
        double lastFrameTime;

        GameboyAdvance gba { get; set; }

        public LcdController(GameboyAdvance gba)
        {
            this.gba = gba;
            
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
            Bg = new Background[4];
            for(int i = 0; i < 4; i++)
            {
                BgControlRegisters[i] = new BgControlRegister(this);
                Bg[i] = new Background(gba, i);
            }

            Obj = new ObjAttributes[Max_Sprites];
            for(int i = 0; i < Max_Sprites; i++)
            {
                Obj[i] = new ObjAttributes(i * 8, gba.Memory.OamRam);
            }

            Mode = LcdMode.ScanlineRendering;
            LcdCycles = 0;
            FrameCycles = 0;
            VblankScanlineCycles = 0;
            CurrentScanline = 0;
        }


        // Step one cycle
        public void Step()
        {

            // If we change these, change vblank cycle count too
            LcdCycles++;
            FrameCycles++;

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
                            gba.Interrupts.RequestInterrupt(Interrupts.InterruptType.HBlank);
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
                        

                        if (CurrentScanline == 160)
                        {
                            Mode = LcdMode.VBlank;

                            VblankScanlineCycles = 0;

                            if (DispStatRegister.VBlankIrqEnabled)
                            {
                                gba.Interrupts.RequestInterrupt(Interrupts.InterruptType.VBlank);
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
                                using (var graphics = Graphics.FromImage(drawBuffer.Bitmap))
                                {
                                    graphics.Clear(Palettes.Palette0[0]);
                                }
                            }

                            // lock to 60fps - 1000 / 60.0
                            double fps60 = 16.6666666;
                            //double frameTime = Gba.EmulatorTimer.Elapsed.TotalMilliseconds - lastFrameTime;
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
                        }
                    }
                    break;


                case LcdMode.VBlank:

                    VblankScanlineCycles++;
                    if (LcdCycles >= VBlank_Length)
                    {
                        // 160 + 68 lines per screen
                        if(CurrentScanline != 227 ||
                            FrameCycles != ScreenRefresh_Length)
                        {
                            throw new InvalidOperationException("LCD: Scanlines / cycles mismatch"); 
                        }

                        LcdCycles -= VBlank_Length;
                        FrameCycles = 0;

                        CurrentScanline = 0;
                        Mode = LcdMode.ScanlineRendering;

                        if (DispStatRegister.VCounterIrqEnabled &&
                            CurrentScanline == gba.LcdController.DispStatRegister.VCountSetting)
                        {
                           gba.Interrupts.RequestInterrupt(Interrupts.InterruptType.VCounterMatch);
                        }
                    }
                    else
                    {
                        // HBlanks IRQ's still fire durng vblank
                        if(VblankScanlineCycles == HDraw_Length)
                        {
                            if (DispStatRegister.HBlankIrqEnabled)
                            {
                                gba.Interrupts.RequestInterrupt(Interrupts.InterruptType.HBlank);
                            }
                        }

                        // We are within vblank
                        if(VblankScanlineCycles == ScanLine_Length)
                        {
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
                    RenderMode0Scanline();
                    break;

                case 0x4:
                    RenderMode4Scanline();
                    break;

                default:
                    throw new NotImplementedException("Unknown or unimplemented video mode");
            }

            if (DisplayControlRegister.DisplayObj)
            {
                RenderObjScanline();
            }
        }

        private void RenderMode0Scanline()
        {
            for(int priority = 3; priority >=0 ; priority--)
            {
                for(int i=0; i < 4; i++)
                {
                    if( DisplayControlRegister.DisplayBg(i) && 
                        Bg[i].CntRegister.Priority == priority)
                    {
                        // TODO: This needs doing when the reg's change 
                        Bg[i].Reset();

                        Bg[i].RenderMode0Scanline4bpp(CurrentScanline, drawBuffer);
                    }
                }
            }
        }


        // Mode 4 is another bitmap mode. It also has a 240×160 frame-buffer, but instead of 16bpp pixels it uses 8bpp pixels. 
        // These 8 bits are a palette index to the background palette located at 0500:0000 (our palette 0)
        private void RenderMode4Scanline()
        {
            byte[] vram = gba.Memory.VRam;
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


        private void RenderObjScanline()
        {
            // OBJ palette is always palette 1
            Color[] palette = Palettes.Palette1;

            // OBJ Tiles are stored in a separate area in VRAM: 06010000-06017FFF (32 KBytes) in BG Mode 0-2, or 06014000-06017FFF (16 KBytes) in BG Mode 3-5.
            int vramBaseOffset = 0x00010000;

            bool TileMapping2D = (DisplayControlRegister.ObjectCharacterVramMapping == 0);

            byte[] vram = gba.Memory.VRam;

            for (int i = 0; i < Max_Sprites; i++)
            {
                Size spriteDimensions = Obj[i].Dimensions;

                // X value is 9 bit and Y is 8 bit! Clamp the values and wrap when they exceed them
                int sprX = Obj[i].XPosition;
                int sprY = Obj[i].YPosition;
                if(sprY > Screen_Y_Resolution) sprY -= 255;

                if (Obj[i].Visible == false || 
                   CurrentScanline < sprY || 
                   CurrentScanline >= (sprY + spriteDimensions.Height))
                {
                    continue;
                }

                int spriteWidthInTiles = spriteDimensions.Width / 8;
                bool eightBitColour = Obj[i].PaletteMode == ObjAttributes.PaletteDepth.Bpp8;
                int tileSize = (eightBitColour ? LcdController.Tile_Size_8bit : LcdController.Tile_Size_4bit);
                int spriteRowSizeInBytes = tileSize * spriteWidthInTiles;

                // Which row of tiles are we rendering? EG: A 64x64 sprinte will have 8 rows of tiles 
                int currentSpriteRowInTiles = (CurrentScanline - sprY) / 8; 
                int currentRowWithinTile = (CurrentScanline - sprY) % 8;
                
                int paletteOffset = 0;
                if(eightBitColour)
                {
                    paletteOffset = Obj[i].PaletteNumber * 16;
                }

                //for (int screenX = Obj[i].XPosition; screenX < Obj[i].XPosition + spriteDimensions.Width; screenX++)
                for(int spriteX = 0; spriteX < spriteDimensions.Width; spriteX++)
                {
                    // Clamp to 9 bit range
                    int screenX = Obj[i].XPosition + spriteX;
                    if (screenX >= 512) screenX -= 512;

                    if (screenX < 0 || screenX >= Screen_X_Resolution)
                    {
                        continue;
                    }

                    int currentSpriteColumnInTiles = spriteX / 8;
                    int currentColumnWithinTile = spriteX % 8;

                    // This offset will be set to point to the start of the next 8x8 tile we will draw
                    int vramTileOffset;

                    // Addressing mode (1d / 2d)
                    if (TileMapping2D)
                    {
                        // 2D addressing, vram is thought of as a 32x32 matrix of tiles. A sprites tiles are arranged as you would view them on a screen

                        int full32TileRowSizeInBytes = tileSize * 32;

                        vramTileOffset = vramBaseOffset + (Obj[i].TileNumber * tileSize) + (currentSpriteRowInTiles * full32TileRowSizeInBytes) + (currentSpriteColumnInTiles * tileSize);
                    }
                    else
                    {
                        // 1D addressing, all the sprites tiles are contiguous in vram
                        vramTileOffset = vramBaseOffset + (Obj[i].TileNumber * tileSize) + (currentSpriteRowInTiles * spriteRowSizeInBytes) + (currentSpriteColumnInTiles * tileSize);
                    }

                    // TODO: Draw all 8 pixles here


                    // Lookup the actual pixel value (which is a palette index) in the tile data 
                    int paletteIndex = TileHelpers.GetTilePixel(currentColumnWithinTile, currentRowWithinTile, eightBitColour, vram, vramTileOffset);

                    // Pal 0 == Transparent 
                    if(paletteIndex == 0)
                    {
                        continue;
                    }

                    drawBuffer.SetPixel(screenX, CurrentScanline, palette[paletteOffset + paletteIndex]);
                }           
            }
        }

    }
}
