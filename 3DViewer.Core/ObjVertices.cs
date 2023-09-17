using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace _3DViewer.Core
{
    public class ObjVertices
    {
        public List<Vector3> vertices = new List<Vector3>();
        public List<Vector3> textureVerticees = new List<Vector3>();
        public List<Vector3> normal = new List<Vector3>();
        public List<List<int>> polygons = new List<List<int>>();

        public void ParseObj(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                IEnumerable<string> elements = line.Split(' ');
                string character = elements.First();
                elements = elements.Skip(1);
                if(character == "f")
                {
                    var polygon = new List<int>();
                    foreach (string element in elements)
                    {
                        IEnumerable<string> vertices = element.Split('/');
                        polygon.Add(Int32.Parse(vertices.First()));
                    }
                    polygons.Add(polygon);
                }
                else if(character.StartsWith("v"))
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

                            textureVerticees.Add(new Vector3(vx[0], vx.Length > 1 ? vx[1] : 0, vx.Length > 2 ? vx[2] : 0));
                            break;

                    }
                }
            }
        }
    }
}
