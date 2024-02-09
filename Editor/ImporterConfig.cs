using System.Collections.Generic;
using UnityEngine;

namespace z3lx.ACGImporter.Editor
{
    /// <summary>
    /// Represents the configuration for the ACGImporter.
    /// </summary>
    public class ImporterConfig
    {
        /// <summary>
        /// Gets or sets the input path for the importer.
        /// </summary>
        public string InputPath;

        /// <summary>
        /// Gets or sets the output path for the importer.
        /// </summary>
        public string OutputPath;

        /// <summary>
        /// Gets or sets a flag indicating whether to
        /// create a directory for each material category.
        /// </summary>
        public bool CreateCategoryDirectory;

        /// <summary>
        /// Gets or sets a flag indicating whether to
        /// create a directory for each material.
        /// </summary>
        public bool CreateMaterialDirectory;

        /// <summary>
        /// Gets or sets the shader to be used to create
        /// the material.
        /// </summary>
        public Shader Shader;

        /// <summary>
        /// Gets or sets the list of shader properties to
        /// be set the material.
        /// </summary>
        public List<ShaderProperty> ShaderProperties;
    }
}
