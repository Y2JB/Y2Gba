using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Gba.Core
{
    public class ObjAttributes
    {
        GameboyAdvance gba;
        int oamRamOffset;
        byte[] oamRam;

        public ObjAttributes(GameboyAdvance gba, int oamRamOffset, byte[] oamRam)
        {
            this.gba = gba;
            this.oamRamOffset = oamRamOffset;
            this.oamRam = oamRam;
        }


        public enum ObjMode
        {
            Normal,
            SemiTransparent,
            ObjWindow,
            Prohibited
        }

        /*
        public enum ObjShape
        {
            Square,
            Horizontal,
            Vertical,
            Prohibited
        }
        */

        public enum PaletteDepth
        {
            Bpp4,
            Bpp8
        }

        // Shape and Size attributes used to lookup into this tab;e 
        readonly Size[] ObjSizes = {    new Size(8, 8), new Size(16, 16), new Size(32, 32), new Size(64, 64),
                                        new Size(16, 8), new Size(32, 8), new Size(32, 16), new Size(64, 32),
                                        new Size(8, 16), new Size(8, 32), new Size(16, 32), new Size(32, 64) };
        
        //  Bit   Expl.
        //  0-7   Y-Coordinate           (0-255)
        //  8     Rotation/Scaling Flag  (0=Off, 1=On)
        //  When Rotation/Scaling used (Attribute 0, bit 8 set):
        //  9     Double-Size Flag     (0=Normal, 1=Double)
        //  When Rotation/Scaling not used (Attribute 0, bit 8 cleared):
        //  9     OBJ Disable          (0=Normal, 1=Not displayed)
        //  10-11 OBJ Mode  (0=Normal, 1=Semi-Transparent, 2=OBJ Window, 3=Prohibited)
        //  12    OBJ Mosaic             (0=Off, 1=On)
        //  13    Colors/Palettes        (0=16/16, 1=256/1)
        //  14-15 OBJ Shape              (0=Square,1=Horizontal,2=Vertical,3=Prohibited)
        public byte ObjAttrib0L { get { return oamRam[oamRamOffset]; } }
        public byte ObjAttrib0H { get { return oamRam[oamRamOffset + 1]; } }


        // Bit   Expl.
        // 0-8   X-Coordinate           (0-511)
        //  When Rotation/Scaling used (Attribute 0, bit 8 set):
        // 9-13  Rotation/Scaling Parameter Selection (0-31)
        //          (Selects one of the 32 Rotation/Scaling Parameters that can be defined in OAM
        //  When Rotation/Scaling not used (Attribute 0, bit 8 cleared):
        // 9-11  Not used
        // 12    Horizontal Flip      (0=Normal, 1=Mirrored)
        // 13    Vertical Flip        (0=Normal, 1=Mirrored)
        // 14-15 OBJ Size               (0..3, depends on OBJ Shape, see Attr 0)
        public byte ObjAttrib1L { get { return oamRam[oamRamOffset + 2]; } }
        public byte ObjAttrib1H { get { return oamRam[oamRamOffset + 3]; } }


        //  Bit   Expl.
        //  0-9   Character Name          (0-1023=Tile Number)
        //  10-11 Priority relative to BG (0-3; 0=Highest)
        //  12-15 Palette Number   (0-15) (Not used in 256 color/1 palette mode)
        public byte ObjAttrib2L { get { return oamRam[oamRamOffset + 4]; } }
        public byte ObjAttrib2H { get { return oamRam[oamRamOffset + 5]; } }


        public int YPosition { get { return ObjAttrib0L; } }
        public int XPosition { get { return ObjAttrib1L + ((ObjAttrib1H & 1) * 256); } }


        public bool RotationAndScaling { get { return ((ObjAttrib0H & 0x01) != 0); } }

        //  When Rotation/Scaling used
        // Doubles the size of the sprites bounding box. Used to avoid clipping when doing a scale / rotate / sheer 
        public bool DoubleSize { get { return ((ObjAttrib0H & 0x02) != 0); } }
        //  When Rotation/Scaling NOT used
        public bool Visible { get { return ((ObjAttrib0H & 0x02) == 0); } }

        public ObjMode Mode { get { return (ObjMode)((ObjAttrib0H & 0x0C) >> 2); } }

        public bool Mosaic { get { return ((ObjAttrib0H & 0x10) != 0); } }

        public PaletteDepth PaletteMode { get { return (PaletteDepth)((ObjAttrib0H & 0x20) >> 4); } }

        int Shape { get { return (int)((ObjAttrib0H & 0xC0) >> 6); } }


        //  When Rotation/Scaling NOT used
        public bool HorizontalFlip { get { return ((ObjAttrib1H & 0x10) != 0); } }
        public bool VerticalFlip { get { return ((ObjAttrib1H & 0x20) != 0); } }

        int Size { get { return (int)((ObjAttrib1H & 0xC0) >> 6); } }

        public int TileNumber { get { return ObjAttrib2L + ((ObjAttrib2H & 0x03) << 8); } }
        
        public int Priority { get { return ((ObjAttrib2H & 0x0C) >> 2); } }

        // Not used in 256 mode
        public int PaletteNumber { get { return ((ObjAttrib2H & 0xF0) >> 4);  } }

        public Size Dimensions { get { return ObjSizes[(Shape * 4) + Size];  } }
      
        public OamAffineMatrix AffineMatrix()
        {
            if(RotationAndScaling == false)
            {
                throw new ArgumentException("No Matrix unless rot / scaling");
            }
            
            int matixindex = ((ObjAttrib1H & 0x3E)>>1);
            return gba.LcdController.OamAffineMatrices[matixindex];
        }

        // How many 8x8 tiles does this sprite use?
        public int TileCount()
        {
            return (Dimensions.Width / 8) * (Dimensions.Height / 8);
        }


        public int XPositionAdjusted()
        {
            // X value is 9 bit and Y is 8 bit! Clamp the values and wrap when they exceed them
            int sprX = XPosition;
            if (sprX >= LcdController.Screen_X_Resolution) sprX -= 512;
            return sprX;
        }

        public int YPositionAdjusted()
        {
            // X value is 9 bit and Y is 8 bit! Clamp the values and wrap when they exceed them
            int sprY = YPosition;
            if (sprY > LcdController.Screen_Y_Resolution) sprY -= 255;
            return sprY;
        }
    }
}
