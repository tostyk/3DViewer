using System;
using System.Collections.Concurrent;
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

        private Color BackgroundColor = new(255, 11, 14, 89);
        private Color DiffuseColor = new(255, 85, 10, 10);
        private Color SpecularColor = new(255, 255, 255, 255);

        private Vector3 _lightPosition;

        private LightningCounter _lightningCounter;

        public BitmapGenerator(
            ObjVertices modelCoordinates,
            int width,
            int height
            )
        {
            _lightningCounter = new LightningCounter(
                BackgroundColor,
                DiffuseColor,
                SpecularColor
                );
            _camera = new Camera();
            _lightPosition = _camera.Position;
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
                    _worldCoordinates[i] = Vector4.Transform(_modelCoordinates.Vertices[i], currModel);

                    _windowCoordinates[i] /= _windowCoordinates[i].W;
                    _windowCoordinates[i] = Vector4.Transform(_windowCoordinates[i], currViewport);
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
        private void Swap(ref Vector3 v1, ref Vector3 v2)
        {
            var temp = v2;
            v2 = v1;
            v1 = temp;
        }

        private void TriangleToCoordinates(Vector4[] triangle, int[] t, ref Vector3[] triangleCoords)
        {
            triangleCoords[0] = new Vector3
            {
                X = triangle[t[0]].X,
                Y = triangle[t[0]].Y,
                Z = triangle[t[0]].Z
            };
            triangleCoords[1] = new Vector3
            {
                X = triangle[t[1]].X,
                Y = triangle[t[1]].Y,
                Z = triangle[t[1]].Z

            };
            triangleCoords[2] = new Vector3
            {
                X = triangle[t[2]].X,
                Y = triangle[t[2]].Y,
                Z = triangle[t[2]].Z,
            };
        }

        private void DrawTriangle(int[] vertices, Vector3[] triangleVertices, Vector3[] intensTriangleVertices, Vector3 lightPos)
        {
            //don't remove poor lambert
            //notice that in reality it works almost well only when lightPos = _camera.Position
            /*    
                float intensivity = LightningCounter.Lambert(intensTriangleVertices, lightPos);
                if (intensivity < 0)
                {
                    return;
                }
                byte currAlpha = (byte)(255 - intensivity * _intensivityCoef * 255);

             */

            byte currRed = 0;
            byte currGreen = 0;
            byte currBlue = 0;
            byte currAlpha = 255;

            Vector3 colorCount = new Vector3(1, 1, 1);

            var ambient = _lightningCounter.CountAmbient();

            if (triangleVertices[0].Y > triangleVertices[1].Y)
                Swap(ref triangleVertices[0], ref triangleVertices[1]);
            if (triangleVertices[0].Y > triangleVertices[2].Y)
                Swap(ref triangleVertices[0], ref triangleVertices[2]);
            if (triangleVertices[1].Y > triangleVertices[2].Y)
                Swap(ref triangleVertices[1], ref triangleVertices[2]);

            float totalHeight = (triangleVertices[2].Y - triangleVertices[0].Y + 1);

            //here starts the suffering
            //    float totalHeightIntense = (intensTriangleVertices[2].Y - intensTriangleVertices[0].Y + 1);
            //

            for (float i = triangleVertices[0].Y; i <= totalHeight + triangleVertices[0].Y; i++)
            {
                bool second_half = i > triangleVertices[1].Y
                                    || triangleVertices[1].Y == triangleVertices[0].Y;


                Vector3 finish_vertice = second_half ?
                triangleVertices[2] : triangleVertices[1];
                Vector3 start_vertice = second_half ?
                triangleVertices[1] : triangleVertices[0];

                float segment_height = (finish_vertice.Y - start_vertice.Y + 1);

                Vector3 A = triangleVertices[0] + (triangleVertices[2] - triangleVertices[0]) * (i - triangleVertices[0].Y) / totalHeight;
                Vector3 B = start_vertice + (finish_vertice - start_vertice) * (i - start_vertice.Y) / segment_height;

                /* 
                 
                bool second_half_intence = i > intensTriangleVertices[1].Y
                                    || intensTriangleVertices[1].Y == intensTriangleVertices[0].Y;

                Vector3 finish_vertice_intense = second_half_intence ?
                intensTriangleVertices[2] : intensTriangleVertices[1];
                Vector3 start_vertice_intense = second_half_intence ?
                intensTriangleVertices[1] : intensTriangleVertices[0];

                  int finish_vertice_intense_n = second_half ?
                  2 : 1;
                  int start_vertice_intense_n = second_half ?
                  1 : 0;
                
                  
                float segment_height_intense = (finish_vertice_intense.Y - start_vertice_intense.Y + 1);  
                  
                Vector3 AIntense = intensTriangleVertices[0] + (intensTriangleVertices[2] - intensTriangleVertices[0]) * (i - intensTriangleVertices[0].Y) / totalHeightIntense;
                Vector3 BIntense = start_vertice_intense + (finish_vertice_intense - start_vertice_intense) * (i - start_vertice_intense.Y) / segment_height_intense;


                var u = Vector3.Distance(triangleVertices[0], A)
                    / Vector3.Distance(triangleVertices[2], triangleVertices[0]);

                var w = Vector3.Distance(start_vertice_intense, BIntense)
                    / Vector3.Distance(finish_vertice, start_vertice);


                var normal1 = u * _worldNormals[vertices[0]] + (1 - u) * _worldNormals[vertices[2]];
                var normal2 = w * _worldNormals[vertices[start_vertice_intense_n]]
                    + (1 - w) * _worldNormals[vertices[finish_vertice_intense_n]];

                normal1 = Vector3.Normalize(normal1);
                normal2 = Vector3.Normalize(normal2);*/

                var normal = Vector3.Normalize(
                    Vector3.Cross(
                    intensTriangleVertices[2] - intensTriangleVertices[0],
                    intensTriangleVertices[1] - intensTriangleVertices[0]
                    ));

                if (A.X > B.X)
                {
                    Swap(ref A, ref B);
                }

                for (float x = (A.X); x <= (B.X); x++)
                {
                    if (Convert.ToInt32(x) < _width && Convert.ToInt32(i) < _height && Convert.ToInt32(x) >= 0 && Convert.ToInt32(i) >= 0)
                    {
                        int ind = Convert.ToInt32(i) * _width + Convert.ToInt32(x);

                        Vector3 p = A + (B - A) * (x - A.X) / (B.X - A.X);
                        /* 
                        Vector3 pInt = AIntense + (BIntense - AIntense) * (x - A.X) / (BIntense.X - AIntense.X);
                        
                        var t = Vector3.Distance(p, B)/ Vector3.Distance(A, B);
                        normal = t * normal2 +  (1 - t) * normal1;
                        */

                        normal = Vector3.Normalize(normal);

                        if (_zbuffer[ind] > p.Z)
                        {
                            _zbuffer[ind] = p.Z;
                            int point = ARGB * ind;

                            var diffuse = _lightningCounter.CountDiffuse(
                               normal,
                                -lightPos);

                            var specular = _lightningCounter.CountSpecular(
                                normal,
                                 -lightPos,
                                _camera.Position
                                );

                            colorCount = 255 * (Vector3.Normalize(ambient + diffuse + specular));

                            currRed = (byte)colorCount.X;
                            currGreen = (byte)colorCount.Y;
                            currBlue = (byte)colorCount.Z;

                            Color color = new Color(currAlpha, currRed, currGreen, currBlue);

                            _image[point + 0] = color.Blue;
                            _image[point + 1] = color.Green;
                            _image[point + 2] = color.Red;
                            _image[point + 3] = color.Alpha;

                        }
                    }
                }
            }

        }

        private void VerticesNormals()
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

                        Vector3[] triangleVertices = new Vector3[currTriangle.Length];

                        TriangleToCoordinates(_worldCoordinates, currTriangle, ref triangleVertices);

                        if (triangleVertices[0].Y > triangleVertices[1].Y)
                            Swap(ref triangleVertices[0], ref triangleVertices[1]);
                        if (triangleVertices[0].Y > triangleVertices[2].Y)
                            Swap(ref triangleVertices[0], ref triangleVertices[2]);
                        if (triangleVertices[1].Y > triangleVertices[2].Y)
                            Swap(ref triangleVertices[1], ref triangleVertices[2]);


                        var normal = Vector3.Normalize(
                             Vector3.Cross(
                                 triangleVertices[2] - triangleVertices[0],
                                 triangleVertices[1] - triangleVertices[0])
                             );

                        sumNormals += normal;

                    }

                    _worldNormals[j] = Vector3.Normalize(sumNormals / _modelCoordinates.VertexTriangles[j].Count);
                }
            });

        }

        private void TriangleRasterization()
        {
            VerticesNormals();
            Parallel.ForEach(Partitioner.Create(0, _modelCoordinates.Triangles.Length), (Action<Tuple<int, int>>)(range =>
            {
                for (int j = range.Item1; j < range.Item2; j++)
                {
                    int[] triangleVertices = _modelCoordinates.Triangles[j];
                    Vector3[] triangleCoordinates = new Vector3[triangleVertices.Length];
                    Vector3[] intensTriangleVertices = new Vector3[triangleVertices.Length];

                    TriangleToCoordinates(_windowCoordinates, triangleVertices, ref triangleCoordinates);
                    TriangleToCoordinates(_worldCoordinates, triangleVertices, ref intensTriangleVertices);

                    if (triangleCoordinates[0].Y == triangleCoordinates[1].Y
                    && triangleCoordinates[0].Y == triangleCoordinates[2].Y) return;

                    _lightPosition = new Vector3(-0.3f, 0.5f, 1);
                    DrawTriangle(triangleVertices, triangleCoordinates, intensTriangleVertices, _lightPosition);

                }
            }));
        }

    }
}
