using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cassowary.Shared.Components.Input
{
    public interface IButtonInputElement : IInputElement
    {
        bool State { get; set; }
    }
}
