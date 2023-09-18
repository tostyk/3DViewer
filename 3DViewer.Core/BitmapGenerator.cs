using System.Numerics;

namespace _3DViewer.Core
{
    public class BitmapGenerator
    {
        public const int ARGB = 4;
        private readonly ObjVertices _modelCoordinates;
        //private float scale = 300;
        public readonly int Width;
        public readonly int Height;
        public Vector3 Camera = new(0, 0, 1);
        private Color BackgroundColor = new(0, 0, 0, 0);//new(255, 255, 255, 255);

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
        }

        public byte[,,] GenerateImage(
            float rotationX = 0,
            float rotationY = 0,
            float rotationZ = 0,
            float scale = 0.5f
            )
        {
            ClearImage();

            ObjVertices objVertices = new()
            {
                Polygons = _modelCoordinates.Polygons,
                Normals = _modelCoordinates.Normals,
                TextureVertices = _modelCoordinates.TextureVertices,
                Vertices = new Vector3[_modelCoordinates.Vertices.Length]
            };

            Matrix4x4 resultMatrix =
                Viewport() *
                Projection() *
                View() *
                Model(rotationX, rotationY, rotationZ);

            for (int i = 0; i < _modelCoordinates.Vertices.Length; i++)
            {
                Vector4 v4 = Vector4.Transform(_modelCoordinates.Vertices[i], resultMatrix);
                objVertices.Vertices[i] = new Vector3(v4.X, v4.Y, v4.Z);
            }

            DrawPolygons(objVertices);

            return _image;
        }
        private Matrix4x4 Model(
            float rotationX,
            float rotationY,
            float rotationZ
            )
        {
            Matrix4x4 resultMatrix = Matrix4x4.Identity;

            resultMatrix *= Matrix4x4.CreateScale(1);
            resultMatrix *= Matrix4x4.CreateRotationX(rotationX);
            resultMatrix *= Matrix4x4.CreateRotationY(rotationY);
            resultMatrix *= Matrix4x4.CreateRotationZ(rotationZ);

            return resultMatrix;
        }
        private Matrix4x4 View()
        {
            Vector3 cameraPosition = Camera;
            Vector3 target = new(0,0,0);
            Vector3 up = new(0,1,0);
            var f =  Matrix4x4.CreateLookAt(cameraPosition, target, up);
            return f;
        }
        private Matrix4x4 Projection()
        {
            return Matrix4x4.CreateOrthographic(2, 2, 1, 5);
        }
        private Matrix4x4 Viewport()
        {
            return new Matrix4x4
            {
                // тут чисто подбором нормально получилось
                M11 = Math.Min(Width, Height) / 10,
                M22 = - Math.Min(Width, Height) / 10,

                M33 = 1,
                M44 = 1,
                M41 = Width / 2f,
                M42 = Height / 2f,
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
        private void DrawPolygons(ObjVertices objVertices)
        {
            foreach (var polygon in objVertices.Polygons)
            {
                for (int i = 0; i < polygon.Length; i++)
                {
                    DrawLine(
                         objVertices.Vertices[polygon[i] - 1].X,
                         objVertices.Vertices[polygon[(i + 1) % polygon.Length] - 1].X,
                         objVertices.Vertices[polygon[i] - 1].Y,
                         objVertices.Vertices[polygon[(i + 1) % polygon.Length] - 1].Y
                         );
                }
            }
        }
    }
}
