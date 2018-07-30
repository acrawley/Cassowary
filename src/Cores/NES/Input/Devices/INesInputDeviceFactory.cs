using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cassowary.Core.Nes.Input.Devices
{
    internal interface INesInputDeviceFactory
    {
        INesInputDevice CreateInstance();
    }

    internal interface INesInputDeviceFactoryMetadata
    {
        string DeviceName { get; }
    }

    [MetadataAttribute]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class InputDeviceFactoryNameAttribute : Attribute
    {
        public InputDeviceFactoryNameAttribute(string inputDeviceName)
        {
            this.InputDeviceName = inputDeviceName;
        }

        public string InputDeviceName { get; private set; }
    }
}
