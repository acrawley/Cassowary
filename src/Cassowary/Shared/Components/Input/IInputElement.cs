using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassowary.Shared.Components.Core;

namespace Cassowary.Shared.Components.Input
{
    public interface IInputElement
    {
        string Name { get; }
        string Id { get; }
    }
}
