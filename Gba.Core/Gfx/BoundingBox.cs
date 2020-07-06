using System;
using System.Collections.Generic;
using System.Text;

namespace Gba.Core
{
    public class BoundingBox
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public bool ContainsPoint(int x, int y)
        {
            return (x >= X && y >= Y && x < (X + Width) && y < (Y + Height));
        }
    }
}
