using System;
using UnityEngine;

namespace z3lx.ACGImporter.Editor
{
    /// <summary>
    /// Represents a shader property.
    /// </summary>
    public class ShaderProperty
    {
        private Type _type;
        private string _name;
        private int _id;
        private object _value;

        /// <summary>
        /// Gets or sets the type of the shader property.
        /// </summary>
        public Type Type
        {
            get => _type;
            set
            {
                if (_type == value)
                    return;
                _type = value;
                _value = Activator.CreateInstance(value);
            }
        }

        /// <summary>
        /// Gets or sets the name of the shader property.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (_name == value)
                    return;
                _name = value;
                _id = Shader.PropertyToID(value);
            }
        }

        /// <summary>
        /// Gets the ID of the shader property.
        /// </summary>
        public int Id => _id;

        /// <summary>
        /// Gets or sets the value of the shader property.
        /// </summary>
        public object Value
        {
            get => _value;
            set
            {
                if (_value == value)
                    return;
                _value = value;
                _type = value.GetType();
            }
        }

        /// <summary>
        /// Initializes a new instance of the ShaderProperty class.
        /// </summary>
        public ShaderProperty()
        {
            Type = typeof(MapType);
        }

        /// <summary>
        /// Initializes a new instance of the ShaderProperty class with the specified name and value.
        /// </summary>
        public ShaderProperty(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }
}
