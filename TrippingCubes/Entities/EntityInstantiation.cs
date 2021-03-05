using System;
using System.Collections.Generic;
using System.Text;

namespace TrippingCubes.Entities
{
    struct EntityInstantiation
    {
        public string ConfigurationIdentifier { get; set; }

        public IEnumerable<KeyValuePair<string, string>> InstanceParameters 
            { get; set; }
    }
}
