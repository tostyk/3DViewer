using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3DViewer.Core
{
    public enum Colors
    {
        pink, 
        alien
    }
    public struct Color
    {
        private static Dictionary<Colors, Color> _colors = new Dictionary<Colors, Color>{

            {
                Colors.pink,
                new Color(0, 255, 50, 193)
            },
            {
                Colors.alien,
                new Color(0, 11, 156, 49)
            }
        };

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
        public Color(byte alpha, Colors colors)
        {
            Red = _colors[colors].Red;
            Green = _colors[colors].Green;
            Blue = _colors[colors].Blue;
            Alpha = alpha;
        }
    }
}
