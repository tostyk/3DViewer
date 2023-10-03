using System.Numerics;

namespace _3DViewer.Core
{
    public class BitmapGenerator
    {
        public const int ARGB = 4;
        public const float ScaleSize = 10;
        private readonly ObjVertices _modelCoordinates;
        private readonly ObjVertices _currCoordinates;
        private readonly int _width;
        private readonly int _height;
        private Vector3 up = new(0, 1, 0);
        private Vector3 target = new(0, 0, 0);
        private Vector3 _camera = new(0, 0, ScaleSize * 5);
        private Quaternion _rotationQuaternion = new(0, 0, 0, 1);

        private Matrix4x4 _normalizationMatrix = Matrix4x4.Identity;
        private Color BackgroundColor = new(255, 255, 255, 255);

        private float zfar = 100f;
        private float znear = 0.01f;
        private float _aspect;
        public const float FOV = (float)(Math.PI / 4); // 45deg Yaxis, 90deg Xaxis

        private Matrix4x4 currViewport;
        private Matrix4x4 currProjection;
        private Matrix4x4 currView;
        private Matrix4x4 currModel;

        private readonly byte[] _image;

        public BitmapGenerator(
            ObjVertices modelCoordinates,
            int width,
            int height
            )
        {
            _image = new byte[height * width * ARGB];
            _width = width;
            _height = height;

            _modelCoordinates = modelCoordinates;
            _normalizationMatrix = Normalize();

            _aspect = _width / _height;

            _currCoordinates = new ObjVertices
            {
                Polygons = _modelCoordinates.Polygons,
                Normals = _modelCoordinates.Normals,
                TextureVertices = _modelCoordinates.TextureVertices,
                Vertices = new Vector3[_modelCoordinates.Vertices.Length]
            };

            currViewport = Viewport();
            currProjection = Projection();
            currView = View();
            currModel = Model();
        }

        private Matrix4x4 Normalize()
        {
            Matrix4x4 matrix = Matrix4x4.Identity;
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
            Vector3 scaleVector = (max - min) / 2;
            float scale = Math.Max(Math.Max(scaleVector.X, scaleVector.Y), scaleVector.Z);

            matrix.Translation = -avg;
            matrix *= Matrix4x4.CreateScale(1f / scale * 10);
            return matrix;
        }

        public byte[] GenerateImage()
        {
            ClearImage();

            Matrix4x4 resultMatrix =
                currModel *
                currView *
                currProjection *
                currViewport;
            RecountCoordinates(resultMatrix, _modelCoordinates.Vertices);

            DrawPolygons();

            return _image;
        }

        public void ChangeCameraPosition(float deltaX, float deltaY, float deltaZ)
        {
            currView = View();
        }

        private void RecountCoordinates(Matrix4x4 matrix, Vector3[] baseVertices)
        {
            for (int i = 0; i < baseVertices.Length; i++)
            {
                Vector4 v4 = Vector4.Transform(baseVertices[i], matrix);
                _currCoordinates.Vertices[i].X = v4.X / v4.W;
                _currCoordinates.Vertices[i].Y = v4.Y / v4.W;
                _currCoordinates.Vertices[i].Z = v4.Z / v4.W;
            }
        }

        private Matrix4x4 Model()
        {
            Matrix4x4 resultMatrix = Matrix4x4.Identity;
            resultMatrix *= _normalizationMatrix;
            resultMatrix *= Matrix4x4.CreateFromQuaternion(_rotationQuaternion);
            return resultMatrix;
        }

        public void ReplaceCameraByScreenCoordinates(
            float x0,
            float y0,
            float x1,
            float y1
            )
        {
            float dx = (x1 - x0);
            float dy = (y1 - y0);

            Vector3 delta = new(-dx, dy, 0);

            float angle = delta.Length() / 100;

            Vector3 rotAxis = Vector3.Normalize(
                Vector3.Cross(
                    new Vector3(_camera.X, _camera.Y, _camera.Z),
                    delta)
                );

            _rotationQuaternion = Quaternion.CreateFromAxisAngle(rotAxis, angle) * _rotationQuaternion;

            currModel = Model();
        }

        public void Scale(float scale)
        {
            if (_camera.Z - scale > ScaleSize * Math.Sqrt(3))
            {
                _camera.Z -= scale;
                currView = View();
            }
        }

        private Matrix4x4 View()
        {
            Matrix4x4 resultMatrix = Matrix4x4.CreateLookAt(_camera, target, up);
            return resultMatrix;
        }
        private Matrix4x4 Projection()
        {
            return Matrix4x4.CreatePerspectiveFieldOfView(FOV, _aspect, znear, zfar);
        }
        private Matrix4x4 Viewport()
        {
            Matrix4x4 matrix = new()
            {
                M11 = _width / 2f,
                M22 = -_height / 2f,
                M33 = 1,
                M44 = 1,

                M41 = _width / 2f,
                M42 = _height / 2f,
            };
            return matrix;
        }

        private void ClearImage()
        {
            for (int i = 0; i < _image.Length / 4; i++)
            {
                _image[i * 4 + 0] = BackgroundColor.Blue;
                _image[i * 4 + 1] = BackgroundColor.Green;
                _image[i * 4 + 2] = BackgroundColor.Red;
                _image[i * 4 + 3] = BackgroundColor.Alpha;
            }
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
            _currCoordinates.Polygons
                .AsParallel()
                .ForAll(polygon =>
            {
                for (int i = 0; i < polygon.Length; i++)
                {
                    DrawLine(
                            _currCoordinates.Vertices[polygon[i] - 1].X,
                            _currCoordinates.Vertices[polygon[(i + 1) % polygon.Length] - 1].X,
                            _currCoordinates.Vertices[polygon[i] - 1].Y,
                            _currCoordinates.Vertices[polygon[(i + 1) % polygon.Length] - 1].Y
                            );
                }
            });
            //foreach (var polygon in _currCoordinates.Polygons)
            //{
            //    for (int i = 0; i < polygon.Length; i++)
            //    {
            //        DrawLine(
            //                _currCoordinates.Vertices[polygon[i] - 1].X,
            //                _currCoordinates.Vertices[polygon[(i + 1) % polygon.Length] - 1].X,
            //                _currCoordinates.Vertices[polygon[i] - 1].Y,
            //                _currCoordinates.Vertices[polygon[(i + 1) % polygon.Length] - 1].Y
            //                );
            //    }
            //}
        }
    }
}
