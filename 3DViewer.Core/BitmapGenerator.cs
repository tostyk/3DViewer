﻿using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Numerics;
using _3DViewer.Core.obj_parse;

namespace _3DViewer.Core
{
    public unsafe class BitmapGenerator
    {
        public const int ARGB = 4;

        private readonly ObjVertices _modelCoordinates;
        private Vector4[] _windowCoordinates;
        private Vector4[] _worldCoordinates;

        private Vector3[] _textureCoordinates;

        private int _width;
        private int _height;

        private int _widthDiffuse;
        private int _heightDiffuse;

        private float minDepth = 0f;
        private float maxDepth = 1f;
        private float minX = 0f;
        private float minY = 0f;


        private float minXText = 0f;
        private float minYText = 0f;

        private Camera _camera;
        private Matrix4x4 _normalizationMatrix = Matrix4x4.Identity;
        private Matrix4x4 _normalizationTextureMatrix = Matrix4x4.Identity;
        private Matrix4x4 currViewport;
        private Matrix4x4 currProjection;
        private Matrix4x4 currView;
        private Matrix4x4 currModel;

        private byte[] _image;
        private double[] _zbuffer;
        private float[] _brightness;
        private float[] _colors;

        private byte[] _imageDiffuse;


        private float _intensivityCoef = 0.7f;
        //x = b, y = g, z = r
        private Vector4 BackgroundColor = new(0, 0, 0, 1f);
        private Vector4 AmbientColor = new(0f, 0.0f, 1f, 1f);
        private Vector4 DiffuseColor = new(0.5f, 0.0f, 1f, 1f);
        private Vector4 SpecularColor = new(1f, 1f, 1f, 1f);

        private LightningCounter _lightningCounter;
        private BloomCounter _bloomCounter;

        public BitmapGenerator(
            ObjVertices modelCoordinates,
            int width,
            int height
            )
        {
            BloomCounter.CountGaussian();

            _lightningCounter = new LightningCounter(
                AmbientColor,
                DiffuseColor,
                SpecularColor
                );


            _camera = new Camera();
            _width = width;
            _height = height;

            _modelCoordinates = modelCoordinates;
            _modelCoordinates.SeparateTriangles();

            _normalizationMatrix = Normalize();

            _windowCoordinates = new Vector4[_modelCoordinates.Vertices.Length];
            _worldCoordinates = new Vector4[_modelCoordinates.Vertices.Length];
            _textureCoordinates = new Vector3[_modelCoordinates.TextureVertices.Length];

            Resized(width, height);
        }

        public void Resized(
            int width,
            int height)
        {
            _image = new byte[height * width * ARGB];
            _brightness = new float[height * width * ARGB];
            _colors = new float[height * width * ARGB];
            _zbuffer = new double[height * width];

            _width = width;
            _height = height;

            currViewport = Viewport();
            currProjection = Projection();
            currView = View();
            currModel = Model();

        }
        public void SetDiffuseMap(byte[] diffuse, int width, int height)
        {
            _imageDiffuse = diffuse;
            _widthDiffuse = width;
            _heightDiffuse = height;
        }
        private Matrix4x4 Normalize()
        {
            Vector3 max = new(
                _modelCoordinates.Vertices.Max(v => v.X),
                _modelCoordinates.Vertices.Max(v => v.Y),
                _modelCoordinates.Vertices.Max(v => v.Z)
                );
            Vector3 min = new(
                _modelCoordinates.Vertices.Min(v => v.X),
                _modelCoordinates.Vertices.Min(v => v.Y),
                _modelCoordinates.Vertices.Min(v => v.Z)
                );
            Vector3 avg = (min + max) / 2;
            Vector3 scaleVector = (max - min);
            float scale = 1 / Math.Max(Math.Max(scaleVector.X, scaleVector.Y), scaleVector.Z);

            Matrix4x4 matrix = Matrix4x4.CreateTranslation(-avg) * Matrix4x4.CreateScale(scale);
            float radius = scale * Vector3.Distance(avg, min);
            _camera.Normalize(radius, (float)_width / _height);
            return matrix;
        }

        public byte[] GenerateImage()
        {
            ClearImage();
            RecountCoordinates();

            TriangleRasterization();
            return _image;
        }

        public void ChangeCameraPosition(float deltaX, float deltaY, float deltaZ)
        {
            currView = View();
        }

        private void RecountCoordinates()
        {
            Matrix4x4 modelViewProjectionMatrix =
                currModel *
                currView *
                currProjection;

            Parallel.ForEach(Partitioner.Create(0, _modelCoordinates.Vertices.Length), range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    _windowCoordinates[i] = Vector4.Transform(_modelCoordinates.Vertices[i], modelViewProjectionMatrix);

                    float w = 1 / _windowCoordinates[i].W;
                    _worldCoordinates[i] = Vector4.Transform(_modelCoordinates.Vertices[i], currModel);

                    _windowCoordinates[i] *= w;
                    _windowCoordinates[i] = Vector4.Transform(_windowCoordinates[i], currViewport);
                    _windowCoordinates[i].W /= w;
                }
            });
        }

        private Matrix4x4 Model()
        {
            Matrix4x4 resultMatrix = Matrix4x4.Identity;
            resultMatrix *= _normalizationMatrix;
            return resultMatrix;
        }

        private Matrix4x4 ModelTexture()
        {
            Matrix4x4 resultMatrix = Matrix4x4.Identity;
            resultMatrix *= _normalizationTextureMatrix;
            return resultMatrix;
        }

        public void ReplaceCameraByScreenCoordinates(
            float x0,
            float y0,
            float x1,
            float y1
            )
        {
            float dx = (x1 - x0) / _width;
            float dy = (y1 - y0) / _height;
            _camera.ReplaceCameraByScreenCoordinates(dx, dy);
            currView = View();
        }

        public void Scale(float scale)
        {
            _camera.FOV = (float)Math.Clamp(_camera.FOV - scale, 0.1f, Math.PI / 2);
            currProjection = Projection();
        }

        private Matrix4x4 View()
        {
            Matrix4x4 resultMatrix = Matrix4x4.CreateLookAt(_camera.Position, _camera.Target, _camera.Up);
            return resultMatrix;
        }
        private Matrix4x4 ProjectionTexture()
        {
            return Matrix4x4.CreatePerspectiveFieldOfView(_camera.FOV, (float)_widthDiffuse / _heightDiffuse, _camera.ZNear, _camera.ZFar);
        }
        private Matrix4x4 Projection()
        {
            return Matrix4x4.CreatePerspectiveFieldOfView(_camera.FOV, (float)_width / _height, _camera.ZNear, _camera.ZFar);
        }
        private Matrix4x4 Viewport()
        {
            Matrix4x4 matrix = new()
            {
                M11 = _width / 2f,
                M22 = -_height / 2f,
                M33 = maxDepth - minDepth,
                M44 = 1,

                M41 = minX + _width / 2f,
                M42 = minY + _height / 2f,
                M43 = _height / 2f,
            };
            return matrix;
        }
        private void ClearImage()
        {
            Parallel.ForEach(Partitioner.Create(0, _image.Length / 4), range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    _colors[i * 4 + 0] = BackgroundColor.X;
                    _colors[i * 4 + 1] = BackgroundColor.Y;
                    _colors[i * 4 + 2] = BackgroundColor.Z;
                    _colors[i * 4 + 3] = BackgroundColor.W;

                    _brightness[i * 4 + 0] = BackgroundColor.X;
                    _brightness[i * 4 + 1] = BackgroundColor.Y;
                    _brightness[i * 4 + 2] = BackgroundColor.Z;
                    _brightness[i * 4 + 3] = BackgroundColor.W;


                    _image[i * 4 + 0] = (byte)(255 * BackgroundColor.X);
                    _image[i * 4 + 1] = (byte)(255 * BackgroundColor.Y);
                    _image[i * 4 + 2] = (byte)(255 * BackgroundColor.Z);
                    _image[i * 4 + 3] = (byte)(255 * BackgroundColor.W);

                    _zbuffer[i] = double.PositiveInfinity;
                }
            });
        }
        private void DrawTriangle(Polygon triangle)
        {
            int[] vertices = new int[3];
            int[] textVertices = new int[3];

            for (int i = 0; i < 3; i++)
            {
                vertices[i] = triangle.FaceVertices[i].vertex;
                textVertices[i] = triangle.FaceVertices[i].textureVertex;
            }
            Vector3 ta = _modelCoordinates.TextureVertices[textVertices[0]];
            Vector3 tb = _modelCoordinates.TextureVertices[textVertices[1]];
            Vector3 tc = _modelCoordinates.TextureVertices[textVertices[2]];

            Vector3 a = new Vector3
            {
                X = _windowCoordinates[vertices[0]].X,
                Y = _windowCoordinates[vertices[0]].Y,
                Z = _windowCoordinates[vertices[0]].Z
            };

            Vector3 b = new Vector3
            {
                X = _windowCoordinates[vertices[1]].X,
                Y = _windowCoordinates[vertices[1]].Y,
                Z = _windowCoordinates[vertices[1]].Z
            };
            Vector3 c = new Vector3
            {
                X = _windowCoordinates[vertices[2]].X,
                Y = _windowCoordinates[vertices[2]].Y,
                Z = _windowCoordinates[vertices[2]].Z
            };

            if (Vector3.Dot(Vector3.Normalize(Vector3.Cross(a - b, c - b)), _camera.ViewerPosition) < 0) return;

            var ambient = _lightningCounter.CountAmbient();

            Vector3 wna = _modelCoordinates.VerticesNormals[vertices[0]];
            Vector3 wnb = _modelCoordinates.VerticesNormals[vertices[1]];
            Vector3 wnc = _modelCoordinates.VerticesNormals[vertices[2]];

            if (a.Y > b.Y)
            {
                (a, b) = (b, a);
                (ta, tb) = (tb, ta);
                (wna, wnb) = (wnb, wna);
            };
            if (a.Y > c.Y)
            {
                (a, c) = (c, a);
                (ta, tc) = (tc, ta);
                (wna, wnc) = (wnc, wna);
            }
            if (b.Y > c.Y)
            {
                (b, c) = (c, b);
                (tb, tc) = (tc, tb);
                (wnc, wnb) = (wnb, wnc);
            };

            if (a.Y == c.Y) return;


            Vector3 kp1 = (c - a) / (c.Y - a.Y);
            Vector3 kp2 = (b - a) / (b.Y - a.Y);
            Vector3 kp3 = (c - b) / (c.Y - b.Y);

            int top = Math.Max(0, Convert.ToInt32(Math.Ceiling(a.Y)));
            int bottom = Math.Min(_height, Convert.ToInt32(Math.Ceiling(c.Y)));

            for (int y = top; y < bottom; y++)
            {
                float kacY = (y - a.Y) / (c.Y - a.Y);
                float kabY = (y - a.Y) / (b.Y - a.Y);
                float kbcY = (y - b.Y) / (c.Y - b.Y);

                Vector3 n1 = wna + (wnc - wna) * kacY;

                Vector3 n2 = y < b.Y ?

                    wna + (wnb - wna) * kabY
                    : wnb + (wnc - wnb) * kbcY;


                Vector3 t1 = (ta / a.Z + (tc / c.Z - ta / a.Z) * kacY) / (1 / a.Z + (1 / c.Z - 1 / a.Z) * kacY);

                Vector3 t2 = y < b.Y ?
                     (ta / a.Z + (tb / b.Z - ta / a.Z) * kabY) / (1 / a.Z + (1 / b.Z - 1 / a.Z) * kabY)
                    : (tb / b.Z + (tc / c.Z - tb / b.Z) * kbcY) / (1 / b.Z + (1 / c.Z - 1 / b.Z) * kbcY);



                n1 = Vector3.Normalize(n1);
                n2 = Vector3.Normalize(n2);

                Vector3 lp = a + (y - a.Y) * kp1;
                Vector3 rp = y < b.Y ? a + (y - a.Y) * kp2 : b + (y - b.Y) * kp3;

                if (lp.X > rp.X)
                {
                    (lp, rp) = (rp, lp);
                    (n1, n2) = (n2, n1);
                    (t1, t2) = (t2, t1);
                }

                int left = Math.Max(0, Convert.ToInt32(Math.Ceiling(lp.X)));
                int right = Math.Min(_width, Convert.ToInt32(Math.Ceiling(rp.X)));

                Vector3 kp = (rp - lp) / (rp.X - lp.X);
                Vector3 kn = (n2 - n1) / (rp.X - lp.X);

                for (int x = left; x < right; x++)
                {
                    int ind = y * _width + x;

                    Vector3 p = lp + (x - lp.X) * kp;

                    if (_zbuffer[ind] > p.Z)
                    {
                        _zbuffer[ind] = p.Z;
                        int point = ARGB * ind;

                        Vector3 normal = n1 + (x - lp.X) * kn;
                        normal = Vector3.Normalize(normal);

                        float kt = (x - lp.X) / (rp.X - lp.X);

                        Vector3 t = ((1 - kt) * t1 / lp.Z + kt * t2 / rp.Z) / ((1 - kt) * 1 / lp.Z + kt / rp.Z);


                        int tx = Convert.ToInt32(Math.Clamp((t.X * (_widthDiffuse - 1)), 0, _widthDiffuse - 1));
                        int ty = Convert.ToInt32(Math.Clamp((1 - t.Y) * (_heightDiffuse - 1), 0, _heightDiffuse - 1));

                        var diffColor = BloomCounter.GetPixelColor(
                            _imageDiffuse,
                            _widthDiffuse,
                            tx,
                            ty) / 255;

                        Vector3 diffuseAlbedo = new Vector3(diffColor.X, diffColor.Y, diffColor.Z);

                        var diffuse = _lightningCounter.CountDiffuse(normal, _camera.LightPosition, diffuseAlbedo);
                        var specular = _lightningCounter.CountSpecular(normal, _camera.LightPosition, -_camera.Position);

                        Vector3 fragColor = diffuse + ambient + specular;
                        Vector3 brightColor = BloomCounter.CountBloom(fragColor);


                        _colors[point + 0] = fragColor.X;
                        _colors[point + 1] = fragColor.Y;
                        _colors[point + 2] = fragColor.Z;
                        _colors[point + 3] = AmbientColor.W;

                        _brightness[point + 0] = brightColor.X;
                        _brightness[point + 1] = brightColor.Y;
                        _brightness[point + 2] = brightColor.Z;
                        _brightness[point + 3] = AmbientColor.W;

                        fragColor = LightningCounter.ColorVector3(fragColor);

                        fragColor *= 255;

                        _image[point + 0] = (byte)fragColor.X;
                        _image[point + 1] = (byte)fragColor.Y;
                        _image[point + 2] = (byte)fragColor.Z;
                        _image[point + 3] = (byte)(255);
                    }

                }
            }
        }
        private void TriangleRasterization()
        {
            Parallel.ForEach(Partitioner.Create(0, _modelCoordinates.Triangles.Length), range =>
            {
                for (int j = range.Item1; j < range.Item2; j++)
                {
                    DrawTriangle(_modelCoordinates.Triangles[j]);
                }
            });
            //bloom start

            /*
            float[] gBl = BloomCounter.GaussianBlur(_brightness, _width, _height);
            Parallel.ForEach(Partitioner.Create(0, _height), range =>
            {
                for (int y = range.Item1; y < range.Item2; y++)
                {
                    for (int x = 0; x < _width; x++)
                    {
                        Vector4 c = BloomCounter.GetPixelColor(gBl, _width, x, y) + BloomCounter.GetPixelColor(_colors, _width, x, y);
                        Vector3 a = LightningCounter.ColorVector3(new Vector3(c.X, c.Y, c.Z));
                        a *= 255;
                        BloomCounter.SetPixelColor(_image, _width, x, y, new Vector4(a.X, a.Y, a.Z, 255));
                    }
                }
            });
            */

            //bloom end
        }
    }
}
