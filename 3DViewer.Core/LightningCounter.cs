using System.Numerics;
using System.Runtime.Intrinsics;

namespace _3DViewer.Core
{
    public static class LightningCounter
    {

        public static readonly float kA = 0.05f;
        public static readonly float kD = 0.5f;
        public static readonly float kS = 0.15f;

        public static Vector3 ColorVector3(Vector3 color)
        {
            float a = 2.51f;
            float b = 0.03f;
            float c = 2.43f;
            float d = 0.59f;
            float e = 0.14f;

            Vector3 vector = new Vector3(color.X, color.Y, color.Z);

            vector.X = Math.Clamp((vector.X * (a * vector.X + b)) / (vector.X * (c * vector.X + d) + e), 0.0f, 1.0f);
            vector.Y = Math.Clamp((vector.Y * (a * vector.Y + b)) / (vector.Y * (c * vector.Y + d) + e), 0.0f, 1.0f);
            vector.Z = Math.Clamp((vector.Z * (a * vector.Z + b)) / (vector.Z * (c * vector.Z + d) + e), 0.0f, 1.0f);

            // gamma correction
            vector.X = (float)Math.Pow(vector.X, 1 / 2.2);
            vector.Y = (float)Math.Pow(vector.Y, 1 / 2.2);
            vector.Z = (float)Math.Pow(vector.Z, 1 / 2.2);

            return vector;
        }
        public static float Lambert(Vector3 n, Vector3 lightningPos)
        {
            Vector3 normalCamera = Vector3.Normalize(lightningPos);

            return Vector3.Dot(normalCamera, n);
        }

        public static Vector3 CountAmbient(Vector3 AmbientAlbedo)
        {
            return kA * AmbientAlbedo;
        }

        public static Vector3 CountDiffuse(Vector3 N, Vector3 L, Vector3 DiffuseAlbedo)
        {
            N = Vector3.Normalize(N);
            L = Vector3.Normalize(L);

            return kD *  Math.Max(Vector3.Dot(N, L), 0.0f) * DiffuseAlbedo;
        }
        public static Vector3 CountSpecular(Vector3 N, Vector3 L, Vector3 V, Vector3 SpecularAlbedo, float SpecularPower)
        {
            N = Vector3.Normalize(N);
            L = Vector3.Normalize(L);
            V = Vector3.Normalize(V);

            Vector3 R = L - 2 * Vector3.Dot(L, N) * N;

            return kS * (float)Math.Pow(Math.Max(Vector3.Dot(R, V), 0.0f), SpecularPower) * SpecularAlbedo;
        }
    }
}
