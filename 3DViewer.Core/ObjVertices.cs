using System.Numerics;

namespace _3DViewer.Core
{
    public class ObjVertices
    {
        public Vector3[] Vertices = Array.Empty<Vector3>();
        public Vector3[] TextureVertices = Array.Empty<Vector3>();
        public Vector3[] Normals = Array.Empty<Vector3>();
        public int[][] Polygons = Array.Empty<int[]>();

        public void ParseObj(string filePath)
        {
            List<int[]> polygons = new();
            List<Vector3> vertices = new();
            List<Vector3> textureVertices = new();
            List<Vector3> normal = new();

            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                if (line.Trim() == "") continue;

                IEnumerable<string> elements = line.Split(' ');
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
                        polygon.Add(int.Parse(v1.First()));
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
                            vertices.Add(new Vector3(vx[0], vx[1], vx[2]));
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
    }
}
