using System.Collections.Generic;
using Cassowary.Services.Configuration;

namespace Cassowary.Services.Input.Implementation.Configuration
{
    [DefaultProperty("Mappings")]
    internal class ControllerElement
    {
        internal ControllerElement()
        {
            this.Mappings = new List<ElementMapping>();
        }

        public string Id { get; set; }

        [ValidType(typeof(KeyboardMapping))]
        [ValidType(typeof(XInputMapping))]
        public IList<ElementMapping> Mappings { get; }
    }
}
