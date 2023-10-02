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
        private Vector3 _rotation;

        private Vector3 ToCameraVector(Vector3 sphereVector)
        {
            return new Vector3
            {
                X = (float)(sphereVector.Z * Math.Sin(sphereVector.Y) * Math.Cos(sphereVector.X)),
                Y = (float)(sphereVector.Z * Math.Sin(sphereVector.Y) * Math.Sin(sphereVector.X)),
                Z = (float)(sphereVector.Z * Math.Cos(sphereVector.Y)),
            };
        }
        private Vector3 ToSphereVector(Vector3 cameraVector)
        {
            return new Vector3
            {
                X = (float)Math.Atan(
                    cameraVector.X == 0 
                        ? double.PositiveInfinity 
                        : cameraVector.Y / cameraVector.X),
                Y = (float)Math.Atan(Math.Sqrt(cameraVector.X * cameraVector.X + cameraVector.Y * cameraVector.Y) / cameraVector.Z),
                Z = (float)Math.Sqrt(
                    cameraVector.X * cameraVector.X + 
                    cameraVector.Y * cameraVector.Y + 
                    cameraVector.Z * cameraVector.Z),
            };
        }

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
            resultMatrix *= Matrix4x4.CreateRotationX(_rotation.X);
            resultMatrix *= Matrix4x4.CreateRotationY(_rotation.Y);
            resultMatrix *= Matrix4x4.CreateRotationZ(_rotation.Z);
            return resultMatrix;
        }

        public void ReplaceCameraByScreenCoordinates(
            float x0, 
            float y0, 
            float x1, 
            float y1
            )
        {
            //double dy = _height / 2 / Math.Tan(FOV / 2);
            //double dx = _width / 2 / Math.Tan(FOV / 2 * _aspect);

            //Vector3 sphereVector = ToSphereVector(_camera);
            _rotation.Y += (x1 - x0) / 100;//(float)(Math.Atan((_width / 2 - x0) / dx) + Math.Atan((x1 - _width / 2) / dx)) * 5;
            _rotation.Z += (y1 - y0) / 100;//(floa)(Math.Atan((_height / 2 - y0) / dy) + Math.Atan((y1 - _height / 2) / dy)) * 5;
            //Quaternion

            //_camera = ToCameraVector(sphereVector);
            currModel = Model();
        }

        public void Scale(float scale)
        {

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
                M22 = - _height / 2f,
                M33 = 1,
                M44 = 1,

                M41 = _width / 2f,
                M42 = _height / 2f,
            };
            return matrix;
        }

        private void ClearImage()
        {
            for (int i = 0; i < _image.Length/4; i++)
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
                int X = Convert.ToInt32(x0 + dx*i);
                int Y = Convert.ToInt32(y0 + dy*i);

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
            foreach (var polygon in _currCoordinates.Polygons)
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
            }
        }
    }
}
