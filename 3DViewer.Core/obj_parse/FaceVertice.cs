using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3DViewer.Core.obj_parse
{
    public class Polygon
    {
        public List<FaceVertex> FaceVertices = new List<FaceVertex>();
    }
    public class FaceVertex
    {
        public int vertex;
        public int textureVertex;
        public int normalVertex;
    }
}
