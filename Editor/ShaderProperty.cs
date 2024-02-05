namespace z3lx.ACGImporter.Editor
{
    public class ShaderProperty
    {
        public enum PropertyType
        {
            Texture,
            Float,
            Int
        }

        public ShaderProperty(PropertyType type, string name, string value)
        {
            this.type = type;
            this.name = name;
            this.value = value;
        }

        public PropertyType type;
        public string name;
        public string value;
    }
}
