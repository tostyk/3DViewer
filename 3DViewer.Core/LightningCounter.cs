using System.Numerics;
using System.Runtime.Intrinsics;

namespace _3DViewer.Core
{
    public class LightningCounter
    {
        public Vector3 DiffuseAlbedo;
        public Vector3 SpecularAlbedo;
        public Vector3 AmbientAlbedo;

        public Vector3 BloomBrightness = new Vector3 (0.2126f, 0.7152f, 0.0722f);

        public float[] weight = { 0.227027f, 0.1945946f, 0.1216216f, 0.054054f, 0.016216f };

        public float kA = 2.1f;
        public float kD = 1.5f;
        public float kS = 1.1f;

        public float SpecularPower = 10f;
       

        public Vector3 CountBloom(Vector3 color)
        {
            Vector3 vector3 = new Vector3(color.X, color.Y, color.Z);
            float dot = Vector3.Dot(vector3, BloomBrightness);
            if(dot <= 1.0)
            {
                vector3 = new Vector3(0, 0, 0);
            }
            return vector3;
        }
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
        public LightningCounter(Color ambient, Color diffuse, Color specular) 
        {
            

            AmbientAlbedo = (new Vector3(
                ambient.Red,
                ambient.Green,
                ambient.Blue
               ));

            DiffuseAlbedo = (new Vector3(
                diffuse.Red,
                diffuse.Green,
                diffuse.Blue
               ));

            SpecularAlbedo = (new Vector3(
                specular.Red,
                specular.Green,
                specular.Blue
               ));

            /*AmbientAlbedo.X /= (AmbientAlbedo.X + 1);
            AmbientAlbedo.Y /= (AmbientAlbedo.Y + 1);
            AmbientAlbedo.Z /= (AmbientAlbedo.Z + 1);


            DiffuseAlbedo.X /= (DiffuseAlbedo.X + 1);
            DiffuseAlbedo.Y /= (DiffuseAlbedo.Y + 1);
            DiffuseAlbedo.Z /= (DiffuseAlbedo.Z + 1);

            SpecularAlbedo.X /= (SpecularAlbedo.X + 1);
            SpecularAlbedo.Y /= (SpecularAlbedo.Y + 1);
            SpecularAlbedo.Z /= (SpecularAlbedo.Z + 1);*/

            AmbientAlbedo /= 255;
            DiffuseAlbedo /= 255;
            SpecularAlbedo /= 255;


        }
        public static float Lambert(Vector3 n, Vector3 lightningPos)
        {
            Vector3 normalCamera = Vector3.Normalize(lightningPos);

            return Vector3.Dot(normalCamera, n);
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
