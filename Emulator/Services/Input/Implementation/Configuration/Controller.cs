using System.Collections.Generic;
using Emulator.Services.Configuration;

namespace Emulator.Services.Input.Implementation.Configuration
{
    [DefaultProperty("Elements")]
    internal class Controller
    {
        internal Controller()
        {
            this.Elements = new List<ControllerElement>();
        }

        internal int Index { get; set; }
        internal bool Connected { get; set; }
        internal string Type { get; set; }

        internal IList<ControllerElement> Elements { get; }
    }
}
