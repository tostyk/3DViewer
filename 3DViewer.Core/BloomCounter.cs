using ImageBlur.Core;
using System;
using System.Collections.Concurrent;
using System.Numerics;
using static System.Net.Mime.MediaTypeNames;

namespace _3DViewer.Core
{
    public class BloomCounter
    {
        private static double[] coeffsY = { 0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216 };
        private static double[] coeffsX = { 0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216 };

        public static int extX = 20;
        public static int extY = 20;

        public static readonly Vector3 BloomBrightness = new Vector3(0.0722f, 0.7152f, 0.2126f);

        public static Vector3 CountBloom(Vector3 color)
        {
            float br = color.X * BloomBrightness.X + color.Y * BloomBrightness.Y + color.Z * BloomBrightness.Z;
            return SmoothStep(0, 1, br) * color;
        }

        public static Vector3 SmoothStep(Vector3 edge0, Vector3 edge1, Vector3 val)
        {
            Vector3 g = new Vector3(val.X, val.Y, val.Z);
            g.X = SmoothStep(edge0.X, edge1.X, val.X);
            g.Y = SmoothStep(edge0.Y, edge1.Y, val.Y);
            g.Z = SmoothStep(edge0.Z, edge1.Z, val.Z);
            return g;
        }

        public static float SmoothStep(float edge0, float edge1, float x)
        {
            float g = Math.Clamp((x - edge0)/(edge1 - edge0), edge0, edge1);
            return g * g * (3.0f - 2.0f * g);
        }

        public static byte[] BoxBlur(byte[] image, int width, int height, int extX, int extY)
        {
            byte[] result = new byte[image.Length];
            byte[] newImage = ExtendImage(image, width, height, extX, extY);
            int newWidth = width + 2 * extX;
            Parallel.ForEach(Partitioner.Create(0, width), range =>
            {
                for (int x = range.Item1; x < range.Item2; x++)
                {
                    int newX = x + extX;
                    int newY = extY + 0;

                    Vector4 res = GetPixelColor(newImage, newWidth, newX, newY);

                    for (int i = 1; i <= extY; i++)
                    {
                        res += GetPixelColor(newImage, newWidth, newX, newY + i);
                        res += GetPixelColor(newImage, newWidth, newX, newY - i);
                    }
                    res /= (extY * 2 + 1);
                    SetPixelColor(result, width, x, 0, res);

                    for (int y = 1; y < height; y++)
                    {
                        newX = x + extX;
                        newY = y + extY;

                        Vector4 oldC = GetPixelColor(newImage, newWidth, newX, (newY - 1) - extY) / ((extY * 2 + 1));
                        Vector4 newC = GetPixelColor(newImage, newWidth, newX, newY + extY) / ((extY * 2 + 1));

                        res += (-oldC + newC);

                        res.X = Math.Clamp(res.X, 0, 255f);
                        res.Y = Math.Clamp(res.Y, 0, 255f);
                        res.Z = Math.Clamp(res.Z, 0, 255f);
                        res.W = Math.Clamp(res.W, 0, 255f);

                        SetPixelColor(result, width, x, y, res);
                    }
                }
            });
            Parallel.ForEach(Partitioner.Create(0, height), range =>
            {
                for (int y = range.Item1; y < range.Item2; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int newX = x + extX;
                        int newY = y + extY;

                        SetPixelColor(newImage, newWidth, newX, newY, GetPixelColor(result, width, x, y));
                    }
                }
            });
            Parallel.ForEach(Partitioner.Create(0, height), range =>
            {
                for (int y = range.Item1; y < range.Item2; y++)
                {

                    int newX = 0 + extX;
                    int newY = extY + y;

                    Vector4 res = GetPixelColor(newImage, newWidth, newX, newY);

                    for (int i = 1; i <= extX; i++)
                    {
                        res += GetPixelColor(newImage, newWidth, newX + i, newY);
                        res += GetPixelColor(newImage, newWidth, newX - i, newY);
                    }
                    res /= ((extX * 2 + 1));
                    SetPixelColor(result, width, 0, y, res);

                    for (int x = 1; x < width; x++)
                    {
                        newX = extX + x;
                        newY = extY + y;

                        Vector4 oldC = GetPixelColor(newImage, newWidth, (newX - 1) - extX, newY) / (extX * 2 + 1);
                        Vector4 newC = GetPixelColor(newImage, newWidth, newX + extX, newY) / (extX * 2 + 1);

                        res += (-oldC + newC);

                        res.X = Math.Clamp(res.X, 0, 255f);
                        res.Y = Math.Clamp(res.Y, 0, 255f);
                        res.Z = Math.Clamp(res.Z, 0, 255f);
                        res.W = Math.Clamp(res.W, 0, 255f);

                        SetPixelColor(result, width, x, y, res);
                    }
                }
            });
            return result;
        }
        public static float[] BoxBlur(float[] image, int width, int height, int extX, int extY)
        {
            float[] result = new float[image.Length];
            float[] newImage = ExtendImage(image, width, height, extX, extY);
            int newWidth = width + 2 * extX;
            Parallel.ForEach(Partitioner.Create(0, width), range =>
            {
                for (int x = range.Item1; x < range.Item2; x++)
                {
                    int newX = x + extX;
                    int newY = extY + 0;

                    Vector4 res = GetPixelColor(newImage, newWidth, newX, newY);

                    for (int i = 1; i <= extY; i++)
                    {
                        res += GetPixelColor(newImage, newWidth, newX, newY + i);
                        res += GetPixelColor(newImage, newWidth, newX, newY - i);
                    }
                    res /= (extY * 2 + 1);
                    SetPixelColor(result, width, x, 0, res);

                    for (int y = 1; y < height; y++)
                    {
                        newX = x + extX;
                        newY = y + extY;

                        Vector4 oldC = GetPixelColor(newImage, newWidth, newX, (newY - 1) - extY) / ((extY * 2 + 1));
                        Vector4 newC = GetPixelColor(newImage, newWidth, newX, newY + extY) / ((extY * 2 + 1));

                        res += (-oldC + newC);

                        res.X = Math.Clamp(res.X, 0, 1f);
                        res.Y = Math.Clamp(res.Y, 0, 1f);
                        res.Z = Math.Clamp(res.Z, 0, 1f);
                        res.W = Math.Clamp(res.W, 0, 1f);

                        SetPixelColor(result, width, x, y, res);
                    }
                }
            });
            Parallel.ForEach(Partitioner.Create(0, height), range =>
            {
                for (int y = range.Item1; y < range.Item2; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int newX = x + extX;
                        int newY = y + extY;

                        SetPixelColor(newImage, newWidth, newX, newY, GetPixelColor(result, width, x, y));
                    }
                }
            });
            Parallel.ForEach(Partitioner.Create(0, height), range =>
            {
                for (int y = range.Item1; y < range.Item2; y++)
                {

                    int newX = 0 + extX;
                    int newY = extY + y;

                    Vector4 res = GetPixelColor(newImage, newWidth, newX, newY);

                    for (int i = 1; i <= extX; i++)
                    {
                        res += GetPixelColor(newImage, newWidth, newX + i, newY);
                        res += GetPixelColor(newImage, newWidth, newX - i, newY);
                    }
                    res /= ((extX * 2 + 1));
                    SetPixelColor(result, width, 0, y, res);

                    for (int x = 1; x < width; x++)
                    {
                        newX = extX + x;
                        newY = extY + y;

                        Vector4 oldC = GetPixelColor(newImage, newWidth, (newX - 1) - extX, newY) / (extX * 2 + 1);
                        Vector4 newC = GetPixelColor(newImage, newWidth, newX + extX, newY) / (extX * 2 + 1);

                        res += (-oldC + newC);

                        res.X = Math.Clamp(res.X, 0, 1f);
                        res.Y = Math.Clamp(res.Y, 0, 1f);
                        res.Z = Math.Clamp(res.Z, 0, 1f);
                        res.W = Math.Clamp(res.W, 0, 1f);

                        SetPixelColor(result, width, x, y, res);
                    }
                }
            });
            return result;
        }
        public static byte[] ApproxGaussianBlur(byte[] image, int width, int height)
        {
            byte[] res = image;

            int n = 3;
            var boxSizesY = GaussBoxesSizes(CountSqrOnExt(extY), n);
            var boxSizesX = GaussBoxesSizes(CountSqrOnExt(extX), n);

            for (int i = 0; i < n; i++)
            {
                int extX = (boxSizesX[i] - 1) / 2;
                int extY = (boxSizesY[i] - 1) / 2;
                res = BoxBlur(res, width, height, extX, extY);
            }
            return res;
        }
        public static float[] ApproxGaussianBlur(float[] image, int width, int height)
        {
            float[] res = image;

            int n = 3;
            var boxSizesY = GaussBoxesSizes(CountSqrOnExt(extY), n);
            var boxSizesX = GaussBoxesSizes(CountSqrOnExt(extX), n);

            for (int i = 0; i < n; i++)
            {
                int extX = (boxSizesX[i] - 1) / 2;
                int extY = (boxSizesY[i] - 1) / 2;
                res = BoxBlur(res, width, height, extX, extY);
            }
            return res;
        }
        private static int[] GaussBoxesSizes(double sqrS, int n)
        {
            int[] res = new int[n];
            int boxSize =
                Convert.ToInt32(Math.Round(Math.Sqrt(12 * sqrS / n + 1), MidpointRounding.ToNegativeInfinity));
            if (boxSize % 2 == 0) boxSize++;

            for (int i = 0; i < n; i++)
            {
                res[i] = boxSize;
            }

            return res;
        }
        public static float[] GaussianBlur(float[] image, int width, int height)
        {
            float[] res = new float[width * height * 4];
            float[] newImage = ExtendImage(image, width, height, extX, extY);
            int newWidth = width + 2 * extX;

            Parallel.ForEach(Partitioner.Create(0, height), range =>
            {
                for (int y = range.Item1; y < range.Item2; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int newX = x + extX;
                        int newY = y + extY;

                        Vector4 curSum = GetPixelColor(newImage, newWidth, newX, newY);

                        curSum *= (float)coeffsX[0];

                        for (int i = 1; i <= extX; i++)
                        {
                            Vector4 col1 = GetPixelColor(newImage, newWidth, newX - i, newY) * (float)coeffsX[i];
                            Vector4 col2 = GetPixelColor(newImage, newWidth, newX + i, newY) * (float)coeffsX[i];

                            curSum += col1;
                            curSum += col2;
                        }
                        SetPixelColor(res, width, x, y, curSum);
                        res[(y * width + x) * 4 + 3] = 1f;
                    }
                }
            });

            Parallel.ForEach(Partitioner.Create(0, height), range =>
            {
                for (int y = range.Item1; y < range.Item2; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int newX = x + extX;
                        int newY = y + extY;

                        SetPixelColor(newImage, newWidth, newX, newY, GetPixelColor(res, width, x, y));
                    }
                }
            });

            Parallel.ForEach(Partitioner.Create(0, height), range =>
            {
                for (int y = range.Item1; y < range.Item2; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int newX = x + extX;
                        int newY = y + extY;

                        Vector4 curSum = GetPixelColor(newImage, newWidth, newX, newY);

                        curSum *= (float)coeffsY[0];

                        for (int i = 1; i <= extY; i++)
                        {
                            Vector4 col1 = GetPixelColor(newImage, newWidth, newX, newY - i) * (float)coeffsY[i];
                            Vector4 col2 = GetPixelColor(newImage, newWidth, newX, newY + i) * (float)coeffsY[i];

                            curSum += col1;
                            curSum += col2;
                        }

                        SetPixelColor(res, width, x, y, curSum);

                        res[(y * width + x) * 4 + 3] = 1f;
                    }
                }
            });
            return res;
        }
        public static byte[] GaussianBlur(byte[] image, int width, int height)
        {
            byte[] res = new byte[width * height * 4];
            byte[] newImage = ExtendImage(image, width, height, extX, extY);
            int newWidth = width + 2 * extX;

            Parallel.ForEach(Partitioner.Create(0, height), range =>
            {
                for (int y = range.Item1; y < range.Item2; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int newX = x + extX;
                        int newY = y + extY;

                        Vector4 curSum = GetPixelColor(newImage, newWidth, newX, newY);

                        curSum *= (float)coeffsX[0];

                        for (int i = 1; i <= extX; i++)
                        {
                            Vector4 col1 = GetPixelColor(newImage, newWidth, newX - i, newY) * (float)coeffsX[i];
                            Vector4 col2 = GetPixelColor(newImage, newWidth, newX + i, newY) * (float)coeffsX[i];

                            curSum += col1;
                            curSum += col2;
                        }
                        SetPixelColor(res, width, x, y, curSum);
                        res[(y * width + x) * 4 + 3] = 255;
                    }
                }
            });

            Parallel.ForEach(Partitioner.Create(0, height), range =>
            {
                for (int y = range.Item1; y < range.Item2; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int newX = x + extX;
                        int newY = y + extY;

                        SetPixelColor(newImage, newWidth, newX, newY, GetPixelColor(res, width, x, y));
                    }
                }
            });

            Parallel.ForEach(Partitioner.Create(0, height), range =>
            {
                for (int y = range.Item1; y < range.Item2; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int newX = x + extX;
                        int newY = y + extY;

                        Vector4 curSum = GetPixelColor(newImage, newWidth, newX, newY);

                        curSum *= (float)coeffsY[0];

                        for (int i = 1; i <= extY; i++)
                        {
                            Vector4 col1 = GetPixelColor(newImage, newWidth, newX, newY - i) * (float)coeffsY[i];
                            Vector4 col2 = GetPixelColor(newImage, newWidth, newX, newY + i) * (float)coeffsY[i];

                            curSum += col1;
                            curSum += col2;
                        }

                        SetPixelColor(res, width, x, y, curSum);

                        res[(y * width + x) * 4 + 3] = 255;
                    }
                }
            });
            return res;
        }
        private static float[] ExtendImage(float[] image, int width, int height, int extX, int extY )
        {
            float[] newImage = new float[4 * (width + 2 * extX) * (height + 2 * extY)];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int oldPoint = 4 * (y * width + x);
                    int newPoint = 4 * ((y + extY) * (width + 2 * extX) + (x + extX));

                    newImage[newPoint] = image[oldPoint];
                    newImage[newPoint + 1] = image[oldPoint + 1];
                    newImage[newPoint + 2] = image[oldPoint + 2];
                    newImage[newPoint + 3] = image[oldPoint + 3];
                }
            }
            FillWithLast(newImage, image, width, height, extX, extY);
            return newImage;
        }
        private static byte[] ExtendImage(byte[] image, int width, int height, int extX, int extY)
        {
            byte[] newImage = new byte[4 * (width + 2 * extX) * (height + 2 * extY)];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int oldPoint = 4 * (y * width + x);
                    int newPoint = 4 * ((y + extY) * (width + 2 * extX) + (x + extX));

                    newImage[newPoint] = image[oldPoint];
                    newImage[newPoint + 1] = image[oldPoint + 1];
                    newImage[newPoint + 2] = image[oldPoint + 2];
                    newImage[newPoint + 3] = image[oldPoint + 3];
                }
            }
            FillWithLast(newImage, image, width, height, extX, extY);
            return newImage;
        }
        public static void CountGaussian()
        {
            double sqrSX = CountSqrOnExt(extX);
            double sqrSY = CountSqrOnExt(extY);

            coeffsX = new double[extX + 1];
            coeffsY = new double[extY + 1];

            double[] cX = CountSigmaPercent(sqrSX);
            double[] cY = CountSigmaPercent(sqrSY);

            Array.Copy(cX, coeffsX, coeffsX.Length > cX.Length ? cX.Length : coeffsX.Length);
            Array.Copy(cY, coeffsY, coeffsY.Length > cY.Length ? cY.Length : coeffsY.Length);
        }

        private static double[] CountSigmaPercent(double sqrS)
        {
            int sigma =
                Convert.ToInt32(
                Math.Round(Math.Sqrt(sqrS), MidpointRounding.ToPositiveInfinity));

            int _3sigma = sigma >= 1 ? 3 * sigma : 3;
            double[] coeffs = new double[_3sigma + 1];

            double k = 1 / (Math.Sqrt(2 * sqrS * Math.PI));
            double sum = 0;

            for (int i = 0; i <= _3sigma; i++)
            {
                coeffs[i] = k * Math.Exp(-(i * i) / (2 * sqrS));
                sum += coeffs[i];
            }
            sum *= 2;
            sum -= coeffs[0];

            for (int i = 0; i < coeffs.Length; i++)
            {
                coeffs[i] = coeffs[i] / sum;
            }

            if (sqrS == 0)
            {
                coeffs[0] = 1;
            }

            return coeffs;
        }
        public static Vector4 GetPixelColor(float[] image, int width, int x, int y)
        {
            int point = 4 * (x + y * width);
            Vector4 vector4 = new Vector4(
                image[point],
                image[point + 1],
                image[point + 2],
                image[point + 3]
                );

            return vector4;
        }
        public static Vector4 GetPixelColor(byte[] image, int width, int x, int y)
        {
            int point = 4 * (x + y * width);
            Vector4 vector4 = new Vector4(
                image[point],
                image[point + 1],
                image[point + 2],
                image[point + 3]
                );

            return vector4;
        }
        public static void SetPixelColor(float[] image, int width, int x, int y, Vector4 v4)
        {
            int point = 4 * (x + y * width);


            image[point] = (v4.X);
            image[point + 1] = (v4.Y);
            image[point + 2] = v4.Z;
            image[point + 3] = (v4.W);
        }

        public static void SetPixelColor(byte[] image, int width, int x, int y, Vector4 v4)
        {
            int point = 4 * (x + y * width);

            image[point] = (byte)(v4.X);
            image[point + 1] = (byte)(v4.Y);
            image[point + 2] = (byte)v4.Z;
            image[point + 3] = (byte)(v4.W);
        }
        private static void FillWithLast(float[] newImage, float[] image, int width, int height, int extX, int extY)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < extX; x++)
                {
                    int oldPoint1 = 4 * (y * width);
                    int oldPoint2 = 4 * (y * width + width - 1);
                    int newPoint1 = 4 * ((y + extY) * (width + 2 * extX) + (x));
                    int newPoint2 = 4 * ((y + extY) * (width + 2 * extX) + (x + width + extX));

                    CopyColor(newImage, image, newPoint1, oldPoint1);
                    CopyColor(newImage, image, newPoint2, oldPoint2);
                }
            }
            for (int y = 0; y < extY; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int oldPoint1 = 4 * x;
                    int oldPoint2 = 4 * ((height - 1) * width + x);
                    int newPoint1 = 4 * ((y) * (width + 2 * extX) + (x + extX));
                    int newPoint2 = 4 * ((y + height + extY) * (width + 2 * extX) + (x + extX));

                    CopyColor(newImage, image, newPoint1, oldPoint1);
                    CopyColor(newImage, image, newPoint2, oldPoint2);
                }
            }

            for (int y = 0; y < extY; y++)
            {
                int oldPoint1 = 0;
                int oldPoint2 = 4 * (width - 1);
                int oldPoint3 = 4 * ((height - 1) * width);
                int oldPoint4 = 4 * ((height - 1) * width + width - 1);

                for (int x = 0; x < extX; x++)
                {
                    int newPoint1 = 4 * (y * (width + 2 * extX) + x);
                    int newPoint2 = 4 * (y * (width + 2 * extX) + (x + width + extX));
                    int newPoint3 = 4 * ((y + extY + height) * (width + 2 * extX) + x);
                    int newPoint4 = 4 * ((y + extY + height) * (width + 2 * extX) + (x + width + extX));

                    CopyColor(newImage, image, newPoint1, oldPoint1);
                    CopyColor(newImage, image, newPoint2, oldPoint2);
                    CopyColor(newImage, image, newPoint3, oldPoint3);
                    CopyColor(newImage, image, newPoint4, oldPoint4);
                }
            }
        }
        private static void FillWithLast(byte[] newImage, byte[] image, int width, int height, int extX, int extY)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < extX; x++)
                {
                    int oldPoint1 = 4 * (y * width);
                    int oldPoint2 = 4 * (y * width + width - 1);
                    int newPoint1 = 4 * ((y + extY) * (width + 2 * extX) + (x));
                    int newPoint2 = 4 * ((y + extY) * (width + 2 * extX) + (x + width + extX));

                    CopyColor(newImage, image, newPoint1, oldPoint1);
                    CopyColor(newImage, image, newPoint2, oldPoint2);
                }
            }
            for (int y = 0; y < extY; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int oldPoint1 = 4 * x;
                    int oldPoint2 = 4 * ((height - 1) * width + x);
                    int newPoint1 = 4 * ((y) * (width + 2 * extX) + (x + extX));
                    int newPoint2 = 4 * ((y + height + extY) * (width + 2 * extX) + (x + extX));

                    CopyColor(newImage, image, newPoint1, oldPoint1);
                    CopyColor(newImage, image, newPoint2, oldPoint2);
                }
            }

            for (int y = 0; y < extY; y++)
            {
                int oldPoint1 = 0;
                int oldPoint2 = 4 * (width - 1);
                int oldPoint3 = 4 * ((height - 1) * width);
                int oldPoint4 = 4 * ((height - 1) * width + width - 1);

                for (int x = 0; x < extX; x++)
                {
                    int newPoint1 = 4 * (y * (width + 2 * extX) + x);
                    int newPoint2 = 4 * (y * (width + 2 * extX) + (x + width + extX));
                    int newPoint3 = 4 * ((y + extY + height) * (width + 2 * extX) + x);
                    int newPoint4 = 4 * ((y + extY + height) * (width + 2 * extX) + (x + width + extX));

                    CopyColor(newImage, image, newPoint1, oldPoint1);
                    CopyColor(newImage, image, newPoint2, oldPoint2);
                    CopyColor(newImage, image, newPoint3, oldPoint3);
                    CopyColor(newImage, image, newPoint4, oldPoint4);
                }
            }
        }
        private static void CopyColor(float[] newImage, float[] oldImage, int newPoint, int oldPoint)
        {
            newImage[newPoint] = oldImage[oldPoint];
            newImage[newPoint + 1] = oldImage[oldPoint + 1];
            newImage[newPoint + 2] = oldImage[oldPoint + 2];
            newImage[newPoint + 3] = oldImage[oldPoint + 3];
        }
        private static void CopyColor(byte[] newImage, byte[] oldImage, int newPoint, int oldPoint)
        {
            newImage[newPoint] = oldImage[oldPoint];
            newImage[newPoint + 1] = oldImage[oldPoint + 1];
            newImage[newPoint + 2] = oldImage[oldPoint + 2];
            newImage[newPoint + 3] = oldImage[oldPoint + 3];
        }
        public static double CountSqrOnExt(int ext)
        {
            double res = ((double)ext / 3) * ((double)ext / 3);
            res = Math.Round(res, 4);
            return res;
        }
    }
}
