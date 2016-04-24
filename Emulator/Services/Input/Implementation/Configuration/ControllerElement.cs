using System.Collections.Generic;
using Emulator.Services.Configuration;

namespace Emulator.Services.Input.Implementation.Configuration
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
