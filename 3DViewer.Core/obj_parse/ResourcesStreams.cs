namespace _3DViewer.Core.obj_parse
{
    public class ResourcesStreams
    {
        public string basePath;
        public ResourcesStreams(string basePath)
        {
            this.basePath = basePath;
        }
        public ObjVertices GetVertices()
        {
            ObjVertices vertices = new();
            string[] paths = Directory.GetFiles(basePath, "*.obj", SearchOption.TopDirectoryOnly);
            if(paths.Length > 0)
            {
                using (FileStream stream = File.OpenRead(paths[0]))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    vertices.ParseObj(stream);
                }
            }
            return vertices;
        }
        public MtlInformation GetMtlInformation(ObjVertices obj)
        {
            MtlInformation mtlInformation = new();
            string mtlPath = Path.Combine(basePath, obj.mtllib);
            if(File.Exists(mtlPath))
            {
                mtlInformation = new MtlInformation();
                using (FileStream stream = File.OpenRead(mtlPath))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    mtlInformation.ParseMtl(stream);
                }

            }
            return mtlInformation;
        }
    }
}
