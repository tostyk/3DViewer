using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace _3DViewer.Core
{
    public class Camera
    {
        public Vector3 Up;
        public Vector3 Target;
        public Vector3 Position;
        public Vector3 ViewerPosition;
        public Vector3 LightPosition;

        public float ZFar = 100f;
        public float ZNear = 1f;

        public float FOV = (float)(Math.PI / 4); // 45deg Yaxis, 90deg Xaxis

        public float CameraSensetivity = 10f;
        public float Pitch { get; private set; }
        public float Yaw { get; private set; }
        public float Roll { get; private set; }

        public Camera()
        {
            Up = new(0, 1, 0);
            Target = new(0, 0, 0);

            Position = new(0, 0, 0);
            ViewerPosition = Position;

            LightPosition = new (10, 1, 0);

            Pitch = 0f;
            Yaw = 0f;
            Roll = 0f;
        }

        public void Normalize(float radius, float aspectRatio)
        {
            float hFov = 2 * (float)Math.Atan(Math.Tan(FOV / 2) * aspectRatio);

            Position.Z = Math.Max(ZNear + radius, radius / (float)Math.Sin(Math.Min(FOV / 2, hFov / 2)));

            ViewerPosition = Position;
        }

        public void ReplaceCameraByScreenCoordinates(
            float dx,
            float dy
            )
        {
            dx *= CameraSensetivity;
            dy *= CameraSensetivity;

            Pitch += dy;
            Yaw += dx;
            Pitch = (float)Math.Clamp(Pitch, -Math.PI / 2 + 0.1f, Math.PI / 2 - 0.1f); ;

            Matrix4x4 rotation = Matrix4x4.CreateFromYawPitchRoll(Yaw, Pitch, Roll);
            Position = Vector3.Transform(ViewerPosition, rotation);
        }
    }
}
