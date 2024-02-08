using System.Collections.Generic;
using UnityEngine;

namespace z3lx.ACGImporter.Editor
{
    public class ImporterConfig
    {
        public string inputPath;
        public string outputPath;
        public bool createCategoryDirectory;
        public bool createMaterialDirectory;
        public Shader shader;
        public List<ShaderProperty> shaderProperties;
    }
}
