using System.Collections.Generic;
using Emulator.Services.Configuration;

namespace Emulator.Services.Input.Implementation.Configuration
{
    [SerializedName("Input")]
    [DefaultProperty("Controllers")]
    internal class InputConfiguration
    {
        internal InputConfiguration()
        {
            this.Controllers = new List<Controller>();
        }

        internal bool AllowImpossibleInputs { get; set; }

        internal IList<Controller> Controllers { get; }
    }
}
