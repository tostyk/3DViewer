using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Numerics;


namespace _3DViewer.Core
{
    public unsafe class BitmapGenerator
    {
        public const int ARGB = 4;

        private readonly ObjVertices _modelCoordinates;
        private Vector4[] _windowCoordinates;
        private Vector4[] _worldCoordinates;


        private Vector3[] _worldNormals;

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

        private float _intensivityCoef = 0.7f;

        private Color BackgroundColor = new(255, 255, 255, 255);
        private Color DiffuseColor = new(255, 156, 10, 0);
        private Color AmbientColor = new(255, 10, 120, 0);
        private Color SpecularColor = new(255, 247, 234, 0);

        private LightningCounter _lightningCounter;

        public BitmapGenerator(
            ObjVertices modelCoordinates,
            int width,
            int height
            )
        {
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

            Resized(width, height);
        }

        public void Resized(
            int width,
            int height)
        {
            _image = new byte[height * width * ARGB];
            _zbuffer = new double[height * width * ARGB];

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

        private void ClearImage()
        {
            Parallel.ForEach(Partitioner.Create(0, _image.Length / 4), range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    _image[i * 4 + 0] = BackgroundColor.Blue;
                    _image[i * 4 + 1] = BackgroundColor.Green;
                    _image[i * 4 + 2] = BackgroundColor.Red;
                    _image[i * 4 + 3] = BackgroundColor.Alpha;
                    _zbuffer[i] = double.PositiveInfinity;
                }
            });
        }
        private void DrawLine(Vector3 v0, Vector3 v1, Color color)
        {
            float x0 = v0.X;
            float x1 = v1.X;
            float y0 = v0.Y;
            float y1 = v1.Y;

            int xDiff = Math.Abs((int)(x1 - x0));
            int yDiff = Math.Abs((int)(y1 - y0));
            int L = xDiff > yDiff ? xDiff : yDiff;

            float dx = (x1 - x0) / L;
            float dy = (y1 - y0) / L;

            int X1 = Convert.ToInt32(x0);
            int Y1 = Convert.ToInt32(y0);
            if (L == 0 && X1 > 0 &&
                    Y1 > 0 &&
                    X1 < _width &&
                    Y1 < _height)
            {

                int point = ARGB * (Y1 * _width + X1);

                _image[point + 0] = color.Blue;
                _image[point + 1] = color.Green;
                _image[point + 2] = color.Red;
                _image[point + 3] = color.Alpha;
            }

            for (int i = 0; i < L; i++)
            {
                int X = Convert.ToInt32(x0 + dx * i);
                int Y = Convert.ToInt32(y0 + dy * i);

                if (X > 0 &&
                    Y > 0 &&
                    X < _width &&
                    Y < _height)
                {
                    int point = ARGB * (Y * _width + X);

                    _image[point + 0] = color.Blue;
                    _image[point + 1] = color.Green;
                    _image[point + 2] = color.Red;
                    _image[point + 3] = color.Alpha;
                }
            }
        }

        private void DrawKraskouski(int[] vertices)
        {

            Vector3 inta = new Vector3
            {
                X = _worldCoordinates[vertices[0]].X,
                Y = _worldCoordinates[vertices[0]].Y,
                Z = _worldCoordinates[vertices[0]].Z
            };

            Vector3 intb = new Vector3
            {
                X = _worldCoordinates[vertices[1]].X,
                Y = _worldCoordinates[vertices[1]].Y,
                Z = _worldCoordinates[vertices[1]].Z
            };
            Vector3 intc = new Vector3
            {
                X = _worldCoordinates[vertices[2]].X,
                Y = _worldCoordinates[vertices[2]].Y,
                Z = _worldCoordinates[vertices[2]].Z
            };

            //don't remove poor lambert

            byte currAlpha = 255;
            float intensivity = LightningCounter.Lambert(inta, intb, intc, _camera.LightPosition);
            if (intensivity < 0)
            {
                intensivity = 0;
                // return;
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

            if (Vector3.Dot(Vector3.Normalize(Vector3.Cross(a - b, c - b)), _camera.ViewerPosition) < 0) return;

            var ambient = _lightningCounter.CountAmbient();

            Vector3 wna = _modelCoordinates.VerticesNormals[vertices[0]];
            Vector3 wnb = _modelCoordinates.VerticesNormals[vertices[1]];
            Vector3 wnc = _modelCoordinates.VerticesNormals[vertices[2]];

            if (a.Y > b.Y)
            {
                (a, b) = (b, a);
                (wna, wnb) = (wnb, wna);
            };
            if (a.Y > c.Y)
            {
                (a, c) = (c, a);
                (wna, wnc) = (wnc, wna);
            }
            if (b.Y > c.Y)
            {
                (b, c) = (c, b);
                (wnc, wnb) = (wnb, wnc);
            };

            if (a.Y == c.Y) return;

            var normal = Vector3.Normalize(Vector3.Cross(inta - intb, intc - intb));

            Vector3 kp1 = (c - a) / (c.Y - a.Y);
            Vector3 kp2 = (b - a) / (b.Y - a.Y);
            Vector3 kp3 = (c - b) / (c.Y - b.Y);

            int top = Math.Max(0, Convert.ToInt32(Math.Ceiling(a.Y)));
            int bottom = Math.Min(_height, Convert.ToInt32(Math.Ceiling(c.Y)));


            for (int y = top; y < bottom; y++)
            {
                Vector3 n1 =  wna + (wnc - wna) * (y - a.Y) / (c.Y - a.Y);

                Vector3 n2 = y < b.Y ? wna + (wnb - wna) * (y - a.Y) / (b.Y - a.Y)
                    : wnb + (wnc - wnb) * (y - b.Y) / (c.Y - b.Y);

                n1 = Vector3.Normalize(n1);
                n2 = Vector3.Normalize(n2);

                Vector3 lp = a + (y - a.Y) * kp1;
                Vector3 rp = y < b.Y ? a + (y - a.Y) * kp2 : b + (y - b.Y) * kp3;
                if (lp.X > rp.X)
                {
                    (lp, rp) = (rp, lp);
                    (n1, n2) = (n2, n1);
                }
                int left = Math.Max(0, Convert.ToInt32(Math.Ceiling(lp.X)));
                int right = Math.Min(_width, Convert.ToInt32(Math.Ceiling(rp.X)));
                for (int x = left; x < right; x++)
                {
                    int ind = Convert.ToInt32(y) * _width + Convert.ToInt32(x);

                    Vector3 p = lp + (rp - lp) * (x - lp.X) / (rp.X - lp.X);

                    double Z = p.Z;

                    if (_zbuffer[ind] > Z)
                    {
                        _zbuffer[ind] = Z;

                        //lambert
                        //no need to put it inside a cycle but easier to find
                        //currAlpha = (byte)(255 - intensivity * _intensivityCoef * 255);
                        byte currRed = 156;
                        byte currGreen = 0;
                        byte currBlue = 56;
                        //phong
                        normal = n1 + (n2 - n1) * (x - lp.X) / (rp.X - lp.X);
                        normal = Vector3.Normalize(normal);


                        var diffuse = _lightningCounter.CountDiffuse(normal, _camera.LightPosition);

                        var specular = _lightningCounter.CountSpecular(normal, _camera.LightPosition, _camera.Position);

                        var colorCount = 255 * (Vector3.Normalize(ambient + diffuse + specular));


                        currRed = (byte)colorCount.X;
                        currGreen = (byte)colorCount.Y;
                        currBlue = (byte)colorCount.Z;

                        int point = ARGB * ind;
                        Color color = new Color(currAlpha, currRed, currGreen, currBlue);

                        _image[point + 0] = color.Blue;
                        _image[point + 1] = color.Green;
                        _image[point + 2] = color.Red;
                        _image[point + 3] = color.Alpha;
                    }

                }
            }
        }
        /*private void VerticesNormals()
        {
            _worldNormals = new Vector3[_worldCoordinates.Length];
            Parallel.ForEach(Partitioner.Create(0, _worldCoordinates.Length), range =>
            {
                for (int j = range.Item1; j < range.Item2; j++)
                {
                    Vector3 sumNormals = new Vector3();
                    for (int i = 0; i < _modelCoordinates.VertexTriangles[j].Count; i++)
                    {
                        var currTriangle = _modelCoordinates.Triangles[i];

                        Vector4[] triangleVertices = new Vector4[currTriangle.Length];

                        TriangleToCoordinates(_worldCoordinates, currTriangle, ref triangleVertices);

                        Vector3 t0 = new Vector3
                        {
                            X = triangleVertices[0].X,
                            Y = triangleVertices[0].Y,
                            Z = triangleVertices[0].Z
                        };

                        Vector3 t1 = new Vector3
                        {
                            X = triangleVertices[1].X,
                            Y = triangleVertices[1].Y,
                            Z = triangleVertices[1].Z
                        };
                        Vector3 t2 = new Vector3
                        {
                            X = triangleVertices[2].X,
                            Y = triangleVertices[2].Y,
                            Z = triangleVertices[2].Z
                        };

                        var normal = Vector3.Normalize(Vector3.Cross(t2 - t0, t1 - t0));

                        sumNormals += normal;

                    }

                    _worldNormals[j] = Vector3.Normalize(sumNormals);
                }
            });

        }*/

        private void TriangleRasterization()
        {

            Parallel.ForEach(Partitioner.Create(0, _modelCoordinates.Triangles.Length), (Action<Tuple<int, int>>)(range =>
            {
                for (int j = range.Item1; j < range.Item2; j++)
                {
                    DrawKraskouski(_modelCoordinates.Triangles[j]);
                }
            }));
        }

    }
}
