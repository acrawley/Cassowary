using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cassowary.Services.Input
{
    public interface IXInputReader
    {
        bool GetButtonState(int controller, XInputElement element);
    }

    public enum XInputElement
    {
        BTN_A = 0,
        BTN_B,
        BTN_X,
        BTN_Y,

        BTN_BACK,
        BTN_START,

        BTN_DPAD_UP,
        BTN_DPAD_DOWN,
        BTN_DPAD_LEFT,
        BTN_DPAD_RIGHT,

        BTN_LSTICK,
        BTN_LSTICK_UP,
        BTN_LSTICK_DOWN,
        BTN_LSTICK_LEFT,
        BTN_LSTICK_RIGHT,

        BTN_RSTICK,
        BTN_RSTICK_UP,
        BTN_RSTICK_DOWN,
        BTN_RSTICK_LEFT,
        BTN_RSTICK_RIGHT,

        BTN_LSHOULDER,
        BTN_RSHOULDER,

        BTN_LTRIGGER,
        BTN_RTRIGGER,

        BTN_LAST
    }
}
