using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EmulatorCore.Components.Input;

namespace NesTests.Fakes
{
    [Export(typeof(IInputReader))]
    internal class FakeInputReader : IInputReader
    {
        void IInputReader.Poll(int controllerIndex, IInputDevice device)
        {
            
        }
    }
}
