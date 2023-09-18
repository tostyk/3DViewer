using System.Numerics;
using System.Runtime.InteropServices;

namespace _3DViewer.Core
{
    public class BitmapGenerator
    {
        public const int RGBA = 4;
        private readonly ObjVertices _objVertices;
        private int scale = 250;
        private int xOffset;
        private int yOffset;

        private readonly byte[,,] _image;
        public BitmapGenerator(ObjVertices objVertices, int width, int height)
        {
            _objVertices = objVertices;
            _image = new byte[height, width, RGBA];
            xOffset = _image.GetLength(1) / 2 - 1;
            yOffset = _image.GetLength(0) / 2 - 1;
        }

        public byte[,,] GenerateImage()
        {

            for (int i = 0; i < _image.GetLength(0); i++)
            {
                for (int j = 0; j < _image.GetLength(1); j++)
                {
                    for (int k = 0; k < _image.GetLength(2); k++)
                    {
                        _image[i, j, k] = 0;
                    }
                }
            }

            foreach (var polygon in _objVertices.polygons)
            {
                for (int i = 0; i < polygon.Count; i++)
                {

                    /*
                     * 
                     * 
                     * WARNING!! PAY ATTENTION TO Y AND Z
                     * 
                     * 
                     */
                    DDALine(
                         _objVertices.vertices[polygon[i] - 1].X,
                         _objVertices.vertices[polygon[(i + 1) % polygon.Count] - 1].X,
                         _objVertices.vertices[polygon[i] - 1].Z,
                         _objVertices.vertices[polygon[(i + 1) % polygon.Count] - 1].Z
                         );
                }
            }


            DDALine( 0, 0, (yOffset - 1)/scale, -(yOffset - 1) / scale);
            DDALine((xOffset) / scale, - (xOffset - 1) / scale, 0, 0);

            return _image;
        }

        private void DDALine(float x0, float x1, float y0, float y1)
        {
            x0 *= scale;
            y0 *= -scale;
            x1 *= scale;
            y1 *= -scale;

            int xDiff = Math.Abs((int)(x1 - x0));
            int yDiff = Math.Abs((int)(y1 - y0));
            int L = xDiff > yDiff ? xDiff : yDiff;

            float dx = (x1 - x0) / L;
            float dy = (y1 - y0) / L;

            for (int i = 0; i < L; i++)
            {
                int X = Convert.ToInt32(x0 + dx*i + xOffset);
                int Y = Convert.ToInt32(y0 + dy*i + yOffset);

                _image[Y, X, 1] = 255;
                _image[Y, X, 2] = 255;
                _image[Y, X, 3] = 255;
                _image[Y, X, 0] = 255;
            }
        }
    }
}
