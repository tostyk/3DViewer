using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Numerics;
using System.Text;
using _3DViewer.Core.obj_parse;

namespace _3DViewer.Core
{
    public unsafe class BitmapGenerator
    {
        public const int ARGB = 4;

        private readonly ObjVertices _modelCoordinates;
        private readonly MtlInformation _mtlInformation;

        private Vector4[] _windowCoordinates;
        private Vector4[] _worldCoordinates;

        private int _width;
        private int _height;

        private float minDepth = 0f;
        private float maxDepth = 1f;
        private float minX = 0f;
        private float minY = 0f;

        private Camera _camera;
        private Matrix4x4 _normalizationMatrix = Matrix4x4.Identity;
        private Matrix4x4 currViewport;
        private Matrix4x4 currProjection;
        private Matrix4x4 currView;
        private Matrix4x4 currModel;

        private byte[] _image;
        private double[] _zbuffer;
        private float[] _brightness;
        private float[] _colors;

        private float _intensivityCoef = 0.7f;
        //x = b, y = g, z = r
        private Vector3 BackgroundColor = new(0, 0, 0);
        private Vector3 AmbientColor = new(0.1f, 0.0f, 0.1f);
        private Vector3 DiffuseColor = new(0.1f, 0.0f, 0.1f);
        private Vector3 SpecularColor = new(1f, 1f, 1f);

        private BloomCounter _bloomCounter;

        public BitmapGenerator(
            ObjVertices modelCoordinates,
            MtlInformation mtlInformation,
            int width,
            int height
            )
        {
            BloomCounter.CountGaussian();

            _camera = new Camera();
            _width = width;
            _height = height;

            _modelCoordinates = modelCoordinates;
            _mtlInformation = mtlInformation;

            _normalizationMatrix = Normalize();

            _windowCoordinates = new Vector4[_modelCoordinates.Vertices.Length];
            _worldCoordinates = new Vector4[_modelCoordinates.Vertices.Length];

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
                    _worldCoordinates[i] = Vector4.Transform(_modelCoordinates.Vertices[i], currModel *
                currView);
                    _windowCoordinates[i] = Vector4.Transform(_modelCoordinates.Vertices[i], modelViewProjectionMatrix);

                    float w = 1 / _windowCoordinates[i].W;

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
        private Vector3 CountTexture(int width, int height, float x, float y, byte[] image)
        {
            int tx = Convert.ToInt32(Math.Clamp(x * (width - 1), 0, width - 1));
            int ty = Convert.ToInt32(Math.Clamp((1 - y) * (height - 1), 0, height - 1));

            Vector4 temp = (BloomCounter.GetPixelColor(
                image,
                width,
                tx,
                ty) / 255);

            Vector3 result = new(temp.X, temp.Y, temp.Z);

            return result;
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
                    _colors[i * 4 + 3] = 1f;

                    _brightness[i * 4 + 0] = BackgroundColor.X;
                    _brightness[i * 4 + 1] = BackgroundColor.Y;
                    _brightness[i * 4 + 2] = BackgroundColor.Z;
                    _brightness[i * 4 + 3] = 1f;


                    _image[i * 4 + 0] = (byte)(255 * BackgroundColor.X);
                    _image[i * 4 + 1] = (byte)(255 * BackgroundColor.Y);
                    _image[i * 4 + 2] = (byte)(255 * BackgroundColor.Z);
                    _image[i * 4 + 3] = (byte)(255);

                    _zbuffer[i] = double.PositiveInfinity;
                }
            });
        }
        private void DrawTriangle(Polygon triangle, MtlCharacter mtlCharacter)
        {
            int[] vertices = new int[3];
            int[] textVertices = new int[3];
            int[] normals = new int[3];


            for (int i = 0; i < 3; i++)
            {
                vertices[i] = triangle.FaceVertices[i].vertex;
                textVertices[i] = triangle.FaceVertices[i].textureVertex;
                normals[i] = triangle.FaceVertices[i].normalVertex;
            }

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

            Vector3 wna = _modelCoordinates.Normals[normals[0]];
            Vector3 wnb = _modelCoordinates.Normals[normals[1]];
            Vector3 wnc = _modelCoordinates.Normals[normals[2]];

            float azw = 1 / _worldCoordinates[vertices[0]].Z;
            float bzw = 1 / _worldCoordinates[vertices[1]].Z;
            float czw = 1 / _worldCoordinates[vertices[2]].Z;

            Vector3 ta = _modelCoordinates.TextureVertices[textVertices[0]];
            Vector3 tb = _modelCoordinates.TextureVertices[textVertices[1]];
            Vector3 tc = _modelCoordinates.TextureVertices[textVertices[2]];

            ta *= azw;
            tb *= bzw;
            tc *= czw;

            if (Vector3.Dot(Vector3.Normalize(Vector3.Cross(a - b, c - b)), _camera.ViewerPosition) < 0) return;

            var ambient = LightCounter.CountAmbient(AmbientColor);

            if (a.Y > b.Y)
            {
                (a, b) = (b, a);
                (ta, tb) = (tb, ta);
                (wna, wnb) = (wnb, wna);
                (azw, bzw) = (bzw, azw);
            };
            if (a.Y > c.Y)
            {
                (a, c) = (c, a);
                (ta, tc) = (tc, ta);
                (wna, wnc) = (wnc, wna);
                (azw, czw) = (czw, azw);
            }
            if (b.Y > c.Y)
            {
                (b, c) = (c, b);
                (tb, tc) = (tc, tb);
                (wnc, wnb) = (wnb, wnc);
                (bzw, czw) = (czw, bzw);
            };

            if (a.Y == c.Y) return;

            int top = Math.Max(0, Convert.ToInt32(Math.Ceiling(a.Y)));
            int bottom = Math.Min(_height, Convert.ToInt32(Math.Ceiling(c.Y)));

            for (int y = top; y < bottom; y++)
            {
                float kacY = (y - a.Y) / (c.Y - a.Y);
                float kabY = (y - a.Y) / (b.Y - a.Y);
                float kbcY = (y - b.Y) / (c.Y - b.Y);


                float tw1 = azw + (czw - azw) * kacY;
                float tw2 = y < b.Y ?
                            azw + (bzw - azw) * kabY :
                            bzw + (czw - bzw) * kbcY;

                Vector3 t1 = (ta + (tc - ta) * kacY) / (azw + (czw - azw) * kacY);
                Vector3 t2 = y < b.Y ?
                            (ta + (tb - ta) * kabY) / (azw + (bzw - azw) * kabY) :
                            (tb + (tc - tb) * kbcY) / (bzw + (czw - bzw) * kbcY);

                t1 *= tw1;
                t2 *= tw2;

                Vector3 n1 = wna + (wnc - wna) * kacY;

                Vector3 n2 = y < b.Y ?
                    wna + (wnb - wna) * kabY
                    : wnb + (wnc - wnb) * kbcY;

                n1 = Vector3.Normalize(n1);
                n2 = Vector3.Normalize(n2);

                Vector3 lp = a + (c - a) * kacY;
                Vector3 rp = y < b.Y ? a + (b - a) * kabY : b + (c - b) * kbcY;

                if (lp.X > rp.X)
                {
                    (lp, rp) = (rp, lp);
                    (n1, n2) = (n2, n1);
                    (tw1, tw2) = (tw2, tw1);
                    (t1, t2) = (t2, t1);
                }

                int left = Math.Max(0, Convert.ToInt32(Math.Ceiling(lp.X)));
                int right = Math.Min(_width, Convert.ToInt32(Math.Ceiling(rp.X)));

                for (int x = left; x < right; x++)
                {
                    float k = (x - lp.X) / (rp.X - lp.X);
                    int index = y * _width + x;

                    Vector3 p = lp + (rp - lp) * k;
                    Vector3 t = (t1 + (t2 - t1) * k) / (tw1 + (tw2 - tw1) * k);

                    if (_zbuffer[index] > p.Z)
                    {
                        _zbuffer[index] = p.Z;
                        int point = ARGB * index;

                        Vector3 diffuseAlbedo = mtlCharacter.Kd;
                        Vector3 specularAlbedo = mtlCharacter.Ks;
                        Vector3 emission = mtlCharacter.Ke;

                        Vector3 normal = Vector3.Normalize(n1 + (n2 - n1) * k);


                        if (mtlCharacter.normImage != null)
                        {
                            normal = CountTexture(mtlCharacter._widthNorm, mtlCharacter._heightNorm, t.X, t.Y, mtlCharacter.normImage);

                            normal = new Vector3(
                                normal.Z,
                                normal.Y,
                                normal.X
                                );

                            normal = normal * 2 - Vector3.One;

                            normal = Vector3.Normalize(normal);
                        }
                        if (mtlCharacter.kdImage != null)
                        {
                            diffuseAlbedo = CountTexture(mtlCharacter._widthKd, mtlCharacter._heightKd, t.X, t.Y, mtlCharacter.kdImage);
                        }
                        if (mtlCharacter.ksImage != null)
                        {
                            specularAlbedo = CountTexture(mtlCharacter._widthKs, mtlCharacter._heightKs, t.X, t.Y, mtlCharacter.ksImage);
                        }
                        if (mtlCharacter.kaImage != null)
                        {
                            ambient = LightCounter.CountAmbient(CountTexture(mtlCharacter._widthKa, mtlCharacter._heightKa, t.X, t.Y, mtlCharacter.kaImage));
                        }
                        if (mtlCharacter.keImage != null)
                        {
                            emission = CountTexture(mtlCharacter._widthKe, mtlCharacter._heightKe, t.X, t.Y, mtlCharacter.keImage);
                        }

                        var diffuse = LightCounter.CountDiffuse(normal, _camera.LightPosition, diffuseAlbedo);
                        var specular = LightCounter.CountSpecular(normal, _camera.LightPosition, -_camera.Position,
                            specularAlbedo, mtlCharacter.Ns > 0 ? mtlCharacter.Ns : 10f);

                        Vector3 fragColor = diffuse + ambient + specular + emission;

                        _brightness[point + 0] = emission.X;
                        _brightness[point + 1] = emission.Y;
                        _brightness[point + 2] = emission.Z;
                        _brightness[point + 3] = 1f;

                        _colors[point + 0] = fragColor.X;
                        _colors[point + 1] = fragColor.Y;
                        _colors[point + 2] = fragColor.Z;
                        _colors[point + 3] = 1f;

                        fragColor = LightCounter.ColorVector3(fragColor);

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
            for (int i = 0; i < _modelCoordinates.mtlTextureTriangles.Length; i++)
            {
                MtlCharacter currMtlCharacter = _mtlInformation.mtlCharacters.Where(x => x.name == _modelCoordinates.mtlTextureTriangles[i].mtlName).First();
                Parallel.ForEach(Partitioner.Create(_modelCoordinates.mtlTextureTriangles[i].firstFace, _modelCoordinates.mtlTextureTriangles[i].lastFace), range =>
                {
                    for (int j = range.Item1; j <= range.Item2; j++)
                    {
                        DrawTriangle(_modelCoordinates.Triangles[j], currMtlCharacter);
                    }
                });
            }
            //bloom start


            float[] gBl = BloomCounter.GaussianBlur(_brightness, _width, _height);
            Parallel.ForEach(Partitioner.Create(0, _height), range =>
            {
                for (int y = range.Item1; y < range.Item2; y++)
                {
                    for (int x = 0; x < _width; x++)
                    {
                        Vector4 c = BloomCounter.GetPixelColor(gBl, _width, x, y) + BloomCounter.GetPixelColor(_colors, _width, x, y);
                        Vector3 a = LightCounter.ColorVector3(new Vector3(c.X, c.Y, c.Z));
                        a *= 255;
                        BloomCounter.SetPixelColor(_image, _width, x, y, new Vector4(a.X, a.Y, a.Z, 255));
                    }
                }
            });


            //bloom end
        }
    }
}
