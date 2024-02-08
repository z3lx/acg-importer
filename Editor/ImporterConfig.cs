using System.Collections.Generic;
using UnityEngine;

namespace z3lx.ACGImporter.Editor
{
    public class ImporterConfig
    {
        public string InputPath;
        public string OutputPath;
        public bool CreateCategoryDirectory;
        public bool CreateMaterialDirectory;
        public Shader Shader;
        public List<ShaderProperty> ShaderProperties;
    }
}
