using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cassowary.Services.Input.Implementation.Configuration
{
    internal class XInputMapping : ElementMapping
    {
        public int Controller { get; set; }
        public XInputElement Element { get; set; }
    }
}
