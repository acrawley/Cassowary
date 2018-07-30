using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cassowary.Services.Configuration
{
    internal interface IConfigurationSection
    {
        Type SectionType { get; }
        object Section { get; set; }
    }
}
