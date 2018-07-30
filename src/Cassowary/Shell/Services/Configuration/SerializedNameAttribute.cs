using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cassowary.Services.Configuration
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    sealed class SerializedNameAttribute : Attribute
    {
        public SerializedNameAttribute(string typeName)
        {
            this.Name = typeName;
        }
        
        public string Name { get; private set; }
    }
}
