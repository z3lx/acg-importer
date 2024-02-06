using System;
using UnityEngine;

namespace z3lx.ACGImporter.Editor
{
    public class ShaderProperty
    {
        private Type _type;
        private string _name;
        private int _id;
        private object _value;

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

        public int Id => _id;

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

        public ShaderProperty()
        {
            Type = typeof(MapType);
        }

        public ShaderProperty(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }
}
