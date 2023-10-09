using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using _3DViewer.Core;
using static System.Windows.Media.PixelFormats;

namespace _3DViewer.View
{
    public unsafe class Pbgra32Bitmap
    {
        private byte* BackBuffer { get; set; }
        private IntPtr BackBufferPtr { get; set; }
        private int BackBufferStride { get; set; }
        private int BytesPerPixel { get; set; }

        public int PixelWidth { get; private set; }
        public int PixelHeight { get; private set; }
        public WriteableBitmap Source { get; private set; }

        public Pbgra32Bitmap(int pixelWidth, int pixelHeight)
        {
            Source = new(pixelWidth, pixelHeight, 96, 96, Pbgra32, null);
            InitializeProperties();
        }

        public Pbgra32Bitmap(BitmapSource source)
        {
            Source = new(source.Format != Pbgra32 ? new FormatConvertedBitmap(source, Pbgra32, null, 0) : source);
            InitializeProperties();
        }

        private void InitializeProperties()
        {
            PixelWidth = Source.PixelWidth;
            PixelHeight = Source.PixelHeight;
            BackBuffer = (byte*)Source.BackBuffer;
            BackBufferPtr = Source.BackBuffer;
            BackBufferStride = Source.BackBufferStride;
            BytesPerPixel = Source.Format.BitsPerPixel / 8;
        }

        private byte* GetPixelAddress(int x, int y)
        {
            return BackBuffer + y * BackBufferStride + x * BytesPerPixel;
        }

        public Color GetPixel(int x, int y)
        {
            byte* pixel = GetPixelAddress(x, y);
            byte b = pixel[0];
            byte g = pixel[1];
            byte r = pixel[2];
            byte alpha = pixel[3];
            return new(alpha, r, g, b);
        }

        public void SetPixel(int x, int y,  Color color)
        {
            byte* pixel = GetPixelAddress(x, y);
            pixel[0] = color.Blue;
            pixel[1] = color.Green;
            pixel[2] = color.Red;
            pixel[3] = color.Alpha;
        }

        public void WriteArray(byte[] btm)
        {
            Marshal.Copy(btm, 0, BackBufferPtr, btm.Length);
           
            /*
            fixed (byte* ptr = btm)
            {
                Buffer.MemoryCopy(ptr, BackBuffer, btm.Length, btm.Length);
            }*/
        }

        public void ClearPixel(int x, int y)
        {
            *(int*)GetPixelAddress(x, y) = 0;
        }
    }
}
