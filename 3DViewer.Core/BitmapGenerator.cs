using System.Collections.Concurrent;
using System.Numerics;
using System.Collections.Concurrent;
using System.Reflection;


namespace _3DViewer.Core
{
    public class BitmapGenerator
    {
        public const int ARGB = 4;
        public const float ScaleSize = 10;
        private readonly ObjVertices _modelCoordinates;
        private readonly ObjVertices _currCoordinates;
        private int _width;
        private int _height;
        private Vector3 _up = new(0, 1, 0);
        private Vector3 _target = new(0, 0, 0);
        private Vector3 _camera = new(0, 0, 1);
        private Vector3 _cameraStart = new(0, 0, 1);
        private Matrix4x4 _normalizationMatrix = Matrix4x4.Identity;
        private Color BackgroundColor = new(255, 255, 255, 255);
        private float zfar = 100f;
        private float znear = 0.01f;
        private float _radius;
        private float minDepth = 0f;
        private float maxDepth = 1f;
        private float minX = 0f;
        private float minY = 0f;
        public float FOV = (float)(Math.PI / 4); // 45deg Yaxis, 90deg Xaxis
        private Matrix4x4 currViewport;
        private Matrix4x4 currProjection;
        private Matrix4x4 currView;
        private Matrix4x4 currModel;
        private float _cameraSensetivity = 10f;
        float _pitch = 0f;
        float _yaw = 0f;
        float _roll = 0f;
        private byte[] _image;

        public BitmapGenerator(
            ObjVertices modelCoordinates,
            int width,
            int height
            )
        {
            _width = width;
            _height = height;

            _modelCoordinates = modelCoordinates;
            _normalizationMatrix = Normalize();

            _currCoordinates = new ObjVertices
            {
                Polygons = _modelCoordinates.Polygons,
                Normals = _modelCoordinates.Normals,
                TextureVertices = _modelCoordinates.TextureVertices,
                Vertices = new Vector4[_modelCoordinates.Vertices.Length]
            };

            Resized(width, height);
        }

        public void Resized(
            int width,
            int height)
        {
            _image = new byte[height * width * ARGB];
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

            _radius = scale * Vector3.Distance(avg, min);
            float hFov = 2 * (float)Math.Atan(Math.Tan(FOV / 2) * ((float)_width / _height));

            _camera.Z = Math.Max(znear + _radius, _radius / (float)Math.Sin(Math.Min(FOV / 2, hFov / 2)));
            
            _cameraStart = _camera;
            return matrix;
        }
        public byte[] GenerateImage()
        {
            ClearImage();
            RecountCoordinates(_modelCoordinates.Vertices);

            DrawPolygons();

            return _image;
        }

        public void ChangeCameraPosition(float deltaX, float deltaY, float deltaZ)
        {
            currView = View();
        }

        private void RecountCoordinates(Vector4[] baseVertices)
        {
            Matrix4x4 modelViewProjectionMatrix =
                currModel *
                currView *
                currProjection;

            Vector4[] windowVertices = new Vector4[baseVertices.Length];

            Parallel.ForEach(Partitioner.Create(0, baseVertices.Length), range =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    _currCoordinates.Vertices[i] = Vector4.Transform(baseVertices[i], modelViewProjectionMatrix);
                    _currCoordinates.Vertices[i] /= _currCoordinates.Vertices[i].W;
                    _currCoordinates.Vertices[i] = Vector4.Transform(_currCoordinates.Vertices[i], currViewport);
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
            float dx = (x1 - x0) / _width * _cameraSensetivity;
            float dy = (y1 - y0) / _height * _cameraSensetivity;

            _pitch += dy;
            _yaw += dx;

            _pitch = (float)Math.Clamp(_pitch, -Math.PI / 2 + 0.1f, Math.PI / 2 - 0.1f); ;

            Matrix4x4 rotation = Matrix4x4.CreateFromYawPitchRoll(_yaw, _pitch, _roll);
            _camera = Vector3.Transform(_cameraStart, rotation);

            currView = View();
        }

        public void Scale(float scale)
        {
            FOV = (float)Math.Clamp(FOV - scale, 0.1f, Math.PI / 2);
            currProjection = Projection();
        }

        private Matrix4x4 View()
        {
            Matrix4x4 resultMatrix = Matrix4x4.CreateLookAt(_camera, _target, _up);
            return resultMatrix;
        }
        private Matrix4x4 Projection()
        {
            return Matrix4x4.CreatePerspectiveFieldOfView(FOV, (float)_width/_height, znear, zfar);
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
                }
            });
        }
        private void DrawLine(float x0, float x1, float y0, float y1)
        {
            int xDiff = Math.Abs((int)(x1 - x0));
            int yDiff = Math.Abs((int)(y1 - y0));
            int L = xDiff > yDiff ? xDiff : yDiff;

            float dx = (x1 - x0) / L;
            float dy = (y1 - y0) / L;


            for (int i = 0; i < L; i++)
            {
                int X = Convert.ToInt32(x0 + dx * i);
                int Y = Convert.ToInt32(y0 + dy * i);

                if (X >= 0 &&
                    Y >= 0 &&
                    X < _width &&
                    Y < _height)
                {
                    int point = Y * _width * ARGB + X * ARGB;
                    _image[point + 0] = 0;
                    _image[point + 1] = 0;
                    _image[point + 2] = 0;
                    _image[point + 3] = 255;
                }
            }
        }
        private void DrawPolygons()
        {
            Parallel.ForEach(Partitioner.Create(0, _currCoordinates.Polygons.Length), range =>
            {
                for (int j = range.Item1; j < range.Item2; j++)
                {
                    int[] p = _currCoordinates.Polygons[j];
                    for (int i = 0; i < p.Length; i++)
                    {
                        DrawLine(
                                _currCoordinates.Vertices[p[i] - 1].X,
                                _currCoordinates.Vertices[p[(i + 1) % p.Length] - 1].X,
                                _currCoordinates.Vertices[p[i] - 1].Y,
                                _currCoordinates.Vertices[p[(i + 1) % p.Length] - 1].Y
                                );
                    }
                }
            });
        }
    }
}
