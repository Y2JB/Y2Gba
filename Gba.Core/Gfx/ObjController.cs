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
    public class ObjController : IDisposable
    {
        public const byte Max_Sprites = 128;
        public const byte Max_OAM_Matrices = 32;

        public Obj[] Obj { get; private set; }
        public OamAffineMatrix[] OamAffineMatrices { get; private set; }

        // Every frame, put the objs in a bucket based on it's priority
        List<Obj>[] priorityObjList = new List<Obj>[4];

        GameboyAdvance gba { get; set; }


        public ObjController(GameboyAdvance gba)
        {
            this.gba = gba;

            Obj = new Obj[Max_Sprites];
            for (int i = 0; i < Max_Sprites; i++)
            {
                Obj[i] = new Obj(gba, new ObjAttributes(gba, i * 8, gba.Memory.OamRam));
            }

            OamAffineMatrices = new OamAffineMatrix[Max_OAM_Matrices];
            UInt32 address = 0x00000006;
            for (int i = 0; i < 32; i++)
            {
                OamAffineMatrices[i] = new OamAffineMatrix(gba.Memory.OamRam, address);

                address += 0x20;
            }

            for (int i = 0; i < 4; i++)
            {
                priorityObjList[i] = new List<Obj>();
            }
        }


        public void Dispose()
        {
        }


        public  void ObjPrioritySort()
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
                    gba.LcdController.CurrentScanline < sprY ||
                    gba.LcdController.CurrentScanline >= (sprY + height))
                {
                    continue;
                }

                obj.CacheRenderData();                

                priorityObjList[obj.Attributes.Priority].Add(obj);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RenderSpritePixel(DirectBitmap drawBuffer, int screenX, int screenY, int priority, bool windowing, int windowRegion, ref int bgVisibleOverride)
        {
            int paletteIndex;
        
            // If a sprite has the same priority as a bg, the sprite is drawn on top, therefore we check sprites first 
            foreach (var obj in priorityObjList[priority])
            {
                // Clip against the bounding box which can be DoubleSize. This is the only time doublesize is actually checked
                if (obj.BoundingBoxScreenSpace.ContainsPoint(screenX, screenY) == false)
                {
                    continue;
                }

                if (obj.Attributes.RotationAndScaling)
                {
                    int sourceWidth = obj.Attributes.Dimensions.Width;
                    int sourceHeight = obj.Attributes.Dimensions.Height;

                    // The game will have set up the matrix to be the inverse texture mapping matrix. I.E it maps from screen space to texture space. Just what we need!                    
                    OamAffineMatrix rotScaleMatrix = obj.Attributes.AffineMatrix();

                    // NB: Order of operations counts here!
                    // Transform with the origin set to the centre of the sprite (that's what the - width/height /2 below is for)
                    int originX = screenX - obj.Attributes.XPosition - (sourceWidth / 2);
                    int originY = screenY - obj.Attributes.YPosition - (sourceHeight / 2);
                    // Not well documented anywhere but when double size is enabled we render offset by half the original source width / height
                    if(obj.Attributes.DoubleSize)
                    {
                        originX -= sourceWidth / 2;
                        originY -= sourceHeight / 2;
                    }

                    int transformedX, transformedY;
                    rotScaleMatrix.Multiply(originX , originY, out transformedX, out transformedY);

                    // Transform back from centre of sprite
                    transformedX += (sourceWidth / 2);
                    transformedY += (sourceHeight / 2);

                    paletteIndex = obj.PixelValue(transformedX, transformedY);
                }
                else
                {
                    //paletteIndex = obj.PixelScreenValue(x, scanline);
                    paletteIndex = obj.PixelValue(screenX - obj.Attributes.XPositionAdjusted(), screenY - obj.Attributes.YPositionAdjusted());
                }

                // Pal 0 == Transparent 
                if (paletteIndex == 0)
                {
                    continue;
                }


                // TODO: I *think* this will render the Obj window correctly but i cannot test it yet
                // This pixel belongs to a sprite in the Obj Window and Win 0 & 1 are not enclosing this pixel
                if (windowing &&
                    gba.LcdController.DisplayControlRegister.DisplayObjWin &&
                    obj.Attributes.Mode == ObjAttributes.ObjMode.ObjWindow &&
                    ((windowRegion & (int)TileHelpers.WindowRegion.WindowIn) == 0))
                {
                    bgVisibleOverride = gba.LcdController.Windows[(int)Window.WindowName.WindowObj].DisplayBg0 |
                                        gba.LcdController.Windows[(int)Window.WindowName.WindowObj].DisplayBg1 |
                                        gba.LcdController.Windows[(int)Window.WindowName.WindowObj].DisplayBg2 |
                                        gba.LcdController.Windows[(int)Window.WindowName.WindowObj].DisplayBg3;

                    return false;
                }

                drawBuffer.SetPixel(screenX, screenY, gba.LcdController.Palettes.Palette1[paletteIndex]);
                return true;
                   
            }                        
            return false;
        }


    }
}
