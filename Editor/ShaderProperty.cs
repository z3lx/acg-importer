using UnityEngine;

namespace z3lx.ACGImporter.Editor
{
    public class ShaderProperty
    {
        public enum Type
        {
            Texture,
            Float,
            Int
        }

        public ShaderProperty(Type type, string name, string value)
        {
            this.type = type;
            this.name = name;
            this.value = value;
        }

        public Type type;
        public string name;
        public string value;
    }
}
