using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Cassowary.Services.Input
{
    interface IKeyboardReader
    {
        void NotifyKeyDown(Key key);
        void NotifyKeyUp(Key key);

        bool GetKeyState(Key key);
    }
}
