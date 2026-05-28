using System;

namespace LitJson
{
    internal struct ArrayMetadata
    {
        private Type _elementType;

        public Type ElementType
        {
            get => _elementType ?? typeof(JsonData);
            set => _elementType = value;
        }

        public bool IsArray { get; set; }

        public bool IsList  { get; set; }
    }
}
