using System.Numerics;

namespace _3DViewer.Core
{
    public class LightningCounter
    {
        public Vector3 DiffuseAlbedo;
        public Vector3 SpecularAlbedo;
        public Vector3 AmbientAlbedo;

        public float kA = 0.1f;
        public float kD = 0.1f;
        public float kS = 0.1f;

        public float SpecularPower = 10.0f;

        public LightningCounter(Color ambient, Color diffuse, Color specular) 
        {
            AmbientAlbedo = Vector3.Normalize(new Vector3(
                ambient.Red,
                ambient.Green,
                ambient.Blue
               ));

            DiffuseAlbedo = Vector3.Normalize(new Vector3(
                diffuse.Red,
                diffuse.Green,
                diffuse.Blue
               ));

            SpecularAlbedo = Vector3.Normalize(new Vector3(
                specular.Red,
                specular.Green,
                specular.Blue
               ));
        }
        public static float Lambert(Vector3 n, Vector3 lightningPos)
        {
            Vector3 normalCamera = Vector3.Normalize(lightningPos);

            return Vector3.Dot(normalCamera, n);
        }
        public Vector3 CountTotalIntensivity(Vector3 N, Vector3 L, Vector3 V)
        {
            N = Vector3.Normalize(N);
            L = Vector3.Normalize(L);
            V = Vector3.Normalize(V);

            Vector3 R = L - 2 * Vector3.Dot(L, N) * N;

            Vector3 Ia = kA * AmbientAlbedo;
            Vector3 Id = kD * Math.Max(Vector3.Dot(N, L), 0.0f) * DiffuseAlbedo;
            Vector3 Is = kS * (float)Math.Pow(Math.Max(Vector3.Dot(R, V), 0.0f), SpecularPower) * SpecularAlbedo;

            return Ia + Id + Is;
        }

        public Vector3 CountAmbient()
        {
            return kA * AmbientAlbedo;
        }

        public Vector3 CountDiffuse(Vector3 N, Vector3 L)
        {
            N = Vector3.Normalize(N);
            L = Vector3.Normalize(L);

            return kD *  Math.Max(Vector3.Dot(N, L), 0.0f) * DiffuseAlbedo;
        }
        public Vector3 CountSpecular(Vector3 N, Vector3 L, Vector3 V)
        {
            N = Vector3.Normalize(N);
            L = Vector3.Normalize(L);
            V = Vector3.Normalize(V);

            Vector3 R = L - 2 * Vector3.Dot(L, N) * N;

            return kS * (float)Math.Pow(Math.Max(Vector3.Dot(R, V), 0.0f), SpecularPower) * SpecularAlbedo;
        }
    }
}
