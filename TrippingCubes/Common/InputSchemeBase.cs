/*
 * TrippingCubes
 * A toolkit for creating games in a voxel-based environment.
 * Copyright (C) 2020, Maximilian Bauer (contact@lengo.cc)
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

using ShamanTK.Controls;
using System;
using System.Collections.Generic;

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
