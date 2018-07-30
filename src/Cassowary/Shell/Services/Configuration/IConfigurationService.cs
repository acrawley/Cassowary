using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cassowary.Services.Configuration
{
    internal interface IConfigurationService
    {
        string ConfigurationFileName { get; set; }

        void Save();
        void Load();
    }
}
