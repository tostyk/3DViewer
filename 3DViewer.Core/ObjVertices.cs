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

        //to count normalsormals
        public Vector3[] VerticesNormals = Array.Empty<Vector3>();

        public void ParseObj(MemoryStream stream)
        {
            List<int[]> polygons = new();
            List<Vector4> vertices = new();
            List<Vector3> textureVertices = new();
            List<Vector3> normal = new();
            Dictionary<int, List<int>> vertexNormals = new();

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
                        int vertex = int.Parse(v1.First()) - 1;
                        polygon.Add(vertex);
                        if(v1.Count() > 1)
                        {
                            int vertexNormal = int.Parse(v1.Last()) - 1;
                            if(!vertexNormals.ContainsKey(vertex))
                            {
                                vertexNormals.Add(vertex, new());
                            }
                            if (!vertexNormals[vertex].Contains(vertexNormal))
                            {
                                vertexNormals[vertex].Add(vertexNormal);
                            }
                        }
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
            
            //must be set before calling CountTotalNormal
            Normals = normal.ToArray();
            
            Polygons = polygons.ToArray();

            VerticesNormals = new Vector3[Vertices.Length];

            foreach (var normals in vertexNormals)
            {
                Vector3 totalNormal = CountTotalNormal(normals.Value);
                VerticesNormals[normals.Key] = totalNormal;
            }
        }

        private Vector3 CountTotalNormal(IEnumerable<int> normalsNums)
        {
            Vector3 totalNormal = new Vector3();
            List<Vector3> normals = new List<Vector3>();
            foreach(var n in normalsNums)
            {
                normals.Add(Normals[n]);
            }

            foreach(Vector3 normal in normals)
            {
                totalNormal += Vector3.Normalize(normal);
            }

            totalNormal = Vector3.Normalize(totalNormal);

            return totalNormal;
        }

        public void SeparateTriangles()
        {
            List<int[]> triangles = new List<int[]>();


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
        }
    }
}
