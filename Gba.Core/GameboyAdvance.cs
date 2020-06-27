using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Text;

namespace Gba.Core
{
    public class GameboyAdvance
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
            //this.Rom = new Rom("../../../../roms/TestRoms/win_demo.gba");
            //this.Rom = new Rom("../../../../roms/TestRoms/dma_demo.gba");

            //this.Rom = new Rom("../../../../roms/NCE-heart.gba");

            //this.Rom = new Rom("../../../../roms/Super Dodgeball Advance.gba");
            this.Rom = new Rom("../../../../roms/Kirby.gba");
            //this.Rom = new Rom("../../../../roms/Metal Slug Advance (U).gba");
            //this.Rom = new Rom("../../../../roms/Super Mario Advance 2 - Super Mario World (U) [!].gba");
            //this.Rom = new Rom("../../../../roms/Legend of Zelda, The - The Minish Cap (U).gba");
            //this.Rom = new Rom("../../../../roms/Pokemon Mystery Dungeon - Red Rescue Team (U).gba");
            //this.Rom = new Rom("../../../../roms/Teenage Mutant Ninja Turtles.gba");
            //this.Rom = new Rom("../../../../roms/Barbie Horse Adventures.gba");
            //this.Rom = new Rom("../../../../roms/Pokemon Pinball.gba");
            //this.Rom = new Rom("../../../../roms/Advance Wars.gba");
            //this.Rom = new Rom("../../../../roms/Advanced Wars 2 - Black Hole Rising.gba");

            this.Memory = new Memory(this);
            this.Cpu = new Cpu(this);
            this.Interrupts = new Interrupts(this);
            this.Timers = new Timers(this);
            this.LcdController = new LcdController(this);
            this.Joypad = new Joypad(this);
            this.Dma = new DmaChannel[4];
            for(int i=0; i < 4; i ++)
            {
                Dma[i] = new DmaChannel(this);
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
        //[Conditional("DEBUG")]
        public void LogMessage(string msg)
        {
            if(OnLogMessage != null)
            {
                OnLogMessage(msg);
            }
        }


        public void DumpObjTiles()
        {
            // OBJ Tiles are stored in a separate area in VRAM: 06010000-06017FFF (32 KBytes) in BG Mode 0-2, or 06014000-06017FFF (16 KBytes) in BG Mode 3-5.
            // We dump the whole memory area in both 4 and 8 bit modes. Some will look wrong depending on Bg mode, colour depth etc
            int vramBaseOffset = 0x00010000;
            Color[] palette = LcdController.Palettes.Palette1;

            // You have to supply the code to get the tiles palette
            Func<int, int> get4BitPaletteNumber = (int tileNumber) =>   { 
                                                                            Obj obj = TileHelpers.FindFirstSpriteThatUsesTile(tileNumber, LcdController.Obj);
                                                                            return (obj == null ? 0 : obj.Attributes.PaletteNumber * 16);
                                                                         };

            DumpTiles(Memory.VRam, vramBaseOffset, palette, true, "Obj", get4BitPaletteNumber);
            DumpTiles(Memory.VRam, vramBaseOffset, palette, false, "Obj", get4BitPaletteNumber);
        }


        public void DumpBgTiles()
        {
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

            DumpTiles(Memory.VRam, vramBaseOffset0, palette, false, "BGV0", get4BitPaletteNumber);
            DumpTiles(Memory.VRam, vramBaseOffset1, palette, false, "BGV1", get4BitPaletteNumber);
        }


        // Code to dump both BG and Obj tiles. Quite a complex list of parameters in order to make it work for both
        void DumpTiles(byte[] vram, int vramBaseOffset, Color[] palette, bool eightBitColour, string filenameMoniker, Func<int, int> getTile4BitPaletteNumber)
        {            
            int tileCountX = 32;
            int tileCountY = 32;
            var image = new Bitmap(tileCountX * 8, tileCountY * 8);

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
                GfxHelpers.DrawTileGrid(image, tileCountX, tileCountY);
            }

            string bpp = (eightBitColour ? "8bpp" : "4bpp");
            image.Save(string.Format("../../../../dump/{0}Tiles{1}_{2}.png", filenameMoniker, bpp, Rom.RomName));
        }


       

    }
}
