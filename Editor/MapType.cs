namespace z3lx.ACGImporter.Editor
{
    /// <summary>
    /// Represents a type of map.
    /// </summary>
    public enum MapType
    {
        /// <summary>
        /// Represents a color map.
        /// </summary>
        Color,

        /// <summary>
        /// Represents a normal map.
        /// </summary>
        Normal,

        /// <summary>
        /// Represents a metallic map.
        /// </summary>
        Metallic,

        /// <summary>
        /// Represents a roughness map.
        /// </summary>
        Roughness,

        /// <summary>
        /// Represents an occlusion map.
        /// </summary>
        Occlusion,

        /// <summary>
        /// Represents a height map.
        /// </summary>
        Height,

        /// <summary>
        /// Represents a smoothness map.
        /// </summary>
        Smoothness,

        /// <summary>
        /// Represents a metallic gloss map, with
        /// Metallic (RGB) and Smoothness (A) packed into a single texture.
        /// </summary>
        MetallicGloss,

        /// <summary>
        /// Represents a mask map, with
        /// Metallic (R), Occlusion (G), Detail Mask (B), and Smoothness (A) packed into a single texture.
        /// </summary>
        Mask
    }
}
