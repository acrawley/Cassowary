using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulator.Services.Configuration
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    sealed class DefaultPropertyAttribute : Attribute
    {
        public DefaultPropertyAttribute(string defaultProperty)
        {
            this.DefaultProperty = defaultProperty;
        }

        public string DefaultProperty { get; private set; }
    }
}
