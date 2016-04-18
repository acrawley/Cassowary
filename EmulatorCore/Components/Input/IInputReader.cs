using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmulatorCore.Components.Input
{
    public interface IInputReader
    {
        void Poll(int controllerIndex, IEnumerable<IInputElement> elements);
    }
}
