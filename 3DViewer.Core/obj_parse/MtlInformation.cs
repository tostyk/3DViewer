using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
namespace _3DViewer.Core.obj_parse
{
    public class MtlInformation
    {
        public MtlCharacter[] mtlCharacters = Array.Empty<MtlCharacter>();
        public void ParseMtl(Stream stream)
        {
            string[] lines;
            List<MtlCharacter> characters = new List<MtlCharacter>();

            using (StreamReader reader = new StreamReader(stream))
            {
                reader.BaseStream.Seek(0, SeekOrigin.Begin);
                var a = reader.ReadToEnd();
                lines = a.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                MtlCharacter? mtlCharacter = null;

                foreach (string line in lines)
                {
                    if (line.Trim() == "") continue;

                    IEnumerable<string> elements = line.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                    if (!elements.Any()) continue;

                    string character = elements.First();
                    elements = elements.Skip(1);
                    if (character == "newmtl")
                    {
                        if (mtlCharacter != null)
                        {
                            characters.Add(mtlCharacter);
                        }
                        mtlCharacter = new MtlCharacter
                        {
                            name = elements.ElementAt(0),
                        };
                    }
                    else
                    if (mtlCharacter != null)
                    {

                        if (character.StartsWith("K"))
                        {
                            var k = elements
                                .Select(x => float.Parse(x))
                                .ToArray();

                            Vector3 Kx = new Vector3(k[0], k[1], k[2]);

                            switch (character)
                            {
                                case "Kd":
                                    mtlCharacter.Kd = Kx;
                                    break;
                                case "Ka":
                                    mtlCharacter.Ka = Kx;
                                    break;
                                case "Ks":
                                    mtlCharacter.Ks = Kx;
                                    break;
                            }
                        }
                        else
                        if (character.StartsWith("map"))
                        {
                            switch (character)
                            {
                                case "map_Kd":
                                    mtlCharacter.mapKd = elements.ElementAt(0);
                                    break;
                                case "map_Ka":
                                    mtlCharacter.mapKa = elements.ElementAt(0);
                                    break;
                                case "map_Ks":
                                    mtlCharacter.mapKs = elements.ElementAt(0);
                                    break;
                            }
                        }
                        else if (character.StartsWith("norm"))
                        {
                            mtlCharacter.norm = elements.ElementAt(0);
                        }
                        else if (character.StartsWith("Ns"))
                        {
                            mtlCharacter.Ns = float.Parse(elements.ElementAt(0));
                        }
                    }

                }
                if (mtlCharacter != null)
                {
                    characters.Add(mtlCharacter);
                    
                }
            }
            mtlCharacters = characters.ToArray();
        }
    }
    public class MtlCharacter
    {
        public string? name;

        public string? mapKd;
        public string? mapKs;
        public string? mapKa;
        public string? norm;

        public int _widthKd;
        public int _widthKs;
        public int _widthKa;
        public int _widthNorm;

        public int _heightKd;
        public int _heightKs;
        public int _heightKa;
        public int _heightNorm;

        public byte[]? kdImage;
        public byte[]? ksImage;
        public byte[]? kaImage;
        public byte[]? normImage;

        public Vector3 Ka = new(1f, 1f, 1f);
        public Vector3 Kd = new(1f, 1f, 1f);
        public Vector3 Ks = new(1f, 1f, 1f);
        public float Ns;
    }
}
