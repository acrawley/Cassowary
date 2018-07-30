using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassowary.Shared.Components.Input;

namespace Cassowary.Tests.Nes.Fakes
{
    [Export(typeof(IInputReader))]
    internal class FakeInputReader : IInputReader
    {
        void IInputReader.Poll(int controllerIndex, IInputDevice device)
        {
            
        }
    }
}
