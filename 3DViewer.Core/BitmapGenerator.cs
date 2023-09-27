using System.Numerics;

namespace _3DViewer.Core
{
    public class BitmapGenerator
    {
        public const int ARGB = 4;
        private readonly ObjVertices _modelCoordinates;
        private readonly ObjVertices _currCoordinates;
        public readonly int Width;
        public readonly int Height;
        public Vector3 up = new(0, 1, 0);
        public Vector3 target = new(0, 0, 0);
        public Vector3 Camera = new(0, 1, -1000);
        private Color BackgroundColor = new(0, 0, 0, 0);

        public float scale = 0.5f;

        public float xmin = -20f;
        public float ymin = -20f;

        public float zfar = 20.0f;
        public float znear = 1.0f;
        public float aspect = 2.0f;
        public float FOV = (float)(Math.PI / 4);


        private Matrix4x4 currViewport;
        private Matrix4x4 currProjection;
        private Matrix4x4 currView;
        private Matrix4x4 currModel;

        private float _side;

        private readonly byte[,,] _image;


        public BitmapGenerator(
            ObjVertices modelCoordinates, 
            int width, 
            int height
            )
        {
            _modelCoordinates = modelCoordinates;
            _image = new byte[height, width, ARGB];
            Width = width;
            Height = height;

            _side = Math.Min( Width, Height ) / 2;

            ymin = -_side/2;
            xmin = -_side/2;

            zfar = _side;

            target = new(_side, _side, -_side);

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

        public byte[,,] GenerateImage()

        {
            ClearImage();

            Matrix4x4 resultMatrix =
                currViewport *
                currProjection *
                currView *
                currModel;
            RecountCoordinates(resultMatrix, _modelCoordinates.Vertices);

            DrawPolygons();

            return _image;
        }

        public byte[,,] ChangeCameraPosition(float deltaX, float deltaY, float deltaZ)
        {
            Camera.X += deltaX;
            Camera.Y += deltaY;
            Camera.Z += deltaZ;
            ClearImage();

            currViewport = Viewport();
            currView = View();
            currProjection = Projection();

            Matrix4x4 resultMatrix = 
                currViewport *
                currProjection *
                currView *
                currModel;

            RecountCoordinates(resultMatrix, _modelCoordinates.Vertices);

            DrawPolygons();
            return _image;
        }

        private void RecountCoordinates(Matrix4x4 matrix, Vector3[] baseVertices)
        {
            for (int i = 0; i < baseVertices.Length; i++)
            {
                Vector4 v4 = Vector4.Transform(baseVertices[i], matrix);
                //Vector4 v4 = Vector4.Transform(new Vector4(baseVertices[i], 1), matrix);
                //v4 /= v4.W;
                _currCoordinates.Vertices[i].X = v4.X;
                _currCoordinates.Vertices[i].Y = v4.Y;
                _currCoordinates.Vertices[i].Z = v4.Z;
            }

        }

        private Matrix4x4 Model()
        {
            Matrix4x4 resultMatrix = Matrix4x4.Identity;
            resultMatrix *= Matrix4x4.CreateScale(scale);
            return resultMatrix;
        }

        public byte[,,] Translate(float translationX, float translationY, float translationZ)
        {
            ClearImage();
            var translation = new Vector3(translationX, translationY, translationZ);
            Matrix4x4 resultMatrix = Matrix4x4.Identity;
            resultMatrix *= Matrix4x4.CreateTranslation(translation);

            currModel *= resultMatrix;

            RecountCoordinates(resultMatrix, _currCoordinates.Vertices);
            DrawPolygons();
            return _image;
        }
        public byte[,,] Rotate(
            float rotationX,
            float rotationY,
            float rotationZ
            )
        {
            ClearImage();
            Matrix4x4 resultMatrix = Matrix4x4.Identity;
            resultMatrix *= Matrix4x4.CreateRotationX(rotationX);
            resultMatrix *= Matrix4x4.CreateRotationY(rotationY);
            resultMatrix *= Matrix4x4.CreateRotationZ(rotationZ);

            currModel *= resultMatrix;

            RecountCoordinates(resultMatrix, _currCoordinates.Vertices);

            DrawPolygons();
            return _image;
        }

        public byte[,,] Scale(float scale)
        {
            ClearImage();
            this.scale = scale;
            Matrix4x4 resultMatrix = Matrix4x4.Identity;
            resultMatrix *= Matrix4x4.CreateScale(scale);

            currModel *= resultMatrix;

            RecountCoordinates(resultMatrix, _currCoordinates.Vertices);

            DrawPolygons();
            return _image;
        }

        private Matrix4x4 View()
        {
            return Matrix4x4.CreateLookAt(Camera, target, up);
        }
        private Matrix4x4 Projection()
        {
            return new Matrix4x4
            {
                M11 = 1 / (aspect * (float)Math.Tan(FOV / 2)),
                M22 = 1 / (float)Math.Tan(FOV / 2),
                M33 = zfar / (znear - zfar),

                M43 = -1,

                M34 = znear * zfar / (znear - zfar),
            };
        }
        private Matrix4x4 Viewport()
        {
           
            return new Matrix4x4
            {
                M11 = _side / 2f,
                M22 = -_side / 2f,
                M33 = 1,
                M44 = 1,

                M14 = xmin + _side / 2f,
                M24 = ymin + _side / 2f,
            };
        }

        private void ClearImage()
        {
            for (int i = 0; i < _image.GetLength(0); i++)
            {
                for (int j = 0; j < _image.GetLength(1); j++)
                {
                    _image[i, j, 0] = BackgroundColor.Alpha;
                    _image[i, j, 1] = BackgroundColor.Red;
                    _image[i, j, 2] = BackgroundColor.Green;
                    _image[i, j, 3] = BackgroundColor.Blue;
                }
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
                    X < Width &&
                    Y < Height)
                {
                    _image[Y, X, 0] = 255;
                    _image[Y, X, 1] = 255;
                    _image[Y, X, 2] = 255;
                    _image[Y, X, 3] = 255;
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
