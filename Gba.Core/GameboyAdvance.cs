using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Threading;

namespace Gba.Core
{
    public class GameboyAdvance : IDisposable
    {
        public Bios Bios { get; private set; }
        public Rom Rom { get; private set; }
        public Cpu Cpu { get; private set; }
        public Interrupts Interrupts { get; private set; }
        public Timers Timers { get; private set; }
        public LcdController LcdController { get; private set; }
        public Memory Memory { get; private set; }
        public Joypad Joypad { get; private set; }
        public DmaChannel[] Dma { get; private set; }

        //long oneSecondTimer;
        public Stopwatch EmulatorTimer { get; private set; }

        // Renderer hooks
        public DirectBitmap FrameBuffer { get { return LcdController.FrameBuffer; } }
        public Action OnFrame { get; set; }

        // TTY output than can be passed on to the Emu container
        public Action<string> OnLogMessage { get; set; }

        public bool PoweredOn { get; private set; }


        public GameboyAdvance()
        {
            EmulatorTimer = new Stopwatch();
            PoweredOn = false;
        }


        public void Dispose()
        {
            PoweredOn = false;
            LcdController.Dispose();
        }


        public void PowerOn()
        {
            PoweredOn = true;

            this.Bios = new Bios(this, "../../../../GBA.BIOS");

            //this.Rom = new Rom("../../../../roms/TestRoms/armwrestler.gba");
            //this.Rom = new Rom("../../../../roms/TestRoms/suite.gba");
            //this.Rom = new Rom("../../../../roms/TestRoms/arm.gba");
            //this.Rom = new Rom("../../../../roms/TestRoms/hello.gba");
            //this.Rom = new Rom("../../../../roms/TestRoms/irq_demo.gba");
            //this.Rom = new Rom("../../../../roms/TestRoms/OrganHarvester/if_ack.gba");
            //this.Rom = new Rom("../../../../roms/TestRoms/tmr_demo.gba");
            //this.Rom = new Rom("../../../../roms/TestRoms/brin_demo.gba");
            //this.Rom = new Rom("../../../../roms/TestRoms/obj_demo.gba");
            //this.Rom = new Rom("../../../../roms/TestRoms/obj_aff.gba");
            //this.Rom = new Rom("../../../../roms/TestRoms/sbb_aff.gba");
            //this.Rom = new Rom("../../../../roms/TestRoms/win_demo.gba");
            //this.Rom = new Rom("../../../../roms/TestRoms/dma_demo.gba");
            //this.Rom = new Rom("../../../../roms/TestRoms/m3_demo.gba");
            //this.Rom = new Rom("../../../../roms/TestRoms/m7_demo.gba");

            //this.Rom = new Rom("../../../../roms/Super Dodgeball Advance.gba");
            //this.Rom = new Rom("../../../../roms/Kirby.gba");
            //this.Rom = new Rom("../../../../roms/Metal Slug Advance (U).gba");
            //this.Rom = new Rom("../../../../roms/Super Mario Advance 2 - Super Mario World (U) [!].gba");
            //this.Rom = new Rom("../../../../roms/Legend of Zelda, The - The Minish Cap (U).gba");
            //this.Rom = new Rom("../../../../roms/Legend of Zelda, The - A Link To The Past Four Swords.gba");
            //this.Rom = new Rom("../../../../roms/Pokemon Mystery Dungeon - Red Rescue Team (U).gba");
            //this.Rom = new Rom("../../../../roms/Teenage Mutant Ninja Turtles.gba");
            //this.Rom = new Rom("../../../../roms/Barbie Horse Adventures.gba");
            //this.Rom = new Rom("../../../../roms/Pokemon Pinball.gba");
            //this.Rom = new Rom("../../../../roms/Contra Advance - The Alien Wars Ex.gba");
            //this.Rom = new Rom("../../../../roms/Castlevania - Aria of Sorrow.GBA");
            //this.Rom = new Rom("../../../../roms/Castlevania - Harmony Of Dissonance.GBA");           
            //this.Rom = new Rom("../../../../roms/Baseball Advance.GBA");
            //this.Rom = new Rom("../../../../roms/Donkey Kong Country 3.gba");
            //this.Rom = new Rom("../../../../roms/Final Fantasy - Tactics Advanced.GBA");
            //this.Rom = new Rom("../../../../roms/Mario Golf - Advance Tour.gba");
            //this.Rom = new Rom("../../../../roms/Yoshi's Island - Super Mario Advance 3.gba");
            //this.Rom = new Rom("../../../../roms/Fire Emblem.gba");
            //this.Rom = new Rom("../../../../roms/Darius R.GBA");
            this.Rom = new Rom("../../../../roms/Mario Kart Super Circuit (U).gba");
            //this.Rom = new Rom("../../../../roms/F-Zero - Maximum Velocity.gba");
            //this.Rom = new Rom("../../../../roms/Konami Krazy Racers.gba");
            //this.Rom = new Rom("../../../../roms/Sega Rally Championship.gba");

            // Intro uses OBJ Win...
            //this.Rom = new Rom("../../../../roms/Pokemon - Emerald Version (U).gba");

            //this.Rom = new Rom("../../../../roms/Advance Wars.gba");
            //this.Rom = new Rom("../../../../roms/Advanced Wars 2 - Black Hole Rising.gba");


            //this.Rom = new Rom("../../../../roms/");


            this.Memory = new Memory(this);
            this.Cpu = new Cpu(this);
            this.Interrupts = new Interrupts(this);
            this.Timers = new Timers(this);
            this.LcdController = new LcdController(this);
            this.Joypad = new Joypad(this);
            this.Dma = new DmaChannel[4];
            for(int i=0; i < 4; i ++)
            {
                Dma[i] = new DmaChannel(this, i);
            }

            EmulatorTimer.Reset();
            EmulatorTimer.Start();

            Cpu.Reset();
            LcdController.Reset();
            Joypad.Reset();
        }


        public void Step()
        {
            // We lockstep everythng off of the CPU so we only update the cpu at this level
            Cpu.Step();

            // Expensive!
            //if (EmulatorTimer.ElapsedMilliseconds - oneSecondTimer >= 1000)
            //{
            //    oneSecondTimer = EmulatorTimer.ElapsedMilliseconds;
            //}
        }


        // TODO: The Conditinal Attribute should use a dedicated logging preprocessor directive 
        [Conditional("DEBUG")]
        //[Conditional("DEBUG_LOGGING")]
        public void LogMessage(string msg)
        {
            if(OnLogMessage != null)
            {
                OnLogMessage(msg);
            }
        }


        public void DumpObjTiles()
        {
            var image = new DirectBitmap(256, 256);

            // OBJ Tiles are stored in a separate area in VRAM: 06010000-06017FFF (32 KBytes) in BG Mode 0-2, or 06014000-06017FFF (16 KBytes) in BG Mode 3-5.
            // We dump the whole memory area in both 4 and 8 bit modes. Some will look wrong depending on Bg mode, colour depth etc
            int vramBaseOffset = 0x00010000;
            Color[] palette = LcdController.Palettes.Palette1;

            // You have to supply the code to get the tiles palette
            Func<int, int> get4BitPaletteNumber = (int tileNumber) =>   { 
                                                                            Obj obj = TileHelpers.FindFirstSpriteThatUsesTile(tileNumber, LcdController.ObjController.Obj);
                                                                            return (obj == null ? 0 : obj.Attributes.PaletteNumber * 16);
                                                                         };

            DrawTiles(image, Memory.VRam, vramBaseOffset, palette, true, get4BitPaletteNumber);
            image.Bitmap.Save(string.Format("../../../../dump/{0}Tiles{1}_{2}.png", "Obj", "8bpp", Rom.RomName));

            DrawTiles(image, Memory.VRam, vramBaseOffset, palette, false, get4BitPaletteNumber);
            image.Bitmap.Save(string.Format("../../../../dump/{0}Tiles{1}_{2}.png", "Obj", "4bpp", Rom.RomName));
        }


        public void DumpBgTiles()
        {
            var image = new DirectBitmap(256, 256);

            int vramBaseOffset0 = 0x0;
            int vramBaseOffset1 = 0x8000;
            Color[] palette = LcdController.Palettes.Palette0;

            // You have to supply the code to get the tiles palette
            Func<int, int> get4BitPaletteNumber = (int tileNumber) =>   {
                                                                            for (int i = 0; i < 4; i++)
                                                                            {
                                                                                int pal = TileHelpers.FindBgPaletteForTile(tileNumber, LcdController.Bg[i].TileMap);
                                                                                if(pal != 0)
                                                                                {
                                                                                    return pal * 16;
                                                                                }
                                                                            }
                                                                            return 0;
                                                                        };

            DrawTiles(image, Memory.VRam, vramBaseOffset0, palette, false, get4BitPaletteNumber);
            image.Bitmap.Save(string.Format("../../../../dump/{0}Tiles{1}_{2}.png", "BGV0", "4bpp", Rom.RomName));

            DrawTiles(image, Memory.VRam, vramBaseOffset1, palette, false, get4BitPaletteNumber);
            image.Bitmap.Save(string.Format("../../../../dump/{0}Tiles{1}_{2}.png", "BGV1", "4bpp", Rom.RomName));

            DrawTiles(image, Memory.VRam, vramBaseOffset0, palette, true, get4BitPaletteNumber);
            image.Bitmap.Save(string.Format("../../../../dump/{0}Tiles{1}_{2}.png", "BGV0", "8bpp", Rom.RomName));

            DrawTiles(image, Memory.VRam, vramBaseOffset1, palette, true, get4BitPaletteNumber);
            image.Bitmap.Save(string.Format("../../../../dump/{0}Tiles{1}_{2}.png", "BGV1", "8bpp", Rom.RomName));
        }


        // Code to dump both BG and Obj tiles. Quite a complex list of parameters in order to make it work for both
        public void DrawTiles(DirectBitmap image, byte[] vram, int vramBaseOffset, Color[] palette, bool eightBitColour, Func<int, int> getTile4BitPaletteNumber)
        {
            int tileCountX = 32;
            int tileCountY = 32;

            int tileX = 0;
            int tileY = 0;

            int tileSize = eightBitColour ? LcdController.Tile_Size_8bit: LcdController.Tile_Size_4bit;
  
            int totalTiles = eightBitColour ? 512 : 1024;

            for (int tileNumber=0; tileNumber < totalTiles; tileNumber++)
            {                              
                int tileVramOffset = vramBaseOffset + (tileNumber * tileSize);
              
                int paletteOffset = 0;
                if (eightBitColour == false && getTile4BitPaletteNumber != null)
                {
                    paletteOffset = getTile4BitPaletteNumber(tileNumber);
                }

                // Add one tiles pixels
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        int paletteIndex = TileHelpers.GetTilePixel(x, y, eightBitColour, vram, tileVramOffset, false, false);

                        // 0 == transparent / pal 0
                        int colIndex = (paletteIndex == 0 ? 0 : paletteOffset + paletteIndex);

                        image.SetPixel(x + (tileX * 8), y + (tileY * 8), palette[colIndex]);
                    }
                }

                // Coordinates on the output image
                tileX++;
                if (tileX == tileCountX)
                {
                    tileX = 0;
                    tileY++;
                }
            }

            bool drawGrid = true;
            if (drawGrid)
            {
                GfxHelpers.DrawTileGrid(image.Bitmap, tileCountX, tileCountY);
            }
        }


    }
}
