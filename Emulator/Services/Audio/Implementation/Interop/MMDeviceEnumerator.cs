using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Emulator.Services.Audio.Implementation.Interop
{
    [ComImport]
    [Guid(Constants.CLSID_MMDeviceEnumerator)]
    internal class MMDeviceEnumerator
    {
    }
}
