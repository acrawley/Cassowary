using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Emulator.Services.Input
{
    internal interface IInputService
    {
        void SetKeyDown(Key key);
        void SetKeyUp(Key key);
    }
}
