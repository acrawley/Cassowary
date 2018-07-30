using System;
using System.Collections.Generic;

namespace Cassowary.Services.Configuration.Implementation
{
    [DefaultProperty("Sections")]
    [SerializedName("Configuration")]
    internal class ConfigurationRoot
    {
        internal ConfigurationRoot()
        {
            this.Sections = new List<object>();
        }

        public IList<Object> Sections { get; }
    }
}
