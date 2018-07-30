using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Cassowary.Services.Input.Implementation.Readers
{
    [Export(typeof(IXInputReader))]
    internal class XInputReader : IXInputReader
    {
        #region Constants

        private const UInt32 ERROR_DEVICE_NOT_CONNECTED = 0x48F;
        private const UInt32 ERROR_SUCCESS = 0x00;
        private const UInt32 MAX_CONTROLLERS = 4;
        private const int XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE = 7849;
        private const int XINPUT_GAMEPAD_RIGHT_THUMB_DEADZONE = 8689;
        private const int XINPUT_GAMEPAD_TRIGGER_THRESHOLD = 30;

        #endregion

        #region Private Fields

        private UInt32[] lastPacketNumber = new UInt32[MAX_CONTROLLERS];
        private bool[,] buttonStates = new bool[MAX_CONTROLLERS, (int)XInputElement.BTN_LAST];

        #endregion

        #region XInput P/Invokes

        private struct XINPUT_STATE
        {
            public UInt32 dwPacketNumber;
            public XINPUT_GAMEPAD Gamepad;
        }

        private struct XINPUT_GAMEPAD
        {
            public XINPUT_GAMEPAD_BUTTONS wButtons;
            public byte bLeftTrigger;
            public byte bRightTrigger;

            public Int16 sThumbLX;
            public Int16 sThumbLY;
            public Int16 sThumbRX;
            public Int16 sThumbRY;
        }

        [Flags]
        private enum XINPUT_GAMEPAD_BUTTONS : UInt16
        {
            XINPUT_GAMEPAD_DPAD_UP = 0x0001,
            XINPUT_GAMEPAD_DPAD_DOWN = 0x0002,
            XINPUT_GAMEPAD_DPAD_LEFT = 0x0004,
            XINPUT_GAMEPAD_DPAD_RIGHT = 0x0008,
            XINPUT_GAMEPAD_START = 0x0010,
            XINPUT_GAMEPAD_BACK = 0x0020,
            XINPUT_GAMEPAD_LEFT_THUMB = 0x0040,
            XINPUT_GAMEPAD_RIGHT_THUMB = 0x0080,
            XINPUT_GAMEPAD_LEFT_SHOULDER = 0x0100,
            XINPUT_GAMEPAD_RIGHT_SHOULDER = 0x0200,
            XINPUT_GAMEPAD_A = 0x1000,
            XINPUT_GAMEPAD_B = 0x2000,
            XINPUT_GAMEPAD_X = 0x4000,
            XINPUT_GAMEPAD_Y = 0x8000,
        }

        [DllImport("Xinput9_1_0.dll")]
        private static extern UInt32 XInputGetState(UInt32 dwUserIndex, ref XINPUT_STATE pState);

        #endregion

        #region IXInputReader Implementation

        bool IXInputReader.GetButtonState(int controller, XInputElement element)
        {
            if (!UpdateState(controller))
            {
                return false;
            }

            return buttonStates[controller, (int)element];
        }

        #endregion

        private bool UpdateState(int controller)
        {
            XINPUT_STATE pState = new XINPUT_STATE();
            UInt32 err = XInputGetState((UInt32)controller, ref pState);

            if (err != ERROR_SUCCESS)
            {
                return false;
            }

            if (pState.dwPacketNumber == lastPacketNumber[controller])
            {
                // Controller state hasn't changed since last time
                return true;
            }

            lastPacketNumber[controller] = pState.dwPacketNumber;

            XINPUT_GAMEPAD_BUTTONS buttons = pState.Gamepad.wButtons;

            // Face buttons
            buttonStates[controller, (int)XInputElement.BTN_A] = buttons.HasFlag(XINPUT_GAMEPAD_BUTTONS.XINPUT_GAMEPAD_A);
            buttonStates[controller, (int)XInputElement.BTN_B] = buttons.HasFlag(XINPUT_GAMEPAD_BUTTONS.XINPUT_GAMEPAD_B);
            buttonStates[controller, (int)XInputElement.BTN_X] = buttons.HasFlag(XINPUT_GAMEPAD_BUTTONS.XINPUT_GAMEPAD_X);
            buttonStates[controller, (int)XInputElement.BTN_Y] = buttons.HasFlag(XINPUT_GAMEPAD_BUTTONS.XINPUT_GAMEPAD_Y);

            buttonStates[controller, (int)XInputElement.BTN_BACK] = buttons.HasFlag(XINPUT_GAMEPAD_BUTTONS.XINPUT_GAMEPAD_BACK);
            buttonStates[controller, (int)XInputElement.BTN_START] = buttons.HasFlag(XINPUT_GAMEPAD_BUTTONS.XINPUT_GAMEPAD_START);

            // D-pad
            buttonStates[controller, (int)XInputElement.BTN_DPAD_UP] = buttons.HasFlag(XINPUT_GAMEPAD_BUTTONS.XINPUT_GAMEPAD_DPAD_UP);
            buttonStates[controller, (int)XInputElement.BTN_DPAD_DOWN] = buttons.HasFlag(XINPUT_GAMEPAD_BUTTONS.XINPUT_GAMEPAD_DPAD_DOWN);
            buttonStates[controller, (int)XInputElement.BTN_DPAD_LEFT] = buttons.HasFlag(XINPUT_GAMEPAD_BUTTONS.XINPUT_GAMEPAD_DPAD_LEFT);
            buttonStates[controller, (int)XInputElement.BTN_DPAD_RIGHT] = buttons.HasFlag(XINPUT_GAMEPAD_BUTTONS.XINPUT_GAMEPAD_DPAD_RIGHT);

            // Thumb buttons
            buttonStates[controller, (int)XInputElement.BTN_LSTICK] = buttons.HasFlag(XINPUT_GAMEPAD_BUTTONS.XINPUT_GAMEPAD_LEFT_THUMB);
            buttonStates[controller, (int)XInputElement.BTN_RSTICK] = buttons.HasFlag(XINPUT_GAMEPAD_BUTTONS.XINPUT_GAMEPAD_RIGHT_THUMB);

            // Shoulder buttons
            buttonStates[controller, (int)XInputElement.BTN_LSHOULDER] = buttons.HasFlag(XINPUT_GAMEPAD_BUTTONS.XINPUT_GAMEPAD_DPAD_RIGHT);
            buttonStates[controller, (int)XInputElement.BTN_RSHOULDER] = buttons.HasFlag(XINPUT_GAMEPAD_BUTTONS.XINPUT_GAMEPAD_DPAD_RIGHT);

            // Translate analog triggers to buttons
            buttonStates[controller, (int)XInputElement.BTN_LTRIGGER] = pState.Gamepad.bLeftTrigger > XINPUT_GAMEPAD_TRIGGER_THRESHOLD;
            buttonStates[controller, (int)XInputElement.BTN_RTRIGGER] = pState.Gamepad.bRightTrigger > XINPUT_GAMEPAD_TRIGGER_THRESHOLD;

            // Translate analog sticks to buttons
            Int16 lStickX = pState.Gamepad.sThumbLX;
            Int16 lStickY = pState.Gamepad.sThumbLY;
            if (Math.Sqrt(lStickX * lStickX + lStickY * lStickY) > XINPUT_GAMEPAD_LEFT_THUMB_DEADZONE)
            {
                double angle = Math.Atan2(lStickY, lStickX) + Math.PI;

                buttonStates[controller, (int)XInputElement.BTN_LSTICK_UP] = (angle >= 3.53429174 && angle <= 5.89048623);
                buttonStates[controller, (int)XInputElement.BTN_LSTICK_DOWN] = (angle >= 0.39269908 && angle <= 2.74889357);
                buttonStates[controller, (int)XInputElement.BTN_LSTICK_LEFT] = (angle >= 5.10508806 || angle <= 1.17809725);
                buttonStates[controller, (int)XInputElement.BTN_LSTICK_RIGHT] = (angle >= 1.96349541 && angle <= 4.3196899);
            }
            else
            {
                buttonStates[controller, (int)XInputElement.BTN_LSTICK_UP] = false;
                buttonStates[controller, (int)XInputElement.BTN_LSTICK_DOWN] = false;
                buttonStates[controller, (int)XInputElement.BTN_LSTICK_LEFT] = false;
                buttonStates[controller, (int)XInputElement.BTN_LSTICK_RIGHT] = false;
            }

            Int16 rStickX = pState.Gamepad.sThumbLX;
            Int16 rStickY = pState.Gamepad.sThumbLY;
            if (Math.Sqrt(rStickX * rStickX + rStickY * rStickY) > XINPUT_GAMEPAD_RIGHT_THUMB_DEADZONE)
            {
                double angle = Math.Atan2(lStickY, lStickX) + Math.PI;

                buttonStates[controller, (int)XInputElement.BTN_RSTICK_UP] = (angle >= 3.53429174 && angle <= 5.89048623);
                buttonStates[controller, (int)XInputElement.BTN_RSTICK_DOWN] = (angle >= 0.39269908 && angle <= 2.74889357);
                buttonStates[controller, (int)XInputElement.BTN_RSTICK_LEFT] = (angle >= 5.10508806 || angle <= 1.17809725);
                buttonStates[controller, (int)XInputElement.BTN_RSTICK_RIGHT] = (angle >= 1.96349541 && angle <= 4.3196899);
            }
            else
            {
                buttonStates[controller, (int)XInputElement.BTN_RSTICK_UP] = false;
                buttonStates[controller, (int)XInputElement.BTN_RSTICK_DOWN] = false;
                buttonStates[controller, (int)XInputElement.BTN_RSTICK_LEFT] = false;
                buttonStates[controller, (int)XInputElement.BTN_RSTICK_RIGHT] = false;
            }

            return true;
        }
    }
}
