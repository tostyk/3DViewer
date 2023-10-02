using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3DViewer.Core
{
    public struct Color
    {
        public byte Red;
        public byte Green;
        public byte Blue;
        public byte Alpha;
        public Color(byte alpha, byte red, byte green, byte blue)
        {
            Red = red;
            Green = green;
            Blue = blue;
            Alpha = alpha;
        }
    }
}
