using System.Numerics;

namespace _3DViewer.Core
{
    public class ObjVertices
    {
        public Vector4[] Vertices = Array.Empty<Vector4>();
        public Vector3[] TextureVertices = Array.Empty<Vector3>();
        public Vector3[] Normals = Array.Empty<Vector3>();
        public int[][] Polygons = Array.Empty<int[]>();
        public int[][] Triangles = Array.Empty<int[]>();

        //to count normal
        public List<int>[] VertexTriangles = Array.Empty<List<int>>(); 

        public void ParseObj(MemoryStream stream)
        {
            List<int[]> polygons = new();
            List<Vector4> vertices = new();
            List<Vector3> textureVertices = new();
            List<Vector3> normal = new();

            List<List<int>> vTrianglses= new();

            string[] lines;

            using (StreamReader reader = new StreamReader(stream))
            {
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                var a = reader.ReadToEnd();
                lines = a.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            }

            foreach (string line in lines)
            {
                if (line.Trim() == "") continue;

                IEnumerable<string> elements = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                if (!elements.Any()) continue;

                string character = elements.First();
                elements = elements.Skip(1);
                if (character == "f")
                {
                    var polygon = new List<int>();
                    foreach (string element in elements)
                    {
                        if (element == "") continue;
                        IEnumerable<string> v1 = element.Split('/');
                        polygon.Add(int.Parse(v1.First()) - 1);
                    }
                    polygons.Add(polygon.ToArray());
                }
                else if (character.StartsWith("v"))
                {
                    var vx = elements
                        .Select(x => float.Parse(x))
                        .ToArray();

                    switch (character)
                    {
                        case "v":
                            vertices.Add(new Vector4(vx[0], vx[1], vx[2], vx.Length > 3 ? vx[3] : 1));
                           break;
                        case "vn":
                            normal.Add(new Vector3(vx[0], vx[1], vx[2]));
                            break;
                        case "vt":
                            textureVertices.Add(new Vector3(vx[0], vx.Length > 1 ? vx[1] : 0, vx.Length > 2 ? vx[2] : 0));
                            break;
                    }
                }
            }

            Vertices = vertices.ToArray();
            TextureVertices = textureVertices.ToArray();
            Normals = normal.ToArray();
            Polygons = polygons.ToArray();
        }
        public void SeparateTriangles()
        {
            List<int[]> triangles = new List<int[]>();

            VertexTriangles = new List<int>[Vertices.Length];

            foreach (var polygon in Polygons)
            {
                for (int first = 0; (first + 1) < polygon.GetLength(0); first+=2)
                {
                    int[] triangle = new int[3];

                    triangle[0] = polygon[first]; 
                    triangle[1] = polygon[first + 1];
                    triangle[2] = polygon[(first + 2)%polygon.GetLength(0)];
                    triangles.Add(triangle);
                }
            }

            Triangles = triangles.ToArray();

            for(int i = 0; i < Triangles.Length; i++)
            {
                if (VertexTriangles[Triangles[i][0]] == null)
                {
                    VertexTriangles[Triangles[i][0]] = new();
                }
                if (VertexTriangles[Triangles[i][1]] == null)
                {
                    VertexTriangles[Triangles[i][1]] = new();
                }
                if (VertexTriangles[Triangles[i][2]] == null)
                {
                    VertexTriangles[Triangles[i][2]] = new();
                }

                VertexTriangles[Triangles[i][0]].Add(i);
                VertexTriangles[Triangles[i][1]].Add(i);
                VertexTriangles[Triangles[i][2]].Add(i);
            }
        }
    }
}
