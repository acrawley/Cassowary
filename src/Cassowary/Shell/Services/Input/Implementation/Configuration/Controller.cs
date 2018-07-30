using System.Collections.Generic;
using Cassowary.Services.Configuration;

namespace Cassowary.Services.Input.Implementation.Configuration
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
