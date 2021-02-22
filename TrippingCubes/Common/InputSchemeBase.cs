using ShamanTK.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace TrippingCubes.Common
{
    abstract class InputSchemeBase
    {
        private IDictionary<string, ControlMapping> mappings;

        public ICollection<string> MappingNames
        {
            get
            {
                ValidateMappingsCache();

                return mappings.Keys;
            }
        }

        protected InputSchemeBase() { }

        protected void InvalidateMappingsCache()
        {
            mappings = null;
        }

        private void ValidateMappingsCache()
        {
            if (mappings == null)
            {
                mappings = GetMappings() ?? 
                    new Dictionary<string, ControlMapping>();
            }
        }

        public bool TryGetControlMapping(string mappingName, 
            out ControlMapping controlMapping)
        {
            ValidateMappingsCache();

            return mappings.TryGetValue(mappingName, out controlMapping);
        }

        protected virtual IDictionary<string, ControlMapping> GetMappings()
        {
            var mappings = new Dictionary<string, ControlMapping>();
            foreach (var property in GetType().GetProperties())
            {
                Type requiredType = typeof(ControlMapping);
                Type propertyType = property.PropertyType;

                if (requiredType.IsAssignableFrom(propertyType))
                {
                    object value = property.GetValue(this);
                    if (value is ControlMapping mapping)
                        mappings.Add(property.Name, mapping);
                }
            }
            return mappings;
        }
    }
}
